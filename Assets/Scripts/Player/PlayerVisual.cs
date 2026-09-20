using OnePercentBattery.Core;
using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using UnityEngine;

namespace OnePercentBattery.Player
{
    /// <summary>
    /// Placeholder-friendly presentation layer: tints the body sprite by charge level and by the
    /// equipped skin, and drives a TrailRenderer for the equipped trail. Swap <see cref="ApplySkin"/>
    /// for a per-id sprite swap once real character art exists - the rest of the game doesn't care.
    /// </summary>
    public sealed class PlayerVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private SpriteRenderer shieldRingRenderer;
        [SerializeField] private SpriteRenderer glowRenderer;
        [SerializeField] private SpriteRenderer mouthRenderer;
        [Tooltip("Placeholder Magnet feedback - a ring around the character, same treatment as shieldRingRenderer. Swap the sprite for final art later; the toggle/scale logic doesn't change.")]
        [SerializeField] private SpriteRenderer magnetRingRenderer;

        [SerializeField] private Color healthyColor = new Color(0.68f, 0.92f, 0.36f);
        [SerializeField] private Color lowColor = new Color(0.95f, 0.65f, 0.2f);
        [SerializeField] private Color criticalColor = new Color(0.94f, 0.45f, 0.5f);

        [Header("Battery fill readout (on the character itself, not just the HUD %)")]
        [Tooltip("A dark mask that grows down from the top of the body as charge depletes, like a battery gauge.")]
        [SerializeField] private SpriteRenderer batteryFillOverlay;
        [SerializeField] private Color depletedOverlayColor = new Color(0.05f, 0.07f, 0.08f, 0.72f);

        [Header("Expression (mouth flattens/inverts as charge drops)")]
        [Tooltip("Whichever sprite currently reads as a smile - flattened for the low tier, swapped away from for critical.")]
        [SerializeField] private Sprite happySprite;
        [SerializeField] private Sprite worriedSprite;
        [Range(0.1f, 1f)]
        [SerializeField] private float lowTierMouthFlatten = 0.35f;

        [Header("Squash & stretch")]
        [Tooltip("Recovery time back to the resting pose - not a fixed duration, so a new jump can interrupt it without snapping (see Vector3.SmoothDamp).")]
        [SerializeField] private float squashStretchRecoverySeconds = 0.12f;
        [Range(0f, 0.6f)]
        [SerializeField] private float jumpStretchAmount = 0.22f;
        [Range(0f, 0.6f)]
        [SerializeField] private float landSquashAmount = 0.24f;

        private Color _skinTint;
        private Vector3 _baseScale;
        private Vector3 _scaleVelocity;
        private bool _survivalMode;
        private Vector3 _mouthBaseScale = Vector3.one;
        private Vector3 _glowBaseScale = Vector3.one;
        private Vector3 _shieldBaseScale = Vector3.one;
        private Vector3 _trailBaseScale = Vector3.one;
        private Vector3 _magnetRingBaseScale = Vector3.one;
        private Vector3 _overlayIntendedScale = Vector3.one;
        private Vector3 _overlayIntendedLocalPos = Vector3.zero;

        private void Awake()
        {
            _skinTint = healthyColor;
            _baseScale = transform.localScale;
            if (batteryFillOverlay != null)
            {
                batteryFillOverlay.color = depletedOverlayColor;
                _overlayIntendedScale = batteryFillOverlay.transform.localScale;
                _overlayIntendedLocalPos = batteryFillOverlay.transform.localPosition;
            }
            if (glowRenderer != null) _glowBaseScale = glowRenderer.transform.localScale;
            if (shieldRingRenderer != null) _shieldBaseScale = shieldRingRenderer.transform.localScale;
            if (trailRenderer != null) _trailBaseScale = trailRenderer.transform.localScale;
            if (magnetRingRenderer != null) _magnetRingBaseScale = magnetRingRenderer.transform.localScale;
            if (mouthRenderer != null)
            {
                _mouthBaseScale = mouthRenderer.transform.localScale;
                if (happySprite == null)
                {
                    happySprite = mouthRenderer.sprite;
                }
            }
        }

        private void OnEnable()
        {
            GameEvents.BatteryChanged += HandleBatteryChanged;
            GameEvents.ShieldTimeChanged += HandleShieldChanged;
            GameEvents.MagnetTimeChanged += HandleMagnetChanged;
            GameEvents.Jumped += HandleJumped;
            GameEvents.Landed += HandleLanded;
            GameEvents.SurvivalModeEntered += HandleSurvivalModeEntered;
            GameEvents.RunStarted += HandleRunStarted;
        }

        private void OnDisable()
        {
            GameEvents.BatteryChanged -= HandleBatteryChanged;
            GameEvents.ShieldTimeChanged -= HandleShieldChanged;
            GameEvents.MagnetTimeChanged -= HandleMagnetChanged;
            GameEvents.Jumped -= HandleJumped;
            GameEvents.Landed -= HandleLanded;
            GameEvents.SurvivalModeEntered -= HandleSurvivalModeEntered;
            GameEvents.RunStarted -= HandleRunStarted;
        }

        private void Update()
        {
            // SmoothDamp (not a fixed-duration Lerp) so a jump landing mid-recovery from a previous
            // jump/land eases from the CURRENT scale rather than snapping to a freshly-reset start
            // point - back-to-back jumps were visibly "vibrating" the character before this fix.
            if (transform.localScale != _baseScale || _scaleVelocity != Vector3.zero)
            {
                transform.localScale = Vector3.SmoothDamp(transform.localScale, _baseScale, ref _scaleVelocity, squashStretchRecoverySeconds);
            }

            // Glow/ShieldRing are drawn at 2-3x the body's size (a soft halo), so they sit as
            // children of this same transform - without correcting for that, the body's small
            // squash/stretch wobble gets multiplied 2-3x on the halo and reads as it detaching or
            // flickering. Counter-scale them so their rendered size stays constant.
            if (glowRenderer != null)
            {
                glowRenderer.transform.localScale = InverseScaleAround(_glowBaseScale, transform.localScale, _baseScale);
            }
            if (shieldRingRenderer != null)
            {
                shieldRingRenderer.transform.localScale = InverseScaleAround(_shieldBaseScale, transform.localScale, _baseScale);
            }

            // TrailRenderer bakes the emitting transform's lossyScale into its ribbon width, so
            // living on a transform that's mid squash/stretch made the trail visibly warp every
            // jump/land - it now lives on its own child (see TrailAnchor), counter-scaled like
            // Glow/ShieldRing above.
            if (trailRenderer != null)
            {
                trailRenderer.transform.localScale = InverseScaleAround(_trailBaseScale, transform.localScale, _baseScale);
            }
            if (magnetRingRenderer != null)
            {
                magnetRingRenderer.transform.localScale = InverseScaleAround(_magnetRingBaseScale, transform.localScale, _baseScale);
            }

            // The battery-depletion mask is drawn directly over the body sprite, so any squash/stretch
            // on the root warped it right along with the "skin" underneath - reapply the intended
            // scale/position (set by UpdateBatteryFillOverlay) counter-scaled every frame, the same
            // fix as Glow/ShieldRing/Trail above, so it stays visually locked to the body regardless
            // of the current jump/land pose.
            if (batteryFillOverlay != null)
            {
                var overlayTransform = batteryFillOverlay.transform;
                Vector3 counterScale = InverseScaleAround(Vector3.one, transform.localScale, _baseScale);
                overlayTransform.localScale = Vector3.Scale(_overlayIntendedScale, counterScale);
                overlayTransform.localPosition = Vector3.Scale(_overlayIntendedLocalPos, counterScale);
            }
        }

        private static Vector3 InverseScaleAround(Vector3 childBaseScale, Vector3 currentParentScale, Vector3 baseParentScale)
        {
            float x = childBaseScale.x * (baseParentScale.x / Mathf.Max(0.0001f, currentParentScale.x));
            float y = childBaseScale.y * (baseParentScale.y / Mathf.Max(0.0001f, currentParentScale.y));
            return new Vector3(x, y, childBaseScale.z);
        }

        private void HandleRunStarted(RunModeType mode)
        {
            _survivalMode = false;
            transform.localScale = _baseScale;
            _scaleVelocity = Vector3.zero;
            ApplySkin(ShopManager.Instance.EquippedSkin);
            ApplyTrail(ShopManager.Instance.EquippedTrail);
            // PlayerController.BeginRun teleports the body straight to the start X - without this,
            // the TrailRenderer (which just accumulates positions over time) draws one long streak
            // connecting where the previous run died to the new start position.
            if (trailRenderer != null) trailRenderer.Clear();
            RefreshExpression(100f);
        }

        private void HandleJumped()
        {
            transform.localScale = new Vector3(_baseScale.x * (1f - jumpStretchAmount * 0.5f), _baseScale.y * (1f + jumpStretchAmount), _baseScale.z);
            _scaleVelocity = Vector3.zero;
        }

        private void HandleLanded()
        {
            transform.localScale = new Vector3(_baseScale.x * (1f + landSquashAmount), _baseScale.y * (1f - landSquashAmount * 0.5f), _baseScale.z);
            _scaleVelocity = Vector3.zero;
        }

        private void HandleSurvivalModeEntered()
        {
            _survivalMode = true;
            RefreshExpression(PlayerBattery.SurvivalModeThresholdPercent);
        }

        private void HandleBatteryChanged(float currentPercent, float delta)
        {
            if (bodyRenderer == null) return;

            // The equipped skin's accent color stands in for "healthy" (the default skin's accent
            // is literally healthyColor, so an unskinned run looks identical); low/critical stay
            // fixed across every skin so the danger cues read the same regardless of cosmetics.
            Color baseColor = currentPercent <= 15f
                ? criticalColor
                : currentPercent <= 40f
                    ? lowColor
                    : _skinTint;

            bodyRenderer.color = baseColor;
            RefreshExpression(currentPercent);
            UpdateBatteryFillOverlay(currentPercent);
        }

        private void UpdateBatteryFillOverlay(float currentPercent)
        {
            if (batteryFillOverlay == null) return;

            // Stored as the "intended" pose rather than applied directly - Update() reapplies it
            // counter-scaled every frame so squash/stretch on the root can't warp it (see Update()).
            float emptyFraction = 1f - Mathf.Clamp01(currentPercent / 100f);
            _overlayIntendedScale = new Vector3(1f, emptyFraction, 1f);
            _overlayIntendedLocalPos = new Vector3(_overlayIntendedLocalPos.x, 0.5f - emptyFraction * 0.5f, _overlayIntendedLocalPos.z);
        }

        private void RefreshExpression(float currentPercent)
        {
            if (mouthRenderer == null || happySprite == null) return;

            if (_survivalMode || currentPercent <= 15f)
            {
                mouthRenderer.sprite = worriedSprite != null ? worriedSprite : happySprite;
                mouthRenderer.transform.localScale = worriedSprite != null
                    ? _mouthBaseScale
                    : Vector3.Scale(_mouthBaseScale, new Vector3(1f, lowTierMouthFlatten, 1f));
            }
            else if (currentPercent <= 40f)
            {
                mouthRenderer.sprite = happySprite;
                mouthRenderer.transform.localScale = Vector3.Scale(_mouthBaseScale, new Vector3(1f, lowTierMouthFlatten, 1f));
            }
            else
            {
                mouthRenderer.sprite = happySprite;
                mouthRenderer.transform.localScale = _mouthBaseScale;
            }
        }

        private void HandleShieldChanged(float secondsRemaining)
        {
            if (shieldRingRenderer == null) return;
            shieldRingRenderer.enabled = secondsRemaining > 0f;
        }

        private void HandleMagnetChanged(float secondsRemaining)
        {
            if (magnetRingRenderer == null) return;
            magnetRingRenderer.enabled = secondsRemaining > 0f;
        }

        public void ApplySkin(CosmeticDefinition skin)
        {
            _skinTint = skin.AccentColor;
            if (bodyRenderer != null)
            {
                bodyRenderer.color = _skinTint;
            }
        }

        public void ApplyTrail(CosmeticDefinition trail)
        {
            if (trailRenderer == null) return;

            bool hasTrail = trail.Id != ShopCatalog.NoneTrailId;
            trailRenderer.emitting = hasTrail;
            if (hasTrail)
            {
                trailRenderer.startColor = trail.AccentColor;
                Color transparent = trail.AccentColor;
                transparent.a = 0f;
                trailRenderer.endColor = transparent;
            }
        }
    }
}

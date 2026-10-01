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
        [Tooltip("A rounded energy capsule (SpriteRenderer.drawMode=Sliced over a pill-shaped 9-sliced sprite) inset well inside the body silhouette - rises and falls with charge without ever crossing the face. Sliced mode changes .size rather than transform.localScale, so the capsule's rounded caps never distort into hard corners at any fill height, and gracefully shrink into a small rounded blob near the minimum instead of a flat-edged sliver.")]
        [SerializeField] private SpriteRenderer innerCharge;
        [Tooltip("A soft glow that tracks the current top edge of innerCharge, like a meniscus/highlight on the energy level.")]
        [SerializeField] private SpriteRenderer innerChargeTopGlow;
        [Tooltip("innerCharge's fixed width in world units - kept narrow with clear margin on both sides so it can never reach the body's edge (and therefore never the silver rim) at any fill level. Matches the pill sprite's native pixel width exactly (34px @ 100 PPU) so the capsule is never horizontally stretched/shrunk away from its authored corner radius.")]
        [SerializeField] private float chamberWidth = 0.34f;
        [Tooltip("Local Y (in the body's own -0.5..0.5 space) of the chamber's bottom and top - the range innerCharge rises and falls within. Kept well inside the body's rounded edge on both ends.")]
        [SerializeField] private float chamberBottom = -0.34f;
        [SerializeField] private float chamberTop = 0.34f;
        [Tooltip("Never lets the column fully vanish - even at 1% there should be a faint, visible sliver of energy left, matching a 'tiny glowing reserve' rather than nothing. Kept small deliberately - large enough to still render as something, never large enough that low battery reads as 'basically full'. The 9-sliced pill also auto-shrinks both rounded caps together below this height, so the floor still reads as a small glowing blob rather than a flat-edged sliver.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float chamberMinFillFraction = 0.02f;

        [Header("Outer glow (energy aura, tied to charge)")]
        [Tooltip("Glow's alpha at 100% charge - was previously a fixed value baked into the scene and never touched by code, which is why it read as a static halo completely disconnected from the battery's actual state.")]
        [Range(0f, 1f)]
        [SerializeField] private float glowMaxAlpha = 0.35f;
        [Tooltip("Glow's alpha at 1% charge, as a fraction of glowMaxAlpha - never fully off (a dying battery still has SOME aura), but noticeably dimmer than a full one, so the aura reads as an energy level rather than pure decoration.")]
        [Range(0f, 1f)]
        [SerializeField] private float glowMinAlphaFraction = 0.35f;

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
        private Vector3 _chargeIntendedLocalPos = Vector3.zero;
        private Vector3 _topGlowBaseScale = Vector3.one;
        private Vector3 _topGlowIntendedLocalPos = Vector3.zero;

        private void Awake()
        {
            _skinTint = healthyColor;
            _baseScale = transform.localScale;
            if (innerCharge != null)
            {
                innerCharge.drawMode = SpriteDrawMode.Sliced;
                _chargeIntendedLocalPos = innerCharge.transform.localPosition;
            }
            if (innerChargeTopGlow != null)
            {
                _topGlowBaseScale = innerChargeTopGlow.transform.localScale;
                _topGlowIntendedLocalPos = innerChargeTopGlow.transform.localPosition;
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

            // innerCharge/innerChargeTopGlow are drawn directly over the body sprite, so any
            // squash/stretch on the root would warp them right along with the "skin" underneath -
            // reapply their intended scale/position (set by UpdateInnerCharge) counter-scaled every
            // frame, the same fix as Glow/ShieldRing/Trail above, so they stay visually locked to the
            // body regardless of the current jump/land pose.
            Vector3 chargeCounterScale = InverseScaleAround(Vector3.one, transform.localScale, _baseScale);
            if (innerCharge != null)
            {
                // Sliced draw mode already conveys width/height through .size (set in
                // UpdateInnerCharge), so the transform itself only needs to cancel the body's
                // squash/stretch back to identity - never scaled to represent fill.
                innerCharge.transform.localScale = chargeCounterScale;
                innerCharge.transform.localPosition = Vector3.Scale(_chargeIntendedLocalPos, chargeCounterScale);
            }
            if (innerChargeTopGlow != null)
            {
                innerChargeTopGlow.transform.localScale = Vector3.Scale(_topGlowBaseScale, chargeCounterScale);
                innerChargeTopGlow.transform.localPosition = Vector3.Scale(_topGlowIntendedLocalPos, chargeCounterScale);
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
            UpdateInnerCharge(currentPercent, baseColor);
        }

        /// <summary>
        /// Replaces the old edge-to-edge BatteryFillOverlay AND the plain-rectangle InnerCharge that
        /// followed it - both still read as a flat colored block across the face even once contained
        /// inside the body. innerCharge is now a Sliced-mode SpriteRenderer over a pill-shaped 9-sliced
        /// sprite (rounded caps baked into the 17px border on all sides): only .size changes here, never
        /// transform.localScale, so the rounded caps are preserved pixel-perfect at every fill height
        /// instead of being stretched into hard corners. Below the sprite's native cap height, Unity's
        /// own 9-slice shrinks both caps together into a small rounded blob rather than a flat-edged
        /// sliver, which is exactly the "tiny glowing reserve" look wanted near 1%. No separate dark
        /// background track is used (removed ChargeChamberBackground) - contrast against the body comes
        /// from tinting the capsule a lighter/brighter blend of the tier color instead, so it reads as a
        /// glowing core rather than a gauge fill against its track. Fills from the BOTTOM up and sits
        /// behind Eyes/Mouth in sorting order so the face is always fully readable.
        /// </summary>
        private void UpdateInnerCharge(float currentPercent, Color tierColor)
        {
            float fillFraction = Mathf.Max(chamberMinFillFraction, Mathf.Clamp01(currentPercent / 100f));
            float fillHeight = (chamberTop - chamberBottom) * fillFraction;
            float topY = chamberBottom + fillHeight;

            // Applied immediately (not just stored for Update() to pick up next frame) so the capsule/
            // glow are correct the instant battery changes, regardless of Update() call timing/order -
            // Update() still re-applies these every frame afterward, but only to counter-scale against
            // squash/stretch, not to do the actual positioning for the first time.
            Vector3 chargeCounterScale = InverseScaleAround(Vector3.one, transform.localScale, _baseScale);

            if (innerCharge != null)
            {
                Color c = Color.Lerp(tierColor, Color.white, 0.4f);
                c.a = 0.95f;
                innerCharge.color = c;
                innerCharge.size = new Vector2(chamberWidth, fillHeight);
                _chargeIntendedLocalPos = new Vector3(_chargeIntendedLocalPos.x, chamberBottom + fillHeight * 0.5f, _chargeIntendedLocalPos.z);
                innerCharge.transform.localPosition = Vector3.Scale(_chargeIntendedLocalPos, chargeCounterScale);
            }
            if (innerChargeTopGlow != null)
            {
                Color glowColor = Color.Lerp(tierColor, Color.white, 0.5f);
                glowColor.a = 0.55f;
                innerChargeTopGlow.color = glowColor;
                _topGlowIntendedLocalPos = new Vector3(_topGlowIntendedLocalPos.x, topY, _topGlowIntendedLocalPos.z);
                innerChargeTopGlow.transform.localPosition = Vector3.Scale(_topGlowIntendedLocalPos, chargeCounterScale);
            }
            if (glowRenderer != null)
            {
                // Previously a fixed scene-authored color/alpha never touched at runtime, so the aura
                // never visibly reacted to anything - now tinted per tier like the body/capsule, and
                // its intensity tracks the same fillFraction the capsule's height already reads from,
                // so a fuller battery genuinely radiates more (an "energy aura"), a dying one visibly
                // dims but never fully goes dark.
                Color glow = tierColor;
                glow.a = glowMaxAlpha * Mathf.Lerp(glowMinAlphaFraction, 1f, fillFraction);
                glowRenderer.color = glow;
            }
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

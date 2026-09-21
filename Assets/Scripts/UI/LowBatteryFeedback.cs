using OnePercentBattery.Core;
using OnePercentBattery.Data;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>
    /// Central low-battery audiovisual feedback: a continuous 0-1 intensity driven by an
    /// <see cref="AnimationCurve"/> over the current charge, plus one-shot events at four
    /// thresholds (25/10/5/1%). Owns its own screen-space overlays (a soft full-screen tint and a
    /// red critical pulse) and a small, screen-readable glitch flicker - all primitives, easy to
    /// swap for final art/shaders later since everything downstream only reacts to
    /// <see cref="LowBatteryIntensity"/> and the two GameEvents this raises. Does not touch
    /// ZoneManager's own Camera.backgroundColor - this layers strictly on top via its own Images.
    /// </summary>
    public sealed class LowBatteryFeedback : MonoBehaviour
    {
        [Header("Intensity (0 at 100% charge, 1 at 1%/critical)")]
        [Tooltip("X = drain progress (1 - battery/100), so 0.5=50% charge, 0.75=25%, 0.9=10%, 0.95=5%, 0.99=1%. Default keeps 100-50% nearly flat, then ramps through the tension/danger/critical bands the brief called for.")]
        [SerializeField] private AnimationCurve intensityCurve = BuildDefaultIntensityCurve();
        [Tooltip("How quickly the DISPLAYED intensity eases toward the target - e.g. smooths a Charger's sudden jump back down. RunStarted resets this instantly instead of easing it.")]
        [SerializeField] private float intensitySmoothTime = 0.35f;

        [Header("Thresholds (battery % - crossing DOWNWARD fires once each)")]
        [SerializeField] private float warning25Threshold = 25f;
        [SerializeField] private float criticalThreshold = 10f;
        [SerializeField] private float severeThreshold = 5f;
        [SerializeField] private float finalThreshold = 1f;

        [Header("Vignette overlay (continuous, subtle - full-screen tint approximating a vignette until real art exists)")]
        [SerializeField] private Image vignetteOverlay;
        [SerializeField] private Color vignetteColor = new Color(0.9f, 0.55f, 0.15f);
        [SerializeField] private float maxVignetteAlpha = 0.28f;

        [Header("Critical pulse overlay (active once at/below criticalThreshold)")]
        [SerializeField] private Image criticalOverlay;
        [SerializeField] private Color criticalColor = new Color(0.85f, 0.1f, 0.1f);
        [SerializeField] private float maxCriticalOverlayAlpha = 0.3f;
        [SerializeField] private float pulseSpeedMin = 1.2f;
        [SerializeField] private float pulseSpeedMax = 3.2f;

        [Header("Glitch (active once at/below criticalThreshold - brief, small, never touches gameplay)")]
        [SerializeField] private RectTransform glitchTarget;
        [Tooltip("Average glitches per second once battery is at finalThreshold - scales down to ~0 at criticalThreshold.")]
        [SerializeField] private float glitchFrequency = 1.5f;
        [SerializeField] private float glitchIntensity = 3f;
        [SerializeField] private float glitchDuration = 0.07f;

        [Header("Threshold warning text (single reused instance - no per-warning instantiation)")]
        [SerializeField] private Text warningText;
        [SerializeField] private float warningDisplaySeconds = 1.2f;
        [SerializeField] private float warningFadeSeconds = 0.35f;

        /// <summary>
        /// Keyframes at (progress, intensity): 100-50% charge stays nearly flat (0/0.02) so the top
        /// half of the bar reads as "practically normal", 50-25% only nudges up to 0.15 ("very subtle
        /// cues"), then climbs through 0.55 at 10% and 0.80 at 5% before topping out at 1.0 right at
        /// 1% - matching "tension only really starts under 25%, danger under 10%" instead of a linear
        /// or symmetric ramp that would make 50% charge already feel half-critical.
        /// </summary>
        private static AnimationCurve BuildDefaultIntensityCurve()
        {
            var curve = new AnimationCurve(
                new Keyframe(0.00f, 0.00f),
                new Keyframe(0.50f, 0.02f),
                new Keyframe(0.75f, 0.15f),
                new Keyframe(0.90f, 0.55f),
                new Keyframe(0.95f, 0.80f),
                new Keyframe(0.99f, 1.00f));
            for (int i = 0; i < curve.length; i++)
            {
                curve.SmoothTangents(i, 0f);
            }
            return curve;
        }

        public float BatteryNormalized { get; private set; } = 1f;
        public float LowBatteryIntensity { get; private set; }

        private float _displayedIntensity;
        private float _intensityVelocity;

        private bool _warning25Fired, _criticalFired, _severeFired, _finalFired;

        private float _warningTimer;
        private Color _warningBaseColor;

        private float _pulsePhase;
        private float _glitchCheckTimer;
        private float _glitchActiveTimer;
        private Vector2 _glitchAnchoredBase;

        private void Awake()
        {
            if (vignetteOverlay != null) vignetteOverlay.raycastTarget = false;
            if (criticalOverlay != null) criticalOverlay.raycastTarget = false;
            if (warningText != null)
            {
                _warningBaseColor = warningText.color;
                warningText.gameObject.SetActive(false);
            }
            if (glitchTarget != null) _glitchAnchoredBase = glitchTarget.anchoredPosition;
            ApplyOverlayAlpha(0f, 0f);
        }

        private void OnEnable()
        {
            GameEvents.BatteryChanged += HandleBatteryChanged;
            GameEvents.RunStarted += HandleRunStarted;
        }

        private void OnDisable()
        {
            GameEvents.BatteryChanged -= HandleBatteryChanged;
            GameEvents.RunStarted -= HandleRunStarted;
        }

        private void HandleBatteryChanged(float currentPercent, float delta)
        {
            BatteryNormalized = Mathf.Clamp01(currentPercent / 100f);
            float progress = 1f - BatteryNormalized;
            LowBatteryIntensity = Mathf.Clamp01(intensityCurve.Evaluate(progress));
            GameEvents.RaiseLowBatteryIntensityChanged(LowBatteryIntensity);

            UpdateThreshold(ref _warning25Fired, currentPercent, warning25Threshold, BatteryWarningTier.Warning25, "LOW BATTERY");
            UpdateThreshold(ref _criticalFired, currentPercent, criticalThreshold, BatteryWarningTier.Critical10, "BATTERY CRITICAL");
            UpdateThreshold(ref _severeFired, currentPercent, severeThreshold, BatteryWarningTier.Severe5, "5%");
            UpdateThreshold(ref _finalFired, currentPercent, finalThreshold, BatteryWarningTier.Final1, "1%");
        }

        /// <summary>
        /// A fresh run always starts back above every threshold, so the normal BatteryChanged path
        /// (fired above) already clears the one-shot flags and drops LowBatteryIntensity to ~0 on its
        /// own - PlayerBattery.BeginRun raises BatteryChanged before RunStarted fires. What that path
        /// does NOT clear is this component's own mid-animation state (an in-progress warning fade,
        /// glitch offset, pulse phase, or the eased _displayedIntensity), which must snap to idle
        /// immediately rather than easing out over top of a brand new run.
        /// </summary>
        private void HandleRunStarted(RunModeType mode)
        {
            _displayedIntensity = 0f;
            _intensityVelocity = 0f;
            _warningTimer = 0f;
            _pulsePhase = 0f;
            _glitchActiveTimer = 0f;
            _glitchCheckTimer = 0f;
            if (warningText != null) warningText.gameObject.SetActive(false);
            if (glitchTarget != null) glitchTarget.anchoredPosition = _glitchAnchoredBase;
            ApplyOverlayAlpha(0f, 0f);
        }

        private void UpdateThreshold(ref bool fired, float currentPercent, float threshold, BatteryWarningTier tier, string label)
        {
            if (currentPercent <= threshold)
            {
                if (!fired)
                {
                    fired = true;
                    ShowWarning(label);
                    GameEvents.RaiseLowBatteryThresholdCrossed(tier);
                }
            }
            else
            {
                fired = false;
            }
        }

        private void ShowWarning(string label)
        {
            if (warningText == null) return;
            warningText.text = label;
            warningText.gameObject.SetActive(true);
            _warningTimer = warningDisplaySeconds + warningFadeSeconds;
        }

        private void Update()
        {
            _displayedIntensity = Mathf.SmoothDamp(_displayedIntensity, LowBatteryIntensity, ref _intensityVelocity, intensitySmoothTime);

            // How far we are specifically through the critical band (criticalThreshold -> finalThreshold),
            // 0 right at criticalThreshold, 1 at/below finalThreshold - this is what actually drives the
            // pulse/glitch, so they stay off above criticalThreshold instead of tracking the full curve.
            float criticalBand = 0f;
            if (criticalThreshold > finalThreshold)
            {
                criticalBand = Mathf.Clamp01((criticalThreshold - BatteryNormalized * 100f) / (criticalThreshold - finalThreshold));
            }

            UpdatePulse(criticalBand);
            UpdateGlitch(criticalBand);
            UpdateWarningText();

            ApplyOverlayAlpha(_displayedIntensity, criticalBand);
        }

        private void UpdatePulse(float criticalBand)
        {
            if (criticalOverlay == null) return;
            float speed = Mathf.Lerp(pulseSpeedMin, pulseSpeedMax, criticalBand);
            _pulsePhase += Time.deltaTime * speed;
        }

        private float PulseWave01()
        {
            return (Mathf.Sin(_pulsePhase * Mathf.PI * 2f) + 1f) * 0.5f;
        }

        private void UpdateGlitch(float criticalBand)
        {
            if (glitchTarget == null || criticalBand <= 0f)
            {
                if (glitchTarget != null && glitchTarget.anchoredPosition != _glitchAnchoredBase)
                {
                    glitchTarget.anchoredPosition = _glitchAnchoredBase;
                }
                return;
            }

            if (_glitchActiveTimer > 0f)
            {
                _glitchActiveTimer -= Time.deltaTime;
                if (_glitchActiveTimer <= 0f)
                {
                    glitchTarget.anchoredPosition = _glitchAnchoredBase;
                }
                return;
            }

            _glitchCheckTimer -= Time.deltaTime;
            if (_glitchCheckTimer <= 0f)
            {
                const float checkInterval = 0.12f;
                _glitchCheckTimer = checkInterval;
                float chancePerCheck = glitchFrequency * criticalBand * checkInterval;
                if (Random.value < chancePerCheck)
                {
                    _glitchActiveTimer = glitchDuration;
                    float offsetX = Random.Range(-glitchIntensity, glitchIntensity);
                    glitchTarget.anchoredPosition = _glitchAnchoredBase + new Vector2(offsetX, 0f);
                }
            }
        }

        private void UpdateWarningText()
        {
            if (warningText == null || _warningTimer <= 0f) return;

            _warningTimer -= Time.deltaTime;
            float alpha = _warningTimer > warningFadeSeconds
                ? 1f
                : Mathf.Clamp01(_warningTimer / warningFadeSeconds);
            warningText.color = new Color(_warningBaseColor.r, _warningBaseColor.g, _warningBaseColor.b, _warningBaseColor.a * alpha);

            if (_warningTimer <= 0f)
            {
                warningText.gameObject.SetActive(false);
            }
        }

        private void ApplyOverlayAlpha(float intensity, float criticalBand)
        {
            if (vignetteOverlay != null)
            {
                vignetteOverlay.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, intensity * maxVignetteAlpha);
            }
            if (criticalOverlay != null)
            {
                float pulseAlpha = criticalBand > 0f ? criticalBand * maxCriticalOverlayAlpha * Mathf.Lerp(0.4f, 1f, PulseWave01()) : 0f;
                criticalOverlay.color = new Color(criticalColor.r, criticalColor.g, criticalColor.b, pulseAlpha);
            }
        }
    }
}

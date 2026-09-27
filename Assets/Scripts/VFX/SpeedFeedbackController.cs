using OnePercentBattery.Core;
using UnityEngine;

namespace OnePercentBattery.VFX
{
    /// <summary>
    /// Purely cosmetic sense-of-speed layer: drives one persistent, always-present ParticleSystem's
    /// emission rate from the SAME 0-1 intensity LowBatteryFeedback already computes from the battery
    /// curve (see GameEvents.LowBatteryIntensityChanged), instead of re-deriving a second curve - so
    /// "stronger under 25%, stronger again under 10%, strongest near 1%" falls out for free from a
    /// curve already tuned for exactly that banding. Never reads CurrentGameSpeedMultiplier and never
    /// touches actual movement - a single persistent ParticleSystem, no Instantiate/Destroy.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class SpeedFeedbackController : MonoBehaviour
    {
        [SerializeField] private float maxEmissionRate = 18f;
        [Tooltip("Below this LowBatteryIntensity, speed lines are fully off - matches roughly where LowBatteryFeedback's own vignette first becomes visible (~25% charge), so this reads as part of the same tension ramp rather than its own separate cue.")]
        [SerializeField] private float minIntensityToShow = 0.12f;

        private ParticleSystem _particles;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            SetIntensity(0f);
        }

        private void OnEnable()
        {
            GameEvents.LowBatteryIntensityChanged += SetIntensity;
            GameEvents.RunStarted += HandleRunStarted;
        }

        private void OnDisable()
        {
            GameEvents.LowBatteryIntensityChanged -= SetIntensity;
            GameEvents.RunStarted -= HandleRunStarted;
        }

        private void HandleRunStarted(RunModeType mode) => SetIntensity(0f);

        private void SetIntensity(float intensity01)
        {
            float shown = intensity01 < minIntensityToShow ? 0f : intensity01;

            ParticleSystem.EmissionModule emission = _particles.emission;
            emission.rateOverTime = shown * maxEmissionRate;

            if (shown > 0f && !_particles.isPlaying)
            {
                _particles.Play();
            }
            else if (shown <= 0f && _particles.isPlaying)
            {
                _particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}

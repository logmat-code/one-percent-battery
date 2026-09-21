using OnePercentBattery.Core;
using OnePercentBattery.Data;
using OnePercentBattery.Save;
using OnePercentBattery.Utils;
using UnityEngine;

namespace OnePercentBattery.Meta
{
    /// <summary>
    /// Reactive haptic feedback on real devices. Uses the built-in cross-platform
    /// <see cref="Handheld.Vibrate"/> rather than a native plugin - it's a single fixed pulse
    /// (no per-call duration/pattern control), which is enough for "something happened" feedback
    /// without pulling in a third-party haptics package for finer-grained taps.
    /// Gated by the existing Settings "Haptics" toggle (<see cref="SaveData.hapticsEnabled"/>).
    /// </summary>
    public sealed class HapticsManager : Singleton<HapticsManager>
    {
        private void OnEnable()
        {
            GameEvents.Jumped += HandleJumped;
            GameEvents.ObstacleHit += HandleObstacleHit;
            GameEvents.SurvivalModeEntered += HandleSurvivalModeEntered;
            GameEvents.RunEnded += HandleRunEnded;
        }

        private void OnDisable()
        {
            GameEvents.Jumped -= HandleJumped;
            GameEvents.ObstacleHit -= HandleObstacleHit;
            GameEvents.SurvivalModeEntered -= HandleSurvivalModeEntered;
            GameEvents.RunEnded -= HandleRunEnded;
        }

        // Every-jump haptics reads as noisy/annoying on a run this fast - reserved for moments
        // that actually matter (impact, the 1% moment, death).
        private void HandleJumped() { }

        private void HandleObstacleHit() => Pulse();
        private void HandleSurvivalModeEntered() => Pulse();

        private void HandleRunEnded(RunResult result)
        {
            if (result.Cause == RunEndCause.HitObstacle || result.Cause == RunEndCause.FellInPit)
            {
                Pulse();
            }
        }

        private static void Pulse()
        {
            if (!SaveSystem.Data.hapticsEnabled) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}

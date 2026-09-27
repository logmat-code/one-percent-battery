using System;
using OnePercentBattery.Data;
using OnePercentBattery.Spawning;
using UnityEngine;

namespace OnePercentBattery.Core
{
    /// <summary>
    /// Static hub of gameplay-wide events. Keeps the player, spawners, HUD and meta managers
    /// (missions, achievements, currency) decoupled from each other.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<RunModeType> RunStarted;
        public static event Action<RunResult> RunEnded;
        public static event Action<RunResult, bool> RunAwaitingContinue; // (preview result, canOfferSecondChance)
        public static event Action SecondChanceGranted;
        public static event Action ReturnedToMenu;
        public static event Action GamePaused;
        public static event Action GameResumed;

        public static event Action<float> DistanceChanged;
        public static event Action<float, float> BatteryChanged; // (current 0-100, delta applied)
        public static event Action<float> ShieldTimeChanged; // seconds remaining, 0 = inactive
        public static event Action<float> MagnetTimeChanged; // seconds remaining, 0 = inactive

        public static event Action SurvivalModeEntered;
        public static event Action Jumped;
        public static event Action Landed;
        public static event Action<int> CoinCollected; // running coin total for the active run
        public static event Action<float> EnergyCollected; // amount restored
        public static event Action ObstacleHit;
        public static event Action<ZoneDefinition> ZoneChanged;

        public static event Action<float> LowBatteryIntensityChanged; // 0 (100% charge) to 1 (1%/critical)
        public static event Action<BatteryWarningTier> LowBatteryThresholdCrossed; // fires once per downward crossing

        // Purely cosmetic notifications for the VFX layer (see Assets/Scripts/VFX) - nothing else
        // subscribes to these, and nothing upstream changes behavior based on whether anything is
        // listening. Kept generic (one event per pickup type instead of four) rather than one-per-cue.
        public static event Action<PickupType, Vector3> PickupCollected; // (type, world position) - fires for Coin/Energy/Shield/Magnet, alongside the existing specific events those already raise
        public static event Action<Vector3> ShieldBlockedHit; // (world position) - an Obstacle hit that IsInvulnerable already turned into a no-op; ObstacleHit does NOT fire for this case
        public static event Action PitCleared; // the player left a Pit's horizontal span without ever committing to the fall - fires once, before the landing that follows it

        public static void RaiseRunStarted(RunModeType mode) => RunStarted?.Invoke(mode);
        public static void RaiseRunEnded(RunResult result) => RunEnded?.Invoke(result);
        public static void RaiseRunAwaitingContinue(RunResult preview, bool canOfferSecondChance) => RunAwaitingContinue?.Invoke(preview, canOfferSecondChance);
        public static void RaiseSecondChanceGranted() => SecondChanceGranted?.Invoke();
        public static void RaiseReturnedToMenu() => ReturnedToMenu?.Invoke();
        public static void RaiseGamePaused() => GamePaused?.Invoke();
        public static void RaiseGameResumed() => GameResumed?.Invoke();

        public static void RaiseDistanceChanged(float meters) => DistanceChanged?.Invoke(meters);
        public static void RaiseBatteryChanged(float current, float delta) => BatteryChanged?.Invoke(current, delta);
        public static void RaiseShieldTimeChanged(float secondsRemaining) => ShieldTimeChanged?.Invoke(secondsRemaining);
        public static void RaiseMagnetTimeChanged(float secondsRemaining) => MagnetTimeChanged?.Invoke(secondsRemaining);

        public static void RaiseSurvivalModeEntered() => SurvivalModeEntered?.Invoke();
        public static void RaiseJumped() => Jumped?.Invoke();
        public static void RaiseLanded() => Landed?.Invoke();
        public static void RaiseCoinCollected(int runTotal) => CoinCollected?.Invoke(runTotal);
        public static void RaiseEnergyCollected(float amount) => EnergyCollected?.Invoke(amount);
        public static void RaiseObstacleHit() => ObstacleHit?.Invoke();
        public static void RaiseZoneChanged(ZoneDefinition zone) => ZoneChanged?.Invoke(zone);

        public static void RaiseLowBatteryIntensityChanged(float intensity01) => LowBatteryIntensityChanged?.Invoke(intensity01);
        public static void RaiseLowBatteryThresholdCrossed(BatteryWarningTier tier) => LowBatteryThresholdCrossed?.Invoke(tier);

        public static void RaisePickupCollected(PickupType type, Vector3 position) => PickupCollected?.Invoke(type, position);
        public static void RaiseShieldBlockedHit(Vector3 position) => ShieldBlockedHit?.Invoke(position);
        public static void RaisePitCleared() => PitCleared?.Invoke();
    }
}

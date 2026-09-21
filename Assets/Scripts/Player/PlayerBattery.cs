using OnePercentBattery.Core;
using OnePercentBattery.Data;
using UnityEngine;

namespace OnePercentBattery.Player
{
    /// <summary>
    /// The battery charge is this game's health bar. Passive drain + obstacle hits bring it down,
    /// energy pickups refill it. A short invulnerability window ("Shielded") follows every hit so a
    /// cluster of obstacles can't chain-kill in one frame.
    ///
    /// The signature mechanic lives here too: once passive drain alone would bring charge to
    /// <see cref="SurvivalModeThresholdPercent"/>, it clamps there instead of continuing to 0 -
    /// "1% Survival Mode". The clock keeps running (tracked as <see cref="SurvivalModeSeconds"/>)
    /// but only a hit can end the run from there. Passive drain never kills during survival mode;
    /// only ApplyHit can.
    /// </summary>
    public sealed class PlayerBattery : MonoBehaviour
    {
        [SerializeField] private float postHitInvulnerabilitySeconds = 1.5f;

        public const float SurvivalModeThresholdPercent = 1f;

        public float CurrentPercent { get; private set; }
        public bool IsInvulnerable => _shieldSecondsRemaining > 0f;
        public bool IsDead { get; private set; }
        public bool IsInSurvivalMode { get; private set; }
        public float SurvivalModeSeconds { get; private set; }
        public RunEndCause DeathCause { get; private set; }

        private RunModeConfig _config;
        private float _shieldSecondsRemaining;
        private RunEndCause _pendingCause;

        public void BeginRun(RunModeConfig config)
        {
            _config = config;
            CurrentPercent = config.StartingChargePercent;
            IsDead = false;
            IsInSurvivalMode = false;
            SurvivalModeSeconds = 0f;
            _shieldSecondsRemaining = 0f;
            GameEvents.RaiseBatteryChanged(CurrentPercent, 0f);
            GameEvents.RaiseShieldTimeChanged(0f);
        }

        public void Tick(float deltaTime)
        {
            if (IsDead) return;

            if (_shieldSecondsRemaining > 0f)
            {
                _shieldSecondsRemaining = Mathf.Max(0f, _shieldSecondsRemaining - deltaTime);
                GameEvents.RaiseShieldTimeChanged(_shieldSecondsRemaining);
            }

            if (IsInSurvivalMode)
            {
                // Coasting at 1%: the clock counts up, but passive drain can no longer kill you here.
                SurvivalModeSeconds += deltaTime;
                return;
            }

            if (_config.PassiveDrainPerSecond > 0f)
            {
                _pendingCause = RunEndCause.RanOutOfCharge;
                ApplyDelta(-_config.PassiveDrainPerSecond * deltaTime);

                if (!IsDead && CurrentPercent <= SurvivalModeThresholdPercent)
                {
                    EnterSurvivalMode();
                }
            }
        }

        private void EnterSurvivalMode()
        {
            IsInSurvivalMode = true;
            float previous = CurrentPercent;
            CurrentPercent = SurvivalModeThresholdPercent;
            if (!Mathf.Approximately(previous, CurrentPercent))
            {
                GameEvents.RaiseBatteryChanged(CurrentPercent, CurrentPercent - previous);
            }
            GameEvents.RaiseSurvivalModeEntered();
        }

        public void ApplyHit()
        {
            if (IsDead || IsInvulnerable) return;

            GameEvents.RaiseObstacleHit();
            _pendingCause = RunEndCause.HitObstacle;

            if (_config.OneHitKnockout)
            {
                ApplyDelta(-CurrentPercent);
                return;
            }

            ApplyDelta(-_config.HitDamagePercent);
            if (!IsDead)
            {
                if (CurrentPercent <= SurvivalModeThresholdPercent && !IsInSurvivalMode)
                {
                    EnterSurvivalMode();
                }
                GrantShield(postHitInvulnerabilitySeconds);
            }
        }

        /// <summary>
        /// Falling into a Pit is an unconditional fall, not a graduated hit - unlike ApplyHit, this
        /// ignores Shield/invulnerability and the mode's HitDamagePercent/OneHitKnockout config (a Pit
        /// deals the same fate in every mode). It still ends the run through the exact same
        /// ApplyDelta -> IsDead/DeathCause path every other death already uses, and still raises
        /// ObstacleHit so the existing impact feedback (haptics/SFX/camera shake) plays.
        /// </summary>
        public void FallIntoPit()
        {
            if (IsDead) return;

            GameEvents.RaiseObstacleHit();
            _pendingCause = RunEndCause.FellInPit;
            ApplyDelta(-CurrentPercent);
        }

        public void RestoreEnergy(float amountPercent)
        {
            if (IsDead) return;
            IsInSurvivalMode = false;
            ApplyDelta(amountPercent);
            GameEvents.RaiseEnergyCollected(amountPercent);
        }

        public void GrantShield(float seconds)
        {
            if (IsDead) return;
            _shieldSecondsRemaining = Mathf.Max(_shieldSecondsRemaining, seconds);
            GameEvents.RaiseShieldTimeChanged(_shieldSecondsRemaining);
        }

        /// <summary>Ad-based revive: refill to a partial charge and grant a fresh grace window (defaults to the normal post-hit shield length).</summary>
        public void Revive(float refillPercent, float invulnerabilitySeconds = -1f)
        {
            IsDead = false;
            IsInSurvivalMode = false;
            CurrentPercent = Mathf.Clamp(refillPercent, 1f, 100f);
            GrantShield(invulnerabilitySeconds >= 0f ? invulnerabilitySeconds : postHitInvulnerabilitySeconds);
            GameEvents.RaiseBatteryChanged(CurrentPercent, 0f);
        }

        private void ApplyDelta(float delta)
        {
            float previous = CurrentPercent;
            CurrentPercent = Mathf.Clamp(previous + delta, 0f, 100f);
            GameEvents.RaiseBatteryChanged(CurrentPercent, CurrentPercent - previous);

            if (CurrentPercent <= 0f && !IsDead)
            {
                IsDead = true;
                DeathCause = _pendingCause;
            }
        }
    }
}

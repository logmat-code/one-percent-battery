using System.Collections.Generic;
using OnePercentBattery.Core;
using OnePercentBattery.Spawning;
using UnityEngine;

namespace OnePercentBattery.VFX
{
    /// <summary>
    /// Central one-shot "juice" layer for pickups, a blocked Shield hit, landings, cleared pits and
    /// the instant of death. Reacts to GameEvents only - nothing in gameplay code calls into this, and
    /// this never calls into gameplay code - and owns two small reusable pools (see
    /// PooledParticleEffect/PooledPulseEffect) instead of an Instantiate/Destroy per cue. Colors match
    /// the existing palette (Charger green, Magnet red, Shield purple, danger red).
    /// </summary>
    public sealed class GameplayVfxController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private PooledParticleEffect burstPrefab;
        [SerializeField] private PooledPulseEffect pulsePrefab;
        [SerializeField] private int burstPrewarm = 4;
        [SerializeField] private int pulsePrewarm = 3;

        [Header("Colors")]
        [SerializeField] private Color coinColor = new Color(1f, 0.85f, 0.25f);
        [SerializeField] private Color chargerColor = new Color(0.25f, 0.95f, 0.4f);
        [SerializeField] private Color magnetColor = new Color(0.95f, 0.2f, 0.25f);
        [SerializeField] private Color shieldColor = new Color(0.65f, 0.3f, 0.95f);
        [SerializeField] private Color shieldBlockColor = new Color(1f, 0.95f, 0.55f);
        [SerializeField] private Color landingDustColor = new Color(0.75f, 0.7f, 0.6f);
        [SerializeField] private Color pitClearColor = new Color(0.3f, 0.9f, 0.9f);
        [SerializeField] private Color obstacleDeathColor = new Color(1f, 0.4f, 0.15f);
        [SerializeField] private Color pitDeathColor = new Color(0.45f, 0.45f, 0.55f);

        [Header("Landing feedback")]
        [Tooltip("Minimum real air time since the last Jumped before Landed plays a dust burst - filters out a theoretical near-zero hop, even though every real jump this game can produce (min tap airtime ~0.6s) clears it easily.")]
        [SerializeField] private float minAirTimeForLandingDust = 0.15f;

        private ObjectPool<PooledParticleEffect> _burstPool;
        private ObjectPool<PooledPulseEffect> _pulsePool;
        private readonly List<PooledParticleEffect> _activeBursts = new List<PooledParticleEffect>();
        private readonly List<PooledPulseEffect> _activePulses = new List<PooledPulseEffect>();

        private float _lastJumpTime = -999f;
        private bool _pendingPitClearEffect;

        private void Awake()
        {
            _burstPool = new ObjectPool<PooledParticleEffect>(burstPrefab, transform, burstPrewarm);
            _pulsePool = new ObjectPool<PooledPulseEffect>(pulsePrefab, transform, pulsePrewarm);
        }

        private void OnEnable()
        {
            GameEvents.PickupCollected += HandlePickupCollected;
            GameEvents.ShieldBlockedHit += HandleShieldBlockedHit;
            GameEvents.PitCleared += HandlePitCleared;
            GameEvents.Jumped += HandleJumped;
            GameEvents.Landed += HandleLanded;
            GameEvents.RunAwaitingContinue += HandleRunAwaitingContinue;
            GameEvents.RunStarted += HandleRunStarted;
        }

        private void OnDisable()
        {
            GameEvents.PickupCollected -= HandlePickupCollected;
            GameEvents.ShieldBlockedHit -= HandleShieldBlockedHit;
            GameEvents.PitCleared -= HandlePitCleared;
            GameEvents.Jumped -= HandleJumped;
            GameEvents.Landed -= HandleLanded;
            GameEvents.RunAwaitingContinue -= HandleRunAwaitingContinue;
            GameEvents.RunStarted -= HandleRunStarted;
        }

        private void HandlePickupCollected(PickupType type, Vector3 position)
        {
            switch (type)
            {
                case PickupType.Coin:
                    PlayBurst(position, coinColor);
                    break;
                case PickupType.Energy:
                    PlayBurst(position, chargerColor);
                    if (player != null) PlayPulse(player.position, chargerColor, player);
                    break;
                case PickupType.Magnet:
                    PlayBurst(position, magnetColor);
                    if (player != null) PlayPulse(player.position, magnetColor, player);
                    break;
                case PickupType.Shield:
                    PlayBurst(position, shieldColor);
                    if (player != null) PlayPulse(player.position, shieldColor, player);
                    break;
            }
        }

        /// <summary>PlayerController raises this only when Battery.IsInvulnerable was already true at the moment of an Obstacle collision - a purely cosmetic reaction to that, never a second damage/consequence path.</summary>
        private void HandleShieldBlockedHit(Vector3 position)
        {
            PlayBurst(position, shieldBlockColor);
            PlayPulse(position, shieldBlockColor, player);
        }

        private void HandlePitCleared() => _pendingPitClearEffect = true;

        private void HandleJumped() => _lastJumpTime = Time.time;

        /// <summary>
        /// A pending pit-clear always wins over the plain landing dust for this same Landed - it is the
        /// SAME landing (see PlayerController.SetOverPit/PitCleared), just a different, more special cue.
        /// </summary>
        private void HandleLanded()
        {
            if (_pendingPitClearEffect)
            {
                _pendingPitClearEffect = false;
                if (player != null) PlayPulse(player.position, pitClearColor, null);
                return;
            }

            if (player != null && Time.time - _lastJumpTime >= minAirTimeForLandingDust)
            {
                PlayBurst(player.position, landingDustColor);
            }
        }

        /// <summary>
        /// Fires immediately inside GameManager.HandleDeath (see RunAwaitingContinue), unlike RunEnded -
        /// which only fires later, when Retry/Home is actually tapped, and is what the existing
        /// CameraFollow hit/death shake and AudioManager death SFX are anchored to. Deliberately not
        /// changed here (see the final report) - this only adds a cosmetic burst at the true instant of
        /// death, on a signal that already carries the same RunResult.Cause those other systems use.
        /// </summary>
        private void HandleRunAwaitingContinue(RunResult preview, bool canOfferSecondChance)
        {
            if (player == null) return;
            switch (preview.Cause)
            {
                case RunEndCause.HitObstacle:
                    PlayBurst(player.position, obstacleDeathColor);
                    break;
                case RunEndCause.FellInPit:
                    // Deliberately smaller/muted than the Obstacle case - the point is to not obscure
                    // the player's own fallen sprite, which is the only "fall animation" that exists.
                    PlayBurst(player.position, pitDeathColor);
                    break;
            }
        }

        private void HandleRunStarted(RunModeType mode)
        {
            _pendingPitClearEffect = false;
            _lastJumpTime = -999f;

            for (int i = 0; i < _activeBursts.Count; i++)
            {
                _activeBursts[i].StopImmediately();
                _burstPool.Release(_activeBursts[i]);
            }
            _activeBursts.Clear();

            for (int i = 0; i < _activePulses.Count; i++)
            {
                _activePulses[i].StopImmediately();
                _pulsePool.Release(_activePulses[i]);
            }
            _activePulses.Clear();
        }

        private void PlayBurst(Vector3 position, Color color)
        {
            PooledParticleEffect fx = _burstPool.Get();
            fx.Configure(ReleaseBurst);
            fx.Play(position, color);
            _activeBursts.Add(fx);
        }

        private void PlayPulse(Vector3 position, Color color, Transform followTarget)
        {
            PooledPulseEffect fx = _pulsePool.Get();
            fx.Configure(ReleasePulse);
            fx.Play(position, color, followTarget);
            _activePulses.Add(fx);
        }

        private void ReleaseBurst(PooledParticleEffect fx)
        {
            _activeBursts.Remove(fx);
            _burstPool.Release(fx);
        }

        private void ReleasePulse(PooledPulseEffect fx)
        {
            _activePulses.Remove(fx);
            _pulsePool.Release(fx);
        }
    }
}

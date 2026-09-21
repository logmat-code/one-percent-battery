using System;
using System.Collections.Generic;
using OnePercentBattery.Data;
using OnePercentBattery.Player;
using UnityEngine;

namespace OnePercentBattery.Spawning
{
    /// <summary>
    /// Procedurally places obstacles, pits and pickups ahead of the player and recycles them once they
    /// scroll behind. Accepts an external System.Random so the Daily Run can seed it from today's date
    /// and give every player the same course ("A fresh course every day").
    ///
    /// Fairness: the gap in front of every hazard is computed from the player's OWN jump physics at the
    /// CURRENT speed (see <see cref="JumpPhysics"/>), not a fixed meter constant. As the run speeds up,
    /// a fixed-meter gap would take less and less time to cross - eventually less than a human reaction
    /// time - and would stop being reachable by even a full-hold jump. Recomputing it every spawn keeps
    /// every hazard clearable by a correctly-timed jump with reaction time to spare, at any speed.
    ///
    /// Sequencing: the spawner looks one spot AHEAD of what it is about to place (<see cref="_pendingPlan"/>)
    /// so <see cref="DynamicMinGap"/> can size the gap using BOTH the hazard it just placed and the one
    /// coming right after it - e.g. widening the approach to a Pit without widening every plain-obstacle
    /// gap - and so a roll that would repeat the tallest obstacle back-to-back can be substituted for a
    /// shorter one deterministically, with no reroll loop.
    /// </summary>
    public sealed class CourseSpawner : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Obstacle[] obstaclePrefabs;
        [SerializeField] private PitZone[] pitPrefabs;
        [SerializeField] private MovingObstacle[] movingObstaclePrefabs;
        [SerializeField] private Pickup coinPrefab;
        [SerializeField] private Pickup energyPrefab;
        [SerializeField] private Pickup shieldPrefab;
        [SerializeField] private Pickup magnetPrefab;

        [Header("Layout")]
        [SerializeField] private float groundY = 0f;
        [SerializeField] private float minGapMeters = 6f;
        [SerializeField] private float maxGapVarianceMeters = 5f;
        [SerializeField] private float lookAheadMeters = 20f;
        [SerializeField] private float despawnBehindMeters = 15f;
        [SerializeField] private float firstSpawnBufferMeters = 12f;

        [Header("Fairness")]
        [Tooltip("Minimum time a player must be able to see a hazard before it's reachable, at any speed.")]
        [SerializeField] private float reactionTimeSeconds = 0.4f;
        [Tooltip("Safety multiplier applied to the raw tap-jump distance when sizing the minimum gap.")]
        [SerializeField] private float jumpClearanceMargin = 1.25f;
        [Tooltip("Extra recovery time (converted to meters via the current speed, like every other distance here) inserted AFTER a demanding hazard - the Tall obstacle or a Medium Pit - before the next one. Not applied after every hazard, only these two.")]
        [SerializeField] private float landingRecoverySeconds = 0.35f;

        [Header("Spawn weights (must sum to <= 1; remainder is a plain coin cluster)")]
        [SerializeField] private float obstacleWeight = 0.55f;
        [Tooltip("Ground-gap hazard. Kept deliberately occasional (unlike obstacles, this is not gated by PowerUpsEnabled - it's a level hazard, not a power-up - but it is rarer than a plain obstacle) so the course doesn't feel like constant pits.")]
        [SerializeField] private float pitWeight = 0.08f;
        [Tooltip("Periodic vertical hazard (see MovingObstacle) - a level hazard like Pit, not gated by PowerUpsEnabled. Kept rarer than Pit while this first timing-based variant is still being validated.")]
        [SerializeField] private float movingObstacleWeight = 0.05f;
        [Tooltip("The battery-life resource - a bit more frequent than Magnet/Shield so it reads as a lifeline, but tuned down from 0.14 which felt too common in real play.")]
        [SerializeField] private float energyWeight = 0.07f;
        [SerializeField] private float shieldWeight = 0.05f;
        [Tooltip("Tuned down from 0.10 which felt too common in real play - Magnet should read as occasional/special, rarer than the Charger.")]
        [SerializeField] private float magnetSpawnChance = 0.05f;

        private enum SpawnKind { Obstacle, Pit, MovingObstacle, Energy, Shield, Magnet, CoinCluster }

        private struct SpawnPlan
        {
            public SpawnKind Kind;
            public int Index;
            public float Width;
            public bool DemandsLandingRecovery;
            public ObstacleHeightClass HeightClass;
        }

        private ObjectPool<Obstacle>[] _obstaclePools;
        private ObjectPool<PitZone>[] _pitPools;
        private ObjectPool<MovingObstacle>[] _movingObstaclePools;
        private ObjectPool<Pickup> _coinPool;
        private ObjectPool<Pickup> _energyPool;
        private ObjectPool<Pickup> _shieldPool;
        private ObjectPool<Pickup> _magnetPool;

        private float[] _obstacleWidths;
        private float[] _pitWidths;
        private float[] _movingObstacleWidths;
        private int _mediumObstacleIndex = -1;
        private int _lowObstacleIndex = -1;

        private readonly List<Transform> _active = new List<Transform>();
        private readonly Dictionary<Transform, Action> _releaseByTransform = new Dictionary<Transform, Action>();

        private System.Random _rng;
        private RunModeConfig _config;
        private float _pendingX;
        private SpawnPlan _pendingPlan;
        private ObstacleHeightClass _lastHeightClass;
        private bool _lastWasMovingObstacle;
        private bool _running;
        private PlayerController _playerController;
        private JumpProfile _jumpProfile;

        private void Awake()
        {
            _obstaclePools = new ObjectPool<Obstacle>[obstaclePrefabs.Length];
            _obstacleWidths = new float[obstaclePrefabs.Length];
            for (int i = 0; i < obstaclePrefabs.Length; i++)
            {
                _obstaclePools[i] = new ObjectPool<Obstacle>(obstaclePrefabs[i], transform, prewarm: 2);
                _obstacleWidths[i] = FootprintWidth(obstaclePrefabs[i].GetComponent<BoxCollider2D>(), obstaclePrefabs[i].transform);

                // Cached once so a roll that lands on Tall right after Tall can be substituted in O(1) -
                // no reroll loop. Medium is preferred (matches the "one tier down" spirit); Low is the
                // fallback only if this build genuinely has no Medium-classed prefab assigned.
                if (obstaclePrefabs[i].HeightClass == ObstacleHeightClass.Medium && _mediumObstacleIndex < 0)
                    _mediumObstacleIndex = i;
                if (obstaclePrefabs[i].HeightClass == ObstacleHeightClass.Low && _lowObstacleIndex < 0)
                    _lowObstacleIndex = i;
            }

            _pitPools = new ObjectPool<PitZone>[pitPrefabs.Length];
            _pitWidths = new float[pitPrefabs.Length];
            for (int i = 0; i < pitPrefabs.Length; i++)
            {
                _pitPools[i] = new ObjectPool<PitZone>(pitPrefabs[i], transform, prewarm: 1);
                _pitWidths[i] = FootprintWidth(pitPrefabs[i].GetComponent<BoxCollider2D>(), pitPrefabs[i].transform);
            }

            _movingObstaclePools = new ObjectPool<MovingObstacle>[movingObstaclePrefabs.Length];
            _movingObstacleWidths = new float[movingObstaclePrefabs.Length];
            for (int i = 0; i < movingObstaclePrefabs.Length; i++)
            {
                _movingObstaclePools[i] = new ObjectPool<MovingObstacle>(movingObstaclePrefabs[i], transform, prewarm: 1);
                _movingObstacleWidths[i] = FootprintWidth(movingObstaclePrefabs[i].GetComponent<BoxCollider2D>(), movingObstaclePrefabs[i].transform);
            }

            _coinPool = new ObjectPool<Pickup>(coinPrefab, transform, prewarm: 8);
            _energyPool = new ObjectPool<Pickup>(energyPrefab, transform, prewarm: 2);
            _shieldPool = new ObjectPool<Pickup>(shieldPrefab, transform, prewarm: 1);
            _magnetPool = new ObjectPool<Pickup>(magnetPrefab, transform, prewarm: 1);

            _playerController = player.GetComponent<PlayerController>();
            _jumpProfile = _playerController.ComputeJumpAirTimes();
        }

        private static float FootprintWidth(BoxCollider2D col, Transform t)
        {
            return col != null ? col.size.x * t.localScale.x : 0f;
        }

        /// <summary>
        /// Minimum safe gap (meters) at the given speed: reachable by a tap-jump with reaction time to
        /// spare, PLUS the wider of the two hazards on either side of this gap (see FootprintWidth) so
        /// that margin describes the actual clear air between their edges, not just the distance between
        /// spawn positions, PLUS a recovery buffer if the hazard just placed was a demanding one (Tall
        /// obstacle / Medium Pit). `speed` must be the real effective horizontal speed the player will be
        /// moving at (PlayerController.CurrentSpeed already folds in the battery-driven GameSpeedMultiplier) -
        /// passing anything lower under-sizes the gap for how fast the player is actually going.
        /// </summary>
        private float DynamicMinGap(float speed, SpawnPlan justPlaced, SpawnPlan upcoming)
        {
            float jumpDistance = _jumpProfile.MinDistanceAt(speed) * jumpClearanceMargin;
            float reactionDistance = speed * reactionTimeSeconds;
            float baseGap = Mathf.Max(minGapMeters, Mathf.Max(jumpDistance, reactionDistance));
            float widthTerm = Mathf.Max(justPlaced.Width, upcoming.Width);
            float recoveryTerm = justPlaced.DemandsLandingRecovery ? speed * landingRecoverySeconds : 0f;
            return baseGap + widthTerm + recoveryTerm;
        }

        public void BeginRun(RunModeConfig config, System.Random rng)
        {
            _config = config;
            _rng = rng ?? new System.Random();
            ClearActive();
            _lastHeightClass = ObstacleHeightClass.Low;
            _lastWasMovingObstacle = false;
            _pendingPlan = RollSpawnPlan();
            _pendingX = player.position.x + firstSpawnBufferMeters;
            _running = true;
        }

        public void StopRun()
        {
            _running = false;
        }

        /// <summary>Continues spawning after a Second-Chance revive without resetting pools or the upcoming layout.</summary>
        public void ResumeRun()
        {
            _running = true;
        }

        private void ClearActive()
        {
            foreach (var t in _active)
            {
                _releaseByTransform[t]?.Invoke();
            }
            _active.Clear();
            _releaseByTransform.Clear();
        }

        private void Update()
        {
            if (!_running) return;

            float horizon = player.position.x + lookAheadMeters;
            while (_pendingX < horizon)
            {
                Execute(_pendingPlan, _pendingX);

                SpawnPlan next = RollSpawnPlan();
                float speed = _playerController.CurrentSpeed;
                float gap = DynamicMinGap(speed, _pendingPlan, next) + (float)_rng.NextDouble() * maxGapVarianceMeters;
                _pendingX += gap;
                _pendingPlan = next;
            }

            float despawnX = player.position.x - despawnBehindMeters;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Transform t = _active[i];
                if (t.position.x < despawnX)
                {
                    _releaseByTransform[t]?.Invoke();
                    _releaseByTransform.Remove(t);
                    _active.RemoveAt(i);
                }
            }
        }

        private SpawnPlan RollSpawnPlan()
        {
            double roll = _rng.NextDouble();

            if (roll < obstacleWeight)
            {
                return RollObstaclePlan();
            }
            roll -= obstacleWeight;

            // Pits are a level hazard, not a power-up - unlike Energy/Shield/Magnet below, this roll is
            // NOT gated by _config.PowerUpsEnabled, so Pro mode still sees pits (same as it still sees
            // obstacles above).
            if (roll < pitWeight)
            {
                return RollPitPlan();
            }
            roll -= pitWeight;

            // Same reasoning as Pit above: a level hazard, not a power-up, so unconditional - except right
            // after another Moving Obstacle (see _lastWasMovingObstacle/Execute): two independently-rolled
            // phases placed back-to-back could combine into a timing puzzle neither one was individually
            // designed to be fair against, so a roll landing here falls through to the next bracket instead,
            // deterministically - no reroll loop, mirroring how Tall-after-Tall substitutes above rather
            // than re-rolling.
            if (roll < movingObstacleWeight && !_lastWasMovingObstacle)
            {
                return RollMovingObstaclePlan();
            }
            roll -= movingObstacleWeight;

            if (_config.PowerUpsEnabled && roll < energyWeight)
            {
                return new SpawnPlan { Kind = SpawnKind.Energy };
            }
            if (_config.PowerUpsEnabled) roll -= energyWeight;

            if (_config.PowerUpsEnabled && roll < shieldWeight)
            {
                return new SpawnPlan { Kind = SpawnKind.Shield };
            }
            if (_config.PowerUpsEnabled) roll -= shieldWeight;

            if (_config.PowerUpsEnabled && roll < magnetSpawnChance)
            {
                return new SpawnPlan { Kind = SpawnKind.Magnet };
            }

            return new SpawnPlan { Kind = SpawnKind.CoinCluster };
        }

        /// <summary>
        /// If this roll would repeat the tallest obstacle right after another Tall one, it is
        /// deterministically downgraded (Medium, else Low, else left as-is if this build has neither
        /// classified) instead of rerolling - a fixed, bounded substitution that can never loop.
        /// </summary>
        private SpawnPlan RollObstaclePlan()
        {
            int index = _rng.Next(obstaclePrefabs.Length);
            ObstacleHeightClass cls = obstaclePrefabs[index].HeightClass;

            if (cls == ObstacleHeightClass.Tall && _lastHeightClass == ObstacleHeightClass.Tall)
            {
                int substitute = _mediumObstacleIndex >= 0 ? _mediumObstacleIndex : _lowObstacleIndex;
                if (substitute >= 0)
                {
                    index = substitute;
                    cls = obstaclePrefabs[index].HeightClass;
                }
            }

            return new SpawnPlan
            {
                Kind = SpawnKind.Obstacle,
                Index = index,
                Width = _obstacleWidths[index],
                HeightClass = cls,
                DemandsLandingRecovery = cls == ObstacleHeightClass.Tall,
            };
        }

        private SpawnPlan RollPitPlan()
        {
            int index = _rng.Next(pitPrefabs.Length);
            bool isMedium = pitPrefabs[index].SizeClass == PitSizeClass.Medium;
            return new SpawnPlan
            {
                Kind = SpawnKind.Pit,
                Index = index,
                Width = _pitWidths[index],
                DemandsLandingRecovery = isMedium,
            };
        }

        private SpawnPlan RollMovingObstaclePlan()
        {
            int index = _rng.Next(movingObstaclePrefabs.Length);
            return new SpawnPlan
            {
                Kind = SpawnKind.MovingObstacle,
                Index = index,
                Width = _movingObstacleWidths[index],
                // Same "demanding" tier as Tall/Medium Pit - this alone is what gives Moving<->Tall and
                // Moving<->Pit their extra spacing in both directions (see DynamicMinGap: it already adds
                // recovery after WHICHEVER hazard just placed demands it, regardless of what follows -
                // so Moving needing recovery covers "Moving->Tall"/"Moving->MediumPit", and Tall/MediumPit
                // already demanding it covers "Tall->Moving"/"MediumPit->Moving". No new spacing code,
                // and no change to any pairing that doesn't involve Moving.
                DemandsLandingRecovery = true,
            };
        }

        private void Execute(SpawnPlan plan, float x)
        {
            switch (plan.Kind)
            {
                case SpawnKind.Obstacle:
                    SpawnObstacle(plan.Index, x);
                    _lastHeightClass = plan.HeightClass;
                    break;
                case SpawnKind.Pit:
                    SpawnPit(plan.Index, x);
                    break;
                case SpawnKind.MovingObstacle:
                    SpawnMovingObstacle(plan.Index, x);
                    break;
                case SpawnKind.Energy:
                    SpawnPickup(_energyPool, x, groundY + 1.1f);
                    break;
                case SpawnKind.Shield:
                    SpawnPickup(_shieldPool, x, groundY + 1.1f);
                    break;
                case SpawnKind.Magnet:
                    SpawnPickup(_magnetPool, x, groundY + 1.1f);
                    break;
                case SpawnKind.CoinCluster:
                    SpawnCoinCluster(x);
                    break;
            }
            _lastWasMovingObstacle = plan.Kind == SpawnKind.MovingObstacle;
        }

        private void SpawnObstacle(int index, float x)
        {
            ObjectPool<Obstacle> pool = _obstaclePools[index];
            Obstacle instance = pool.Get();
            instance.transform.position = new Vector3(x, groundY + instance.GroundOffsetY, 0f);
            Track(instance.transform, () => pool.Release(instance));
        }

        private void SpawnPit(int index, float x)
        {
            ObjectPool<PitZone> pool = _pitPools[index];
            PitZone instance = pool.Get();
            instance.transform.position = new Vector3(x, groundY, 0f);
            Track(instance.transform, () => pool.Release(instance));
        }

        /// <summary>
        /// Picks between the block's two "safe" extremes (fully low = a Medium-height obstacle; fully
        /// high = a clear passage underneath) and solves the phase that puts it EXACTLY there at the
        /// moment the player is expected to arrive - not a fixed/hardcoded phase. The player's own X and
        /// current speed give timeToReach directly; solving phase0 = target - AngularFrequency*timeToReach
        /// guarantees the target regardless of how far ahead this particular spawn landed within the
        /// look-ahead window. Both targets sit at a peak/trough of the sine (derivative ~0), which is
        /// deliberate: it makes the outcome robust to the same kind of speed-estimate drift
        /// DynamicMinGap already tolerates elsewhere (the player's real speed at arrival can differ
        /// slightly from the speed read here at spawn time) - near a peak/trough a small phase error only
        /// changes the height by a second-order amount, unlike near the sine's zero-crossings.
        /// </summary>
        private void SpawnMovingObstacle(int index, float x)
        {
            ObjectPool<MovingObstacle> pool = _movingObstaclePools[index];
            MovingObstacle instance = pool.Get();

            float distanceAhead = x - player.position.x;
            float speed = Mathf.Max(0.01f, _playerController.CurrentSpeed);
            float timeToReach = distanceAhead / speed;
            float angularFrequency = movingObstaclePrefabs[index].AngularFrequency;
            float arrivalPhaseIfZeroStart = angularFrequency * timeToReach;

            float targetArrivalPhase = _rng.NextDouble() < 0.5 ? MovingObstacle.LowestPhase : MovingObstacle.HighestPhase;
            float phase0 = targetArrivalPhase - arrivalPhaseIfZeroStart;
            // x is handed in directly (this spawn's fixed basePosition) rather than set on the Transform
            // first and read back - see MovingObstacle.ResetForSpawn/_baseX.
            instance.ResetForSpawn(x, phase0);

            Track(instance.transform, () => pool.Release(instance));
        }

        private void SpawnPickup(ObjectPool<Pickup> pool, float x, float y)
        {
            Pickup instance = pool.Get();
            instance.transform.position = new Vector3(x, y, 0f);
            instance.OnCollected = OnPickupCollected;
            instance.SetPlayer(_playerController);
            Track(instance.transform, () => ReleasePickup(pool, instance));
        }

        private void SpawnCoinCluster(float x)
        {
            int count = 3 + _rng.Next(3); // 3-5 coins, echoing the small diamond/arc patterns in the source game
            float spacing = 1.1f;
            for (int i = 0; i < count; i++)
            {
                Pickup coin = _coinPool.Get();
                float arcHeight = Mathf.Sin((i + 1) / (float)(count + 1) * Mathf.PI) * 1.6f;
                coin.transform.position = new Vector3(x + i * spacing, groundY + 0.6f + arcHeight, 0f);
                coin.OnCollected = OnPickupCollected;
                coin.SetPlayer(_playerController);
                Pickup capturedCoin = coin;
                Track(coin.transform, () => ReleasePickup(_coinPool, capturedCoin));
            }
        }

        private void OnPickupCollected(Pickup pickup)
        {
            // Collected pickups are released immediately rather than waiting to scroll off-screen.
            Transform t = pickup.transform;
            if (_releaseByTransform.TryGetValue(t, out Action release))
            {
                release();
                _releaseByTransform.Remove(t);
                _active.Remove(t);
            }
        }

        private void ReleasePickup(ObjectPool<Pickup> pool, Pickup instance)
        {
            instance.OnCollected = null;
            pool.Release(instance);
        }

        private void Track(Transform t, Action release)
        {
            _active.Add(t);
            _releaseByTransform[t] = release;
        }
    }
}

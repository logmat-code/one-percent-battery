using System;
using System.Collections.Generic;
using OnePercentBattery.Data;
using OnePercentBattery.Player;
using UnityEngine;

namespace OnePercentBattery.Spawning
{
    /// <summary>
    /// Procedurally places obstacles and pickups ahead of the player and recycles them once they
    /// scroll behind. Accepts an external System.Random so the Daily Run can seed it from today's
    /// date and give every player the same course ("A fresh course every day").
    ///
    /// Fairness: the gap in front of every obstacle is computed from the player's OWN jump physics
    /// at the CURRENT speed (see <see cref="JumpPhysics"/>), not a fixed meter constant. As the run
    /// speeds up, a fixed-meter gap would take less and less time to cross - eventually less than a
    /// human reaction time - and would stop being reachable by even a full-hold jump. Recomputing it
    /// every spawn keeps every obstacle clearable by a tap-jump with reaction time to spare, at any
    /// speed the config allows.
    /// </summary>
    public sealed class CourseSpawner : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Obstacle[] obstaclePrefabs;
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
        [Tooltip("Minimum time a player must be able to see an obstacle before it's reachable, at any speed.")]
        [SerializeField] private float reactionTimeSeconds = 0.4f;
        [Tooltip("Safety multiplier applied to the raw tap-jump distance when sizing the minimum gap.")]
        [SerializeField] private float jumpClearanceMargin = 1.25f;

        [Header("Spawn weights (must sum to <= 1; remainder is a plain coin cluster)")]
        [SerializeField] private float obstacleWeight = 0.55f;
        [Tooltip("The battery-life resource - a bit more frequent than Magnet/Shield so it reads as a lifeline, but tuned down from 0.14 which felt too common in real play.")]
        [SerializeField] private float energyWeight = 0.07f;
        [SerializeField] private float shieldWeight = 0.05f;
        [Tooltip("Tuned down from 0.10 which felt too common in real play - Magnet should read as occasional/special, rarer than the Charger.")]
        [SerializeField] private float magnetSpawnChance = 0.05f;

        private ObjectPool<Obstacle>[] _obstaclePools;
        private ObjectPool<Pickup> _coinPool;
        private ObjectPool<Pickup> _energyPool;
        private ObjectPool<Pickup> _shieldPool;
        private ObjectPool<Pickup> _magnetPool;

        private readonly List<Transform> _active = new List<Transform>();
        private readonly Dictionary<Transform, Action> _releaseByTransform = new Dictionary<Transform, Action>();

        private System.Random _rng;
        private RunModeConfig _config;
        private float _nextSpawnX;
        private bool _running;
        private PlayerController _playerController;
        private JumpProfile _jumpProfile;
        private float _maxObstacleWidthMeters;

        private void Awake()
        {
            _obstaclePools = new ObjectPool<Obstacle>[obstaclePrefabs.Length];
            for (int i = 0; i < obstaclePrefabs.Length; i++)
            {
                _obstaclePools[i] = new ObjectPool<Obstacle>(obstaclePrefabs[i], transform, prewarm: 2);

                // DynamicMinGap spaces spawn POSITIONS as if obstacles were points, but a spike/pillar
                // has real width - without this, the actual clear air between two obstacles' edges was
                // narrower than the fairness math assumed, worst-case when both slots roll an obstacle.
                var col = obstaclePrefabs[i].GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    float width = col.size.x * obstaclePrefabs[i].transform.localScale.x;
                    _maxObstacleWidthMeters = Mathf.Max(_maxObstacleWidthMeters, width);
                }
            }
            _coinPool = new ObjectPool<Pickup>(coinPrefab, transform, prewarm: 8);
            _energyPool = new ObjectPool<Pickup>(energyPrefab, transform, prewarm: 2);
            _shieldPool = new ObjectPool<Pickup>(shieldPrefab, transform, prewarm: 1);
            _magnetPool = new ObjectPool<Pickup>(magnetPrefab, transform, prewarm: 1);

            _playerController = player.GetComponent<PlayerController>();
            _jumpProfile = _playerController.ComputeJumpAirTimes();
        }

        /// <summary>
        /// Minimum safe gap (meters) at the given speed: reachable by a tap-jump with reaction time to
        /// spare, PLUS the widest obstacle's own footprint (see Awake) so that margin describes the
        /// actual clear air between two obstacles' edges, not just the distance between their spawn
        /// positions. `speed` must be the real effective horizontal speed the player will be moving at
        /// (PlayerController.CurrentSpeed already folds in the battery-driven GameSpeedMultiplier) -
        /// passing anything lower under-sizes the gap for how fast the player is actually going.
        /// </summary>
        private float DynamicMinGap(float speed)
        {
            float jumpDistance = _jumpProfile.MinDistanceAt(speed) * jumpClearanceMargin;
            float reactionDistance = speed * reactionTimeSeconds;
            return Mathf.Max(minGapMeters, Mathf.Max(jumpDistance, reactionDistance)) + _maxObstacleWidthMeters;
        }

        public void BeginRun(RunModeConfig config, System.Random rng)
        {
            // TEMP PROFILING - remove after investigation.
            var _swTotal = System.Diagnostics.Stopwatch.StartNew();
            int _activeCountBefore = _active.Count;
            long _allocBefore = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();

            _config = config;
            _rng = rng ?? new System.Random();

            var _swClear = System.Diagnostics.Stopwatch.StartNew();
            ClearActive();
            _swClear.Stop();

            _nextSpawnX = player.position.x + firstSpawnBufferMeters;
            _running = true;

            long _allocAfter = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            _swTotal.Stop();
            UnityEngine.Debug.Log("[TempProfile] CourseSpawner.BeginRun activeCountBefore=" + _activeCountBefore
                + " ClearActiveMs=" + _swClear.Elapsed.TotalMilliseconds.ToString("F3")
                + " TotalMs=" + _swTotal.Elapsed.TotalMilliseconds.ToString("F3")
                + " allocBytes=" + (_allocAfter - _allocBefore));
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
            while (_nextSpawnX < horizon)
            {
                SpawnAt(_nextSpawnX);
                float dynamicMinGap = DynamicMinGap(_playerController.CurrentSpeed);
                _nextSpawnX += dynamicMinGap + (float)_rng.NextDouble() * maxGapVarianceMeters;
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

        private void SpawnAt(float x)
        {
            double roll = _rng.NextDouble();

            if (roll < obstacleWeight)
            {
                SpawnObstacle(x);
                return;
            }
            roll -= obstacleWeight;

            if (_config.PowerUpsEnabled && roll < energyWeight)
            {
                SpawnPickup(_energyPool, x, groundY + 1.1f);
                return;
            }
            if (_config.PowerUpsEnabled) roll -= energyWeight;

            if (_config.PowerUpsEnabled && roll < shieldWeight)
            {
                SpawnPickup(_shieldPool, x, groundY + 1.1f);
                return;
            }
            if (_config.PowerUpsEnabled) roll -= shieldWeight;

            if (_config.PowerUpsEnabled && roll < magnetSpawnChance)
            {
                SpawnPickup(_magnetPool, x, groundY + 1.1f);
                return;
            }

            SpawnCoinCluster(x);
        }

        private void SpawnObstacle(float x)
        {
            int index = _rng.Next(obstaclePrefabs.Length);
            ObjectPool<Obstacle> pool = _obstaclePools[index];
            Obstacle instance = pool.Get();
            instance.transform.position = new Vector3(x, groundY + instance.GroundOffsetY, 0f);
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

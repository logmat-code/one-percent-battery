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
    ///
    /// Micro-patterns: <see cref="RollNextPlan"/> occasionally (see patternChance) substitutes a single
    /// independent roll with a short 2-3 element <see cref="HazardPatternDefinition"/> instead (queued in
    /// <see cref="_patternQueue"/> and drained one element per iteration of the same loop) - a designed
    /// rhythm like "Tall then Medium" rather than pure chance. This is layered ON the existing roll/gap
    /// machinery, not a parallel system: a pattern element still goes through the same pools, the same
    /// DynamicMinGap call (see the IsPatternElement branch below), and - for MovingObstacle/Pit - the same
    /// fairness math as any independently-rolled hazard. It only ever occupies slots that would otherwise
    /// have held independent rolls, so it never inflates the total amount of content along the course.
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

        [Header("Micro-patterns")]
        [Tooltip("Chance, each time the spawner is about to decide its next content, that it attempts a short designed sequence (see patterns) instead of an independent random roll. An attempt only succeeds if the cooldown (minNormalSlotsBetweenPatterns) has elapsed, a pattern other than the last one used is available, and every element in it maps to a prefab this build actually has - otherwise it silently falls back to a normal roll for that same slot.")]
        [SerializeField] private float patternChance = 0.15f;
        [Tooltip("Normal (non-pattern) slots required between the end of one pattern and the start of the next - keeps the course from reading as a string of prefabricated segments back-to-back.")]
        [SerializeField] private int minNormalSlotsBetweenPatterns = 3;
        [SerializeField] private HazardPatternDefinition[] patterns = DefaultPatterns();

        private const float PatternCoinClusterWidth = 4.5f; // conservative upper bound for a 5-coin SpawnCoinCluster (4 gaps * 1.1m spacing) - reserves real room via DynamicMinGap so a pattern's coins never land inside a neighboring hazard's collider.

        private enum SpawnKind { Obstacle, Pit, MovingObstacle, Energy, Shield, Magnet, CoinCluster }

        private struct SpawnPlan
        {
            public SpawnKind Kind;
            public int Index;
            public float Width;
            public bool DemandsLandingRecovery;
            public ObstacleHeightClass HeightClass;
            public bool IsPatternElement;
            public float DesiredExtraSpacingBefore;
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
        private int _tallObstacleIndex = -1;
        private int _smallPitIndex = -1;
        private int _mediumPitIndex = -1;

        private readonly List<Transform> _active = new List<Transform>();
        private readonly Dictionary<Transform, Action> _releaseByTransform = new Dictionary<Transform, Action>();
        private readonly Queue<SpawnPlan> _patternQueue = new Queue<SpawnPlan>();

        private System.Random _rng;
        private RunModeConfig _config;
        private float _pendingX;
        private SpawnPlan _pendingPlan;
        private ObstacleHeightClass _lastHeightClass;
        private bool _lastWasMovingObstacle;
        private int _lastPatternIndex = -1;
        private int _normalSlotsSincePattern;
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
                // Also cached purely for micro-patterns (see BuildPatternElementPlan), which need to name a
                // SPECIFIC height class rather than roll a random index - the normal random roll never reads this.
                if (obstaclePrefabs[i].HeightClass == ObstacleHeightClass.Tall && _tallObstacleIndex < 0)
                    _tallObstacleIndex = i;
            }

            _pitPools = new ObjectPool<PitZone>[pitPrefabs.Length];
            _pitWidths = new float[pitPrefabs.Length];
            for (int i = 0; i < pitPrefabs.Length; i++)
            {
                _pitPools[i] = new ObjectPool<PitZone>(pitPrefabs[i], transform, prewarm: 1);
                _pitWidths[i] = FootprintWidth(pitPrefabs[i].GetComponent<BoxCollider2D>(), pitPrefabs[i].transform);

                // Same reasoning as the obstacle indices above - only micro-patterns need a specific size class.
                if (pitPrefabs[i].SizeClass == PitSizeClass.Small && _smallPitIndex < 0)
                    _smallPitIndex = i;
                if (pitPrefabs[i].SizeClass == PitSizeClass.Medium && _mediumPitIndex < 0)
                    _mediumPitIndex = i;
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
            _patternQueue.Clear();
            _lastPatternIndex = -1;
            _normalSlotsSincePattern = 0;
            _pendingPlan = RollNextPlan();
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

                SpawnPlan next = RollNextPlan();
                float speed = _playerController.CurrentSpeed;
                float dynamicGap = DynamicMinGap(speed, _pendingPlan, next);
                // A pattern-internal element (see RollNextPlan/TryStartPattern) uses ONLY the safe dynamic
                // minimum plus whatever extra the pattern itself asked for - never LESS than DynamicMinGap,
                // and never the added random variance a normal roll gets, so the sequence reads as a single
                // deliberate beat rather than the usual loose spacing. Element 0 of a pattern is not marked
                // (see TryStartPattern) - the gap INTO a pattern still behaves like any other normal transition.
                float gap = next.IsPatternElement
                    ? Mathf.Max(dynamicGap, next.DesiredExtraSpacingBefore)
                    : dynamicGap + (float)_rng.NextDouble() * maxGapVarianceMeters;
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
            return BuildObstaclePlan(index, _lastHeightClass);
        }

        /// <summary>Shared by the random roll above and micro-patterns (see BuildPatternElementPlan) - `lastHeightClass` is a plain parameter rather than reading `_lastHeightClass` directly so a pattern can simulate its OWN internal progression before committing to placing anything.</summary>
        private SpawnPlan BuildObstaclePlan(int index, ObstacleHeightClass lastHeightClass)
        {
            ObstacleHeightClass cls = obstaclePrefabs[index].HeightClass;

            if (cls == ObstacleHeightClass.Tall && lastHeightClass == ObstacleHeightClass.Tall)
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
            return BuildPitPlan(index);
        }

        private SpawnPlan BuildPitPlan(int index)
        {
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
            return BuildMovingObstaclePlan(index);
        }

        private SpawnPlan BuildMovingObstaclePlan(int index)
        {
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

        /// <summary>
        /// The single entry point RollSpawnPlan's old call sites now use: drains a micro-pattern already in
        /// progress one element at a time, otherwise mostly defers to the normal random roll - a pattern
        /// attempt only replaces THIS ONE call's result, so it can never add extra content beyond what an
        /// independent roll would have occupied anyway.
        /// </summary>
        private SpawnPlan RollNextPlan()
        {
            if (_patternQueue.Count > 0)
            {
                return _patternQueue.Dequeue();
            }

            if (_normalSlotsSincePattern >= minNormalSlotsBetweenPatterns && _rng.NextDouble() < patternChance)
            {
                SpawnPlan? started = TryStartPattern();
                if (started != null)
                {
                    return started.Value;
                }
            }

            _normalSlotsSincePattern++;
            return RollSpawnPlan();
        }

        /// <summary>
        /// Picks a pattern (never the same one used last - see _lastPatternIndex) and resolves every one of
        /// its elements against this build's actual prefabs/pools, simulating the Tall-Tall and
        /// Moving-Moving fairness rules across the pattern's OWN elements as it goes (seeded from the real
        /// _lastHeightClass/_lastWasMovingObstacle, so a pattern starting right after a Tall or a Moving
        /// Obstacle still gets the same protection a random roll would have). If ANY element can't be
        /// resolved (a size class this build has no prefab for, or a Moving element that would illegally
        /// follow another one), the whole attempt is discarded and nothing is committed - RollNextPlan falls
        /// back to a normal roll for this slot, so a partially-invalid pattern never leaves the course in an
        /// inconsistent state.
        /// </summary>
        private SpawnPlan? TryStartPattern()
        {
            if (patterns == null || patterns.Length == 0) return null;

            int chosenIndex = ChoosePatternIndex();
            if (chosenIndex < 0) return null;

            HazardPatternDefinition def = patterns[chosenIndex];
            if (def.Elements == null || def.Elements.Length == 0) return null;

            var built = new List<SpawnPlan>(def.Elements.Length);
            ObstacleHeightClass simulatedLastHeight = _lastHeightClass;
            bool simulatedLastMoving = _lastWasMovingObstacle;

            foreach (PatternElementKind kind in def.Elements)
            {
                SpawnPlan? maybePlan = BuildPatternElementPlan(kind, ref simulatedLastHeight, ref simulatedLastMoving);
                if (maybePlan == null) return null;
                built.Add(maybePlan.Value);
            }

            // Element 0 behaves exactly like a normal roll for the gap LEADING INTO the pattern (see
            // Update()) - only elements 1+ are marked so their gap uses the pattern's own tighter spacing.
            for (int i = 1; i < built.Count; i++)
            {
                SpawnPlan element = built[i];
                element.IsPatternElement = true;
                element.DesiredExtraSpacingBefore = def.DesiredExtraSpacingMeters;
                _patternQueue.Enqueue(element);
            }

            _lastHeightClass = simulatedLastHeight;
            _lastWasMovingObstacle = simulatedLastMoving;
            _lastPatternIndex = chosenIndex;
            _normalSlotsSincePattern = 0;

            return built[0];
        }

        private int ChoosePatternIndex()
        {
            float totalWeight = 0f;
            for (int i = 0; i < patterns.Length; i++)
            {
                if (i == _lastPatternIndex) continue; // anti-repeat: never the same pattern twice in a row
                totalWeight += Mathf.Max(0f, patterns[i].Weight);
            }
            if (totalWeight <= 0f) return -1;

            float roll = (float)_rng.NextDouble() * totalWeight;
            for (int i = 0; i < patterns.Length; i++)
            {
                if (i == _lastPatternIndex) continue;
                float w = Mathf.Max(0f, patterns[i].Weight);
                if (roll < w) return i;
                roll -= w;
            }
            return -1;
        }

        /// <summary>
        /// Resolves one pattern element against this build's cached prefab indices. MovingObstacle's own
        /// phase (HIGH/LOW) is deliberately NOT decided here - see SpawnMovingObstacle, which every queued
        /// MovingObstacle plan still passes through unchanged, computing the safe phase from the real
        /// distance/speed at the moment it's actually placed, exactly like an independently-rolled one.
        /// </summary>
        private SpawnPlan? BuildPatternElementPlan(PatternElementKind kind, ref ObstacleHeightClass simulatedLastHeight, ref bool simulatedLastMoving)
        {
            switch (kind)
            {
                case PatternElementKind.Low:
                case PatternElementKind.Medium:
                case PatternElementKind.Tall:
                {
                    int index = kind == PatternElementKind.Tall ? _tallObstacleIndex
                        : kind == PatternElementKind.Medium ? _mediumObstacleIndex
                        : _lowObstacleIndex;
                    if (index < 0) return null;

                    SpawnPlan plan = BuildObstaclePlan(index, simulatedLastHeight);
                    simulatedLastHeight = plan.HeightClass;
                    simulatedLastMoving = false;
                    return plan;
                }
                case PatternElementKind.SmallPit:
                case PatternElementKind.MediumPit:
                {
                    int index = kind == PatternElementKind.MediumPit ? _mediumPitIndex : _smallPitIndex;
                    if (index < 0) return null;

                    simulatedLastMoving = false;
                    return BuildPitPlan(index);
                }
                case PatternElementKind.MovingObstacle:
                {
                    if (movingObstaclePrefabs.Length == 0) return null;
                    // Mirrors the same rule RollSpawnPlan enforces for independent rolls (see
                    // _lastWasMovingObstacle there) - a pattern must never be the loophole that places two
                    // Moving Obstacles back-to-back, whether both are its own elements or its first element
                    // happens to land right after a Moving Obstacle the normal roll just placed.
                    if (simulatedLastMoving) return null;

                    int index = _rng.Next(movingObstaclePrefabs.Length);
                    simulatedLastMoving = true;
                    return BuildMovingObstaclePlan(index);
                }
                case PatternElementKind.Coins:
                {
                    simulatedLastMoving = false;
                    return new SpawnPlan
                    {
                        Kind = SpawnKind.CoinCluster,
                        Width = PatternCoinClusterWidth,
                    };
                }
                default:
                    return null;
            }
        }

        private static HazardPatternDefinition[] DefaultPatterns() => new[]
        {
            new HazardPatternDefinition { Name = "P1 Warmup", Elements = new[] { PatternElementKind.Low, PatternElementKind.Coins, PatternElementKind.Low }, Weight = 1f },
            new HazardPatternDefinition { Name = "P2 StepDown", Elements = new[] { PatternElementKind.Tall, PatternElementKind.Medium }, Weight = 1f },
            new HazardPatternDefinition { Name = "P3 JumpReward", Elements = new[] { PatternElementKind.SmallPit, PatternElementKind.Coins }, Weight = 1f },
            new HazardPatternDefinition { Name = "P4 TimingReward", Elements = new[] { PatternElementKind.MovingObstacle, PatternElementKind.Coins }, Weight = 1f },
            new HazardPatternDefinition { Name = "P5 Mixed", Elements = new[] { PatternElementKind.Low, PatternElementKind.SmallPit }, Weight = 1f },
            new HazardPatternDefinition { Name = "P6 Recovery", Elements = new[] { PatternElementKind.MediumPit, PatternElementKind.Coins, PatternElementKind.Medium }, Weight = 1f },
            new HazardPatternDefinition { Name = "P7 TallReward", Elements = new[] { PatternElementKind.Tall, PatternElementKind.Coins, PatternElementKind.Low }, Weight = 1f },
            new HazardPatternDefinition { Name = "P8 TimingCombo", Elements = new[] { PatternElementKind.MovingObstacle, PatternElementKind.Coins, PatternElementKind.SmallPit }, Weight = 1f },
        };

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

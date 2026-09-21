using UnityEngine;

namespace OnePercentBattery.Spawning
{
    /// <summary>
    /// A simple periodic vertical hazard - continuously rises and falls on a sine wave, read by timing
    /// rather than by size. Damage reuses the exact same pipeline as any other obstacle: this GameObject
    /// also carries a plain <see cref="Obstacle"/> component (added as a sibling on the same prefab), and
    /// PlayerController's existing OnTriggerEnter2D -> Obstacle -> Battery.ApplyHit() path picks it up
    /// completely unchanged - no separate damage system. The trigger collider lives on this same root
    /// Transform (not a child), so moving this Transform each frame moves the collider and the sprite
    /// together automatically - there is nothing else that needs to stay in sync.
    ///
    /// Position is recomputed directly from elapsed time every tick (baseY + amplitude*sin(...)), never
    /// accumulated via Translate, so there is no drift no matter how long an instance stays alive across
    /// the pool's rotation.
    ///
    /// Moved via a Kinematic Rigidbody2D's MovePosition in FixedUpdate, exactly like PlayerController's own
    /// body - a bare static collider whose Transform is reassigned directly every frame does not reliably
    /// generate Unity 2D trigger callbacks even when a genuine overlap occurs (see PlayerController.Awake()'s
    /// own comment on kinematic-vs-static pairs not producing triggers - the player itself is Dynamic for the
    /// same reason). Requiring the Rigidbody2D here mirrors that, and rotation is frozen since nothing here
    /// ever needs to spin.
    ///
    /// X never drifts: <see cref="_baseX"/> is captured once per spawn (see ResetForSpawn) and every tick
    /// re-reads that same stored value rather than the Rigidbody2D's current position, so there is no way
    /// for horizontal error to accumulate no matter how long an instance stays alive between pool rotations.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class MovingObstacle : MonoBehaviour
    {
        [Tooltip("Center of the oscillation. Together with Amplitude this sets the range: lowest = baseY-amplitude, highest = baseY+amplitude. Chosen so baseY-amplitude stays exactly 0.6 (the LOW extreme's top edge stays at 1.2 - identical to the Medium obstacle) while baseY+amplitude pushes the HIGH extreme's bottom edge above even a FULL-HOLD jump's apex, with a real safety margin - see amplitude's tooltip for the exact clearance math.")]
        [SerializeField] private float baseY = 2.35f;
        [Tooltip("How far above/below baseY the block travels. At the default baseY/amplitude: LOW top edge = 1.2 (unchanged, same as Medium); HIGH bottom edge = 3.5. Measured with real colliders, a full-hold jump's apex puts the player's head at pivot 2.914 + half-height 0.425 = 3.339, so HIGH's 3.5 bottom edge clears it by ~0.16m (within the requested 0.10-0.20m safety margin) - an unnecessary jump, tap OR full-hold, while the block is safely high is never punished, only skipping a jump while it's low still is.")]
        [SerializeField] private float amplitude = 1.75f;
        [Tooltip("Seconds for one full down-up-down cycle. Kept slow on purpose for this first version so the motion is easy to read at a glance - not meant to be the fast/aggressive variant.")]
        [SerializeField] private float oscillationPeriodSeconds = 3f;
        [Tooltip("Phase (radians) used only if this instance is never handed a spawn-time phase via ResetForSpawn - e.g. a hand-placed instance in a test scene. Real gameplay spawns always get a fairness-computed phase from CourseSpawner instead.")]
        [SerializeField] private float startPhase = LowestPhase;

        /// <summary>Sine argument at the lowest point of the cycle - the block sits flush with the ground here. CourseSpawner targets this when it wants "arrives low, needs a jump" (same clearance as the Medium obstacle).</summary>
        public const float LowestPhase = -Mathf.PI / 2f;
        /// <summary>Sine argument at the highest point of the cycle - a safely-clear passage underneath. CourseSpawner targets this when it wants "arrives high, safe to run through without jumping."</summary>
        public const float HighestPhase = Mathf.PI / 2f;

        /// <summary>Radians/second - CourseSpawner reads this to solve for the spawn-time phase that lands on a target phase exactly when the player is due to arrive (see ResetForSpawn).</summary>
        public float AngularFrequency => 2f * Mathf.PI / oscillationPeriodSeconds;

        private Rigidbody2D _rigidbody;
        private float _baseX;
        private float _phase;
        private float _elapsed;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.gravityScale = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _baseX = transform.position.x;
            _phase = startPhase;
        }

        /// <summary>
        /// Called by CourseSpawner right after pulling this instance from its pool, with the X it has
        /// already chosen and the phase it already solved for fairness. Stores X as this spawn's fixed
        /// basePosition - every later FixedUpdate tick recomputes position from THIS stored value and the
        /// clock reset below, never from wherever the Rigidbody2D/Transform currently happen to be - and
        /// hard-teleports both the Rigidbody2D and the Transform immediately (mirroring
        /// PlayerController.BeginRun's Body.position= teleport pattern, as opposed to Step's per-tick
        /// MovePosition), so a reused instance never continues visually - or physically - from wherever a
        /// previous appearance left off.
        /// </summary>
        public void ResetForSpawn(float x, float phase)
        {
            _baseX = x;
            _phase = phase;
            _elapsed = 0f;

            float y = ComputeY();
            _rigidbody.position = new Vector2(_baseX, y);
            transform.position = new Vector3(_baseX, y, transform.position.z);
        }

        private void FixedUpdate()
        {
            _elapsed += Time.fixedDeltaTime;
            _rigidbody.MovePosition(new Vector2(_baseX, ComputeY()));
        }

        private float ComputeY()
        {
            return baseY + amplitude * Mathf.Sin(AngularFrequency * _elapsed + _phase);
        }
    }
}

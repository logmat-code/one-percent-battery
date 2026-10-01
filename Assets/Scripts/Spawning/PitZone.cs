using OnePercentBattery.Data;
using OnePercentBattery.Player;
using UnityEngine;

namespace OnePercentBattery.Spawning
{
    /// <summary>
    /// Marker + ground-state notifier for a ground-gap hazard. This does NOT deal a hit or kill the
    /// player itself - it only tells PlayerController "there is no ground under you right now"
    /// (SetOverPit) whenever the player's effective CENTER X is within its horizontal span, regardless
    /// of their Y. What happens as a result (falling instead of being clamped to groundY, and dying only
    /// after falling far enough - see PlayerController.Step/pitFallDeathDepth) lives entirely in
    /// PlayerController, the same way "is there ground here" already worked before Pits existed.
    ///
    /// The ground in this project has no physical collider anywhere (PlayerController integrates its
    /// own Y and clamps to a hardcoded groundY); it is purely a scrolling visual. So this GameObject's
    /// own BoxCollider2D stands in for "the hole a real floor collider would have here" - sized only to
    /// the pit's own WIDTH, but tall enough (see the prefab) to always contain the player vertically
    /// regardless of jump height or fall depth, so overlap only ever depends on the player's X, never
    /// their Y. See CourseSpawner for how width/spacing is derived.
    ///
    /// Real-device bug this fixes: a player whose body was still mostly over solid ground (just grazing
    /// the pit's near edge with their leading side) would trigger "over pit" the instant ANY overlap
    /// began - a fast, wide player collider only needs to poke a few centimeters into the pit's collider
    /// to flip this, well before they're meaningfully over the hole. Since a grounded SetOverPit(true)
    /// starts PlayerController's 0.1s coyote grace toward a committed fall, a player with no visual reason
    /// to jump (they can still see solid ground under most of their body) could get sucked into a fall.
    /// Gating on the player's CENTER X, re-checked every physics tick via OnTriggerStay2D (not just once
    /// on Enter), means the pit only takes over once the player is genuinely, meaningfully over the
    /// opening - and immediately releases them the moment their center is back over solid ground - without
    /// touching PlayerController's own commitment/coyote/death-depth logic at all.
    /// </summary>
    public sealed class PitZone : MonoBehaviour
    {
        [SerializeField] private PitSizeClass sizeClass = PitSizeClass.Small;

        private Collider2D _collider;

        public PitSizeClass SizeClass => sizeClass;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
        }

        private void OnTriggerEnter2D(Collider2D other) => UpdateOverlap(other);

        private void OnTriggerStay2D(Collider2D other) => UpdateOverlap(other);

        private void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null) player.SetOverPit(false);
        }

        private void UpdateOverlap(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player == null) return;

            float playerCenterX = other.bounds.center.x;
            bool meaningfullyOverPit = playerCenterX >= _collider.bounds.min.x && playerCenterX <= _collider.bounds.max.x;
            player.SetOverPit(meaningfullyOverPit);
        }
    }
}

using OnePercentBattery.Data;
using OnePercentBattery.Player;
using UnityEngine;

namespace OnePercentBattery.Spawning
{
    /// <summary>
    /// Marker + ground-state notifier for a ground-gap hazard. This does NOT deal a hit or kill the
    /// player itself - it only tells PlayerController "there is no ground under you right now"
    /// (SetOverPit) whenever the player's X is within its horizontal span, regardless of their Y. What
    /// happens as a result (falling instead of being clamped to groundY, and dying only after falling
    /// far enough - see PlayerController.Step/pitFallDeathDepth) lives entirely in PlayerController,
    /// the same way "is there ground here" already worked before Pits existed.
    ///
    /// The ground in this project has no physical collider anywhere (PlayerController integrates its
    /// own Y and clamps to a hardcoded groundY); it is purely a scrolling visual. So this GameObject's
    /// own BoxCollider2D stands in for "the hole a real floor collider would have here" - sized only to
    /// the pit's own WIDTH, but tall enough (see the prefab) to always contain the player vertically
    /// regardless of jump height or fall depth, so Enter/Exit only ever fires on the player's X crossing
    /// the pit's horizontal edges, never on their Y. See CourseSpawner for how width/spacing is derived.
    /// </summary>
    public sealed class PitZone : MonoBehaviour
    {
        [SerializeField] private PitSizeClass sizeClass = PitSizeClass.Small;

        public PitSizeClass SizeClass => sizeClass;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null) player.SetOverPit(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null) player.SetOverPit(false);
        }
    }
}

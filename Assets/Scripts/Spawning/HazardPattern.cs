using UnityEngine;

namespace OnePercentBattery.Spawning
{
    /// <summary>Gameplay-relevant kinds a micro-pattern element can request - see <see cref="HazardPatternDefinition"/>. Deliberately excludes power-ups (Magnet/Charger/Shield): those keep depending only on the normal roll.</summary>
    public enum PatternElementKind
    {
        Low,
        Medium,
        Tall,
        SmallPit,
        MediumPit,
        MovingObstacle,
        Coins,
    }

    /// <summary>
    /// A short, intentional 2-3 element sequence CourseSpawner occasionally places instead of an
    /// independent random roll (see CourseSpawner.patternChance/TryStartPattern) - e.g. "Tall then
    /// Medium" reads as a small designed rhythm rather than coincidence. A pattern only decides WHICH
    /// kinds appear in WHAT order: every element still goes through the exact same pools, the exact same
    /// DynamicMinGap spacing math, and (for MovingObstacle/Pit) the exact same fairness rules as a normal
    /// roll - a pattern never gets to be less safe than a random one would have been.
    /// </summary>
    [System.Serializable]
    public struct HazardPatternDefinition
    {
        public string Name;
        public PatternElementKind[] Elements;
        [Tooltip("Relative weight among patterns when a pattern roll already succeeded (see CourseSpawner.patternChance) - this is a share of the occasional pattern slot, not a share of all spawns.")]
        public float Weight;
        [Tooltip("Extra spacing (meters) this pattern would LIKE between its own elements, on top of whatever DynamicMinGap already requires. 0 means \"exactly the safe minimum, with none of a normal roll's added random variance\" - tighter and more consistent than a normal gap, but never less safe: DynamicMinGap always wins if it asks for more than this.")]
        public float DesiredExtraSpacingMeters;
    }
}

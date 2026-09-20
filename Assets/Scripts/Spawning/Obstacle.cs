using UnityEngine;

namespace OnePercentBattery.Spawning
{
    /// <summary>
    /// Marker component only: every obstacle deals the same mode-defined damage (see RunModeConfig /
    /// PlayerBattery.ApplyHit), so the different prefabs (spike trap, pillar) exist purely for
    /// silhouette/height variety, not different rules.
    /// </summary>
    public sealed class Obstacle : MonoBehaviour
    {
        [Tooltip("Half the visual height - lets the spawner place different obstacle heights (spike vs. pillar) so they all sit flush on the ground line.")]
        [SerializeField] private float groundOffsetY = 0.5f;

        public float GroundOffsetY => groundOffsetY;
    }
}

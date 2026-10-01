using UnityEngine;

namespace OnePercentBattery.UI
{
    /// <summary>
    /// Continuous, self-contained horizontal scroll for one Main Menu background layer (skyline, fog,
    /// foreground lights...). Deliberately NOT a reuse of <see cref="World.ParallaxLayer"/> itself:
    /// that component derives its movement from a followed gameplay camera and resets on
    /// GameEvents.RunStarted, and the menu must animate with neither a camera to track nor any
    /// gameplay-run lifecycle to hook into. This borrows only the concept (pre-placed, touching tiles
    /// recycled forward once they scroll past the left edge) with a plain constant scroll instead.
    ///
    /// Uses unscaledDeltaTime so the menu keeps breathing even if something sets Time.timeScale=0
    /// (e.g. a paused overlay drawn on top). Moves existing tile transforms only - no
    /// Instantiate/Destroy, no per-frame allocations.
    /// </summary>
    public sealed class MenuBackgroundScroller : MonoBehaviour
    {
        [Tooltip("World units per second, positive = scrolls left (the classic runner direction). Vary this per layer (skyline slow, foreground lights faster) to fake depth without a moving camera.")]
        [SerializeField] private float scrollSpeed = 0.6f;
        [Tooltip("Width of one tile - tiles must be pre-placed left-to-right, touching, with this spacing.")]
        [SerializeField] private float tileWidth = 20f;
        [SerializeField] private Transform[] tiles;

        private float _leftmostX;

        private void Awake()
        {
            CacheLeftmostX();
        }

        private void CacheLeftmostX()
        {
            if (tiles == null || tiles.Length == 0) return;
            _leftmostX = tiles[0].position.x;
            for (int i = 1; i < tiles.Length; i++)
            {
                if (tiles[i].position.x < _leftmostX) _leftmostX = tiles[i].position.x;
            }
        }

        private void Update()
        {
            if (tiles == null || tiles.Length == 0) return;

            float step = scrollSpeed * Time.unscaledDeltaTime;
            float recycleDistance = tileWidth * tiles.Length;
            float recycleThreshold = _leftmostX - tileWidth;

            for (int i = 0; i < tiles.Length; i++)
            {
                Vector3 pos = tiles[i].position;
                pos.x -= step;
                if (pos.x < recycleThreshold)
                {
                    pos.x += recycleDistance;
                }
                tiles[i].position = pos;
            }
        }
    }
}

using UnityEngine;

namespace OnePercentBattery.World
{
    /// <summary>
    /// Reused for every scrolling layer, ground included. <see cref="parallaxFactor"/> is how much of
    /// the camera's own movement this layer copies: 0 stays fixed in world space (ground, obstacles'
    /// visual backdrop - scrolls past at full speed), 1 tracks the camera exactly (appears static, for
    /// a distant sky). Values in between give the classic multi-layer skyline depth.
    /// Children under this transform are the repeating tiles, pre-placed left-to-right and touching.
    /// </summary>
    public sealed class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [Range(0f, 1f)]
        [SerializeField] private float parallaxFactor = 0.5f;
        [SerializeField] private float tileWidth = 20f;
        [SerializeField] private Transform[] tiles;

        private Vector3 _lastCameraPosition;

        public void Initialize(Transform trackedCamera)
        {
            cameraTransform = trackedCamera;
            _lastCameraPosition = cameraTransform.position;
        }

        private void Start()
        {
            if (cameraTransform != null)
            {
                _lastCameraPosition = cameraTransform.position;
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform == null || tiles == null || tiles.Length == 0) return;

            float deltaX = cameraTransform.position.x - _lastCameraPosition.x;
            if (parallaxFactor > 0f)
            {
                transform.position += Vector3.right * (deltaX * parallaxFactor);
            }
            _lastCameraPosition = cameraTransform.position;

            float recycleDistance = tileWidth * tiles.Length;
            foreach (var tile in tiles)
            {
                if (tile.position.x - cameraTransform.position.x < -tileWidth)
                {
                    tile.position += Vector3.right * recycleDistance;
                }
            }
        }
    }
}

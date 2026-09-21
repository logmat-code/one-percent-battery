using OnePercentBattery.Core;
using OnePercentBattery.Data;
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
        private Vector3 _initialRootPosition;
        private Vector3[] _initialTilePositions;
        private bool _pendingRunStartReset;

        private void Awake()
        {
            // The recycle logic below only ever slides a tile FORWARD (by a whole recycleDistance)
            // when it falls behind the camera - the layout a fresh Let's Run shows is exactly this
            // component's scene-authored starting arrangement, so caching it once here (before any
            // gameplay has had a chance to move anything) gives HandleRunStarted an exact, known-good
            // pose to restore rather than trying to derive one from tileWidth/count math.
            _initialRootPosition = transform.position;
            if (tiles != null)
            {
                _initialTilePositions = new Vector3[tiles.Length];
                for (int i = 0; i < tiles.Length; i++)
                {
                    _initialTilePositions[i] = tiles[i].position;
                }
            }
        }

        private void OnEnable()
        {
            GameEvents.RunStarted += HandleRunStarted;
        }

        private void OnDisable()
        {
            GameEvents.RunStarted -= HandleRunStarted;
        }

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

        /// <summary>
        /// The forward-only recycle below has no way to catch up when the camera jumps BACKWARD by
        /// more than one recycleDistance in a single frame - exactly what Retry/Let's Run-after-death
        /// does when the previous run went any real distance (measured: a run to x=300 left every
        /// tile stranded 100-330 units ahead of the new camera position, forever - the per-frame check
        /// only ever slides a tile by one recycleDistance, so it could never fully close a gap that
        /// large). Restoring the exact scene-authored layout on every run start sidesteps the recycle
        /// math entirely instead of trying to make it handle an arbitrarily large jump.
        ///
        /// This only flags the reset rather than applying it here, because RunStarted fires while
        /// CameraFollow's own handler (on the same event) may not have run yet - reading
        /// cameraTransform.position at this point could still see its pre-teleport pose depending on
        /// subscription order. LateUpdate is the actual reset point specifically because every
        /// RunStarted subscriber (including CameraFollow's, which snaps the camera synchronously
        /// during the event) has already run by the time ANY component's LateUpdate fires that same
        /// frame - so by then cameraTransform.position is guaranteed final, regardless of subscription
        /// order between the two.
        /// </summary>
        private void HandleRunStarted(RunModeType mode)
        {
            _pendingRunStartReset = true;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null || tiles == null || tiles.Length == 0) return;

            if (_pendingRunStartReset)
            {
                _pendingRunStartReset = false;
                transform.position = _initialRootPosition;
                for (int i = 0; i < tiles.Length; i++)
                {
                    tiles[i].position = _initialTilePositions[i];
                }
                // Camera is already at its final post-teleport position by this point (see summary
                // above), so this is the correct baseline for next frame's delta - not the stale
                // pre-Retry position RunStarted fired alongside.
                _lastCameraPosition = cameraTransform.position;
                return;
            }

            float deltaX = cameraTransform.position.x - _lastCameraPosition.x;
            if (parallaxFactor > 0f)
            {
                transform.position += Vector3.right * (deltaX * parallaxFactor);
            }
            _lastCameraPosition = cameraTransform.position;

            // A while (not if): normal per-frame camera movement only ever needs at most one
            // recycle, so this costs nothing in the common case, but it also means an unexpectedly
            // large forward jump - not just the backward Retry teleport RunStarted now handles above -
            // can't leave a tile permanently stranded needing more than one recycleDistance to catch up.
            float recycleDistance = tileWidth * tiles.Length;
            foreach (var tile in tiles)
            {
                while (tile.position.x - cameraTransform.position.x < -tileWidth)
                {
                    tile.position += Vector3.right * recycleDistance;
                }
            }
        }
    }
}

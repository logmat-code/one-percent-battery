using OnePercentBattery.Core;
using OnePercentBattery.Data;
using UnityEngine;

namespace OnePercentBattery.World
{
    /// <summary>
    /// Keeps the runner a fixed fraction from the left edge, matching the source game's static
    /// horizon. The horizontal offset is computed from the camera's own orthographic size and
    /// aspect (not a hardcoded world-unit constant), so the framing holds up across the many
    /// screen aspect ratios real phones ship with.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Range(0.1f, 0.5f)]
        [SerializeField] private float horizontalBiasFromLeft = 0.27f;
        [SerializeField] private float fixedY = 2f;
        [SerializeField] private float smoothTime = 0.08f;

        [Header("Impact feedback")]
        [SerializeField] private float hitShakeDuration = 0.22f;
        [SerializeField] private float hitShakeMagnitude = 0.18f;
        [SerializeField] private float deathShakeDuration = 0.35f;
        [SerializeField] private float deathShakeMagnitude = 0.3f;
        [SerializeField] private float survivalShakeDuration = 0.25f;
        [SerializeField] private float survivalShakeMagnitude = 0.2f;

        private Camera _camera;
        private float _velocityX;
        private float _shakeTimer;
        private float _shakeDuration;
        private float _shakeMagnitude;
        private Vector2 _shakeOffset;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            GameEvents.ObstacleHit += HandleObstacleHit;
            GameEvents.SurvivalModeEntered += HandleSurvivalModeEntered;
            GameEvents.RunEnded += HandleRunEnded;
            GameEvents.RunStarted += HandleRunStarted;
        }

        private void OnDisable()
        {
            GameEvents.ObstacleHit -= HandleObstacleHit;
            GameEvents.SurvivalModeEntered -= HandleSurvivalModeEntered;
            GameEvents.RunEnded -= HandleRunEnded;
            GameEvents.RunStarted -= HandleRunStarted;
        }

        public void SetTarget(Transform newTarget) => target = newTarget;

        public void Shake(float duration, float magnitude)
        {
            if (duration <= _shakeTimer && magnitude <= _shakeMagnitude) return;
            _shakeTimer = duration;
            _shakeDuration = duration;
            _shakeMagnitude = magnitude;
        }

        private void HandleObstacleHit() => Shake(hitShakeDuration, hitShakeMagnitude);
        private void HandleSurvivalModeEntered() => Shake(survivalShakeDuration, survivalShakeMagnitude);

        private void HandleRunEnded(RunResult result)
        {
            if (result.Cause == RunEndCause.HitObstacle || result.Cause == RunEndCause.FellInPit)
            {
                Shake(deathShakeDuration, deathShakeMagnitude);
            }
        }

        /// <summary>
        /// FinalizeRun() (Retry/Home) raises RunEnded first, which on a HitObstacle death queues the
        /// shake above - and Retry in particular can reach Playing again in the very same frame (no
        /// interstitial due), well inside the shake's duration. Without this, the new run's first
        /// frame visibly inherited the previous run's death shake instead of opening on a settled
        /// camera.
        ///
        /// Separately (measured, not assumed): BeginRun teleports the player back to the start line,
        /// but nothing previously told this camera about that jump. Whatever real distance the player
        /// had covered before dying, the camera was still sitting there - SmoothDamp doesn't (and
        /// shouldn't) close a large gap in one frame, so for however many frames it took to catch up
        /// (measured up to ~15 frames after a ~20m run), the player was rendered off the left edge of
        /// the screen entirely - the new run visibly opened with an empty screen, not a repositioned
        /// character. Since target.position is already the new start position by the time RunStarted
        /// fires, snapping straight there (and zeroing the SmoothDamp velocity so the old run's
        /// momentum doesn't fight the next frame) makes every run open framed exactly like a fresh
        /// Let's Run, regardless of how far the previous run went.
        /// </summary>
        private void HandleRunStarted(RunModeType mode)
        {
            _shakeTimer = 0f;
            _shakeOffset = Vector2.zero;

            if (target != null)
            {
                var pos = transform.position;
                pos.x = DesiredX();
                pos.y = fixedY;
                transform.position = pos;
            }
            _velocityX = 0f;
        }

        private float DesiredX()
        {
            float halfWidth = _camera.orthographicSize * _camera.aspect;
            float xOffset = halfWidth * (1f - 2f * horizontalBiasFromLeft);
            return target.position.x + xOffset;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            var pos = transform.position;
            pos.x = Mathf.SmoothDamp(pos.x, DesiredX(), ref _velocityX, smoothTime);
            pos.y = fixedY;

            if (_shakeTimer > 0f)
            {
                _shakeTimer = Mathf.Max(0f, _shakeTimer - Time.deltaTime);
                float falloff = _shakeDuration > 0f ? _shakeTimer / _shakeDuration : 0f;
                _shakeOffset = Random.insideUnitCircle * (_shakeMagnitude * falloff);
            }
            else
            {
                _shakeOffset = Vector2.zero;
            }

            pos.x += _shakeOffset.x;
            pos.y += _shakeOffset.y;
            transform.position = pos;
        }
    }
}

using OnePercentBattery.Core;
using OnePercentBattery.Data;
using OnePercentBattery.Save;
using OnePercentBattery.Spawning;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OnePercentBattery.Player
{
    /// <summary>
    /// Auto-runner movement: constant forward motion (ramps up with distance) plus a jump with
    /// variable height - tap for a short hop, hold for a higher one - matching the source game's
    /// "Tap or Space to jump - Hold to go higher" prompt.
    /// </summary>
    [RequireComponent(typeof(PlayerBattery))]
    [RequireComponent(typeof(PlayerMagnet))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Jump")]
        [SerializeField] private float minJumpVelocity = 9f;
        [SerializeField] private float maxJumpVelocity = 15f;
        [SerializeField] private float maxHoldSeconds = 0.28f;
        [SerializeField] private float gravity = -30f;
        [SerializeField] private float groundY = 0f;
        [SerializeField] private float practiceModeSpeedMultiplier = 0.7f;

        [Header("Forgiveness (make the input feel fair, not just accurate)")]
        [Tooltip("A jump pressed shortly after walking off a ledge still fires.")]
        [SerializeField] private float coyoteTimeSeconds = 0.1f;
        [Tooltip("A jump pressed shortly before landing is remembered and fires on landing.")]
        [SerializeField] private float jumpBufferSeconds = 0.1f;

        [Header("1% Survival Mode")]
        [Tooltip("Extra forward-speed multiplier while the battery is coasting at 1% (see PlayerBattery.IsInSurvivalMode).")]
        [SerializeField] private float survivalModeSpeedMultiplier = 1.15f;

        public float CurrentSpeed { get; private set; }
        public bool IsGrounded { get; private set; } = true;
        public float DistanceMeters { get; private set; }

        public PlayerBattery Battery { get; private set; }
        public PlayerMagnet Magnet { get; private set; }
        public Rigidbody2D Body { get; private set; }

        private RunModeConfig _config;
        private float _verticalVelocity;
        private bool _isHoldingJump;
        private float _holdTimer;
        private bool _runActive;
        private bool _externalJumpHeld;
        private float _speedMultiplier = 1f;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _jumpHeldThisFrame;
        private float _startX;

        private void Awake()
        {
            Battery = GetComponent<PlayerBattery>();
            Magnet = GetComponent<PlayerMagnet>();
            Body = GetComponent<Rigidbody2D>();
            _startX = Body.position.x;
            // Dynamic + zero gravity (we integrate our own gravity below) so trigger events fire
            // reliably against the obstacles/pickups' plain (non-Rigidbody) static colliders -
            // Kinematic-vs-static pairs do not generate Unity 2D trigger callbacks.
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.gravityScale = 0f;
            Body.constraints = RigidbodyConstraints2D.FreezeRotation;
            // Interpolation only smooths the rendered position BETWEEN FixedUpdate ticks - it
            // requires Body.position to actually change on that fixed cadence (see FixedUpdate/Step
            // below). Writing it from Update() instead defeats interpolation entirely and reads as
            // constant jitter, no matter this setting.
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        public void BeginRun(RunModeConfig config)
        {
            _config = config;
            // Reset to the scene's authored starting X (captured once in Awake), not the current X -
            // otherwise a Retry left the player wherever the previous run ended instead of restarting
            // from the actual beginning of the course.
            Body.position = new Vector2(_startX, groundY);
            // On any run after the first, this Rigidbody2D already has FixedUpdate/interpolation
            // history, and Body.position alone does not update transform.position until the next
            // physics step - transform.position (and anything reading it, e.g. CameraFollow's target)
            // kept reporting the OLD run's last position for a frame after the teleport (measured:
            // ~21m stale on a run that had covered ~21m). GameEvents.RunStarted fires later this same
            // method call and CameraFollow snaps to target.position on it, so that snap read the stale
            // position and did nothing - the new run opened with the player teleported but rendered
            // off-screen at the old spot until interpolation caught up several frames later. Setting
            // the Transform directly too (not just the Rigidbody2D) makes it correct immediately,
            // regardless of Unity's own physics-to-transform sync timing.
            transform.position = new Vector3(_startX, groundY, transform.position.z);
            DistanceMeters = 0f;
            _verticalVelocity = 0f;
            _isHoldingJump = false;
            _holdTimer = 0f;
            IsGrounded = true;
            _coyoteTimer = coyoteTimeSeconds;
            _jumpBufferTimer = 0f;
            _speedMultiplier = SaveSystem.Data.practiceMode ? practiceModeSpeedMultiplier : 1f;
            CurrentSpeed = config.BaseSpeed * _speedMultiplier;
            Battery.BeginRun(config);
            Magnet.ResetState();
            _runActive = true;
        }

        /// <summary>Exact air time (tap vs. full hold) for the current jump tuning - used by the spawner to keep generation fair.</summary>
        public JumpProfile ComputeJumpAirTimes() =>
            JumpPhysics.Simulate(minJumpVelocity, maxJumpVelocity, maxHoldSeconds, gravity, groundY);

        public void StopRun()
        {
            _runActive = false;
            // Covers both "died" (GameManager.HandleDeath) and "returned to menu" (FinishReturnToMenu) -
            // a Magnet active at either moment must not carry a lingering visual/state into the next run.
            Magnet.ResetState();
        }

        /// <summary>Continues the current run in place after a Second-Chance revive (position/speed are kept, unlike BeginRun).</summary>
        public void ResumeRun()
        {
            _verticalVelocity = 0f;
            _isHoldingJump = false;
            IsGrounded = true;
            _coyoteTimer = coyoteTimeSeconds;
            _jumpBufferTimer = 0f;
            _runActive = true;
        }

        private void Update()
        {
            if (!_runActive) return;

            // Raw input is polled every render frame so a quick tap between two FixedUpdate ticks
            // is never missed - it just latches into the buffer timer below, which FixedUpdate
            // consumes on the fixed cadence that actually moves the body.
            if (_externalJumpHeld || WasJumpPressedThisFrame())
            {
                _jumpBufferTimer = jumpBufferSeconds;
            }
            _jumpHeldThisFrame = _externalJumpHeld || IsJumpCurrentlyHeld();
        }

        private void FixedUpdate()
        {
            if (!_runActive) return;

            // Coyote time: still counts as "grounded enough to jump" for a brief window after
            // leaving the ground. Jump buffer: a press slightly before landing isn't dropped.
            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
            {
                StartJump();
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
            }

            if (!_jumpHeldThisFrame)
            {
                _isHoldingJump = false;
            }

            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - Time.fixedDeltaTime);
            if (!IsGrounded)
            {
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.fixedDeltaTime);
            }

            Step(Time.fixedDeltaTime);
        }

        /// <summary>Drives the actual physics integration - always called on the fixed timestep (see FixedUpdate) so Rigidbody2D interpolation has a consistent cadence to smooth between.</summary>
        public void Step(float deltaTime)
        {
            if (!_runActive) return;

            float survivalMultiplier = Battery.IsInSurvivalMode ? survivalModeSpeedMultiplier : 1f;
            // GameManager.CurrentGameSpeedMultiplier is the battery-driven "gets more frantic as charge
            // drops" factor - applied only to horizontal speed, after the existing distance ramp/cap, so
            // it layers on top of (rather than replaces) the per-mode pacing. Vertical jump physics below
            // (gravity, hold force, timers) never reads deltaTime scaled by it, so jump feel is untouched.
            CurrentSpeed = Mathf.Min(_config.MaxSpeed, _config.BaseSpeed + DistanceMeters * _config.SpeedPerMeter)
                * _speedMultiplier * survivalMultiplier * GameManager.Instance.CurrentGameSpeedMultiplier;

            float stepDistance = CurrentSpeed * deltaTime;
            DistanceMeters += stepDistance;

            if (_isHoldingJump && _holdTimer < maxHoldSeconds)
            {
                float extraForce = (maxJumpVelocity - minJumpVelocity) / maxHoldSeconds;
                _verticalVelocity += extraForce * deltaTime;
                _holdTimer += deltaTime;
            }

            _verticalVelocity += gravity * deltaTime;

            Vector2 pos = Body.position;
            pos.x += stepDistance;
            pos.y += _verticalVelocity * deltaTime;

            if (pos.y <= groundY)
            {
                pos.y = groundY;
                _verticalVelocity = 0f;
                if (!IsGrounded)
                {
                    _coyoteTimer = coyoteTimeSeconds;
                    GameEvents.RaiseLanded();
                }
                IsGrounded = true;
                _isHoldingJump = false;
            }
            else
            {
                IsGrounded = false;
            }

            // MovePosition (not the `.position` setter) is the API Rigidbody2D interpolation is
            // actually built around for per-tick movement - the direct setter is meant for one-off
            // teleports (see BeginRun) and can desync the interpolation buffer it kept looking
            // jittery even after this call was already correctly on the fixed timestep.
            Body.MovePosition(pos);

            GameEvents.RaiseDistanceChanged(DistanceMeters);
            Battery.Tick(deltaTime);
        }

        private void StartJump()
        {
            _verticalVelocity = minJumpVelocity;
            _isHoldingJump = true;
            _holdTimer = 0f;
            IsGrounded = false;
            GameEvents.RaiseJumped();
        }

        /// <summary>Lets UI (an on-screen touch button) drive jumping without going through the Input System.</summary>
        public void SetExternalJumpHeld(bool held) => _externalJumpHeld = held;

        private static bool WasJumpPressedThisFrame()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
        }

        private static bool IsJumpCurrentlyHeld()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) return true;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
            return false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_runActive) return;

            var obstacle = other.GetComponent<Obstacle>();
            if (obstacle != null)
            {
                Battery.ApplyHit();
                return;
            }

            var pickup = other.GetComponent<Pickup>();
            if (pickup != null)
            {
                pickup.Collect(this);
            }
        }
    }
}

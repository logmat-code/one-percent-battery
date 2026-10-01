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
        [Tooltip("Extra forward-speed multiplier while the battery is coasting at 1% (see PlayerBattery.IsInSurvivalMode), stacked on top of GameManager.CurrentGameSpeedMultiplier. Lowered from 1.15 - it was stacking with the battery curve's own ~1.42x tail to reach ~1.63x total at exactly 1%, which is what made Survival Mode 'far too fast, practically impossible' on real devices. Now just a small final-urgency tic on top of the curve's own (much flatter) ceiling, not the main source of 1%'s intensity.")]
        [SerializeField] private float survivalModeSpeedMultiplier = 1.02f;

        [Header("Pits")]
        [Tooltip("How far below groundY the player must fall (while over a Pit) before it's fatal - see SetOverPit.")]
        [SerializeField] private float pitFallDeathDepth = 1f;
        [Tooltip("How far below groundY counts as a genuine fall (vs. floating-point noise at rest) for a jump that was ALREADY airborne when it entered a Pit and turns out insufficient - see the !_pitGraceActive branch in Step(). A grounded entry's fate is decided by pit grace/coyote instead (see _pitGraceActive), not by this.")]
        [SerializeField] private float pitFallCommitEpsilon = 0.05f;

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
        private bool _jumpHeldLastFrame;
        private float _startX;
        private bool _isOverPit;
        private bool _pitGraceActive;
        private bool _pitFallCommitted;

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
            _isOverPit = false;
            _pitGraceActive = false;
            _pitFallCommitted = false;
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
            _isOverPit = false;
            _pitGraceActive = false;
            _pitFallCommitted = false;
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
            // A revive always treats the player as safely grounded again (see IsGrounded above) -
            // carrying a stale IsOverPit/_pitGraceActive/_pitFallCommitted into that would contradict
            // it (grounded, yet "no ground exists here" or "already doomed to fall"), so all three
            // reset the same way for the same reason.
            _isOverPit = false;
            _pitGraceActive = false;
            _pitFallCommitted = false;
            _runActive = true;
        }

        private void Update()
        {
            if (!_runActive) return;

            // Raw input is polled every render frame so a quick tap between two FixedUpdate ticks
            // is never missed - it just latches into the buffer timer below, which FixedUpdate
            // consumes on the fixed cadence that actually moves the body.
            bool heldNow = _externalJumpHeld || IsJumpInputHeldNow();
            bool justPressed = heldNow && !_jumpHeldLastFrame;
            _jumpHeldLastFrame = heldNow;

            if (justPressed)
            {
                _jumpBufferTimer = jumpBufferSeconds;
            }
            _jumpHeldThisFrame = heldNow;
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
                // This IS the coyote jump a Pit's grace exists to wait for - no separate jump system,
                // just ending grace right where the existing consumption already happens. StartJump
                // just applied the normal jump velocity, so the fall stops under its own steam; Step()
                // needs nothing else from this.
                _pitGraceActive = false;
            }

            if (!_jumpHeldThisFrame)
            {
                _isHoldingJump = false;
            }

            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - Time.fixedDeltaTime);
            if (!IsGrounded)
            {
                _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.fixedDeltaTime);
                if (_pitGraceActive && _coyoteTimer <= 0f)
                {
                    // The full 0.1s ran out with no jump to show for it - irreversible from here,
                    // regardless of whether IsOverPit is still true (a fast crossing may already have
                    // left the trigger during grace - see Step(), which keeps treating this the same
                    // as still being over the Pit either way).
                    _pitGraceActive = false;
                    _pitFallCommitted = true;
                }
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
                // This physical check ONLY matters for a jump that was already airborne when it
                // entered the Pit (no grace was ever granted for that case - see SetOverPit) and turns
                // out insufficient: it commits the instant that jump is genuinely sinking below ground.
                // A grounded entry's fate is instead decided entirely by pit grace/coyote (FixedUpdate)
                // below, never by this - gating on !_pitGraceActive is what stops this from re-cutting
                // the coyote window short the way it did before grace existed (gravity alone reaches
                // pitFallCommitEpsilon in ~2-3 ticks, well before a walking entry's real 0.1s coyote
                // window would otherwise resolve one way or the other).
                if (!_pitGraceActive && _isOverPit && !_pitFallCommitted && _verticalVelocity < 0f && pos.y < groundY - pitFallCommitEpsilon)
                {
                    _pitFallCommitted = true;
                }

                if (_isOverPit || _pitGraceActive || _pitFallCommitted)
                {
                    // No ground here - keep falling instead of clamping. _pitGraceActive is included
                    // deliberately: it can outlive _isOverPit (a fast crossing can clear the Pit's own
                    // trigger before its 0.1s coyote grace resolves - see FixedUpdate), and until grace
                    // itself resolves (jump consumed, or coyote runs out and commits the fall), ground
                    // must NOT reappear just because the player is no longer horizontally over the gap.
                    IsGrounded = false;
                    if (pos.y <= groundY - pitFallDeathDepth)
                    {
                        Battery.FallIntoPit();
                    }
                }
                else
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

        /// <summary>
        /// Called by PitZone's own trigger as the player's X enters/exits its horizontal span - see
        /// Step() for what this actually does to the ground check. A grounded entry starts this Pit's
        /// coyote grace (see _pitGraceActive); one that was already airborne (a jump already in
        /// progress) does not - that jump just continues, with no artificial extra 0.1s. Exiting
        /// (overPit=false) never touches grace either way - only a consumed coyote jump or an expired
        /// one (both in FixedUpdate) end it.
        /// </summary>
        public void SetOverPit(bool overPit)
        {
            if (overPit && !_isOverPit && IsGrounded)
            {
                _pitGraceActive = true;
            }
            // Purely cosmetic notification for the VFX layer - fires once, on the exit edge, only when
            // the fall was never committed (a clean jump-over, or a still-in-progress coyote jump that
            // will resolve safely). Read-only against existing state; changes nothing about the fall logic.
            if (!overPit && _isOverPit && !_pitFallCommitted)
            {
                GameEvents.RaisePitCleared();
            }
            _isOverPit = overPit;
        }

        /// <summary>
        /// Single source of truth for "is the jump input physically held right now" - keyboard/mouse/
        /// touch via the New Input System, PLUS a legacy UnityEngine.Input fallback for touch and mouse.
        /// The legacy fallback matters because Unity Remote predates the New Input System and is not
        /// officially supported by it - it only ever feeds touches into the legacy API, so without this
        /// a device connected only through Unity Remote could never register a touch as Touchscreen.current.
        /// #if guarded rather than an unconditional call: UnityEngine.Input throws at runtime when Active
        /// Input Handling is set to "Input System Package (New)" only: ENABLE_LEGACY_INPUT_MANAGER is
        /// only defined when the legacy manager is actually available (Old or Both - this project is
        /// currently Both), so switching Active Input Handling later can't reintroduce that exception.
        /// Update() below turns this single "held" read into its own "just pressed" edge itself (see
        /// _jumpHeldLastFrame) instead of trusting the Input System's own wasPressedThisFrame flag, which
        /// is edge-triggered against the Input System's internal frame bookkeeping rather than against
        /// this MonoBehaviour's own Update cadence.
        /// </summary>
        private static bool IsJumpInputHeldNow()
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) return true;
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.touchCount > 0) return true;
            if (UnityEngine.Input.GetMouseButton(0)) return true;
#endif

            return false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_runActive) return;

            // PitZone does not deal a hit here - it drives SetOverPit on its own (see PitZone), and
            // falling far enough while over one is handled entirely inside Step().
            if (other.GetComponent<PitZone>() != null) return;

            var obstacle = other.GetComponent<Obstacle>();
            if (obstacle != null)
            {
                // Purely cosmetic notification for the VFX layer - read BEFORE ApplyHit() since that
                // is what would flip IsInvulnerable off if this hit uses up the last of the shield
                // window. ApplyHit() itself is completely unchanged; ObstacleHit does not fire for a
                // blocked hit (see PlayerBattery.ApplyHit), so this is the only signal a shield block happened.
                if (Battery.IsInvulnerable)
                {
                    GameEvents.RaiseShieldBlockedHit(transform.position);
                }
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

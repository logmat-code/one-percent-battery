using OnePercentBattery.Core;
using UnityEngine;

namespace OnePercentBattery.Player
{
    /// <summary>
    /// Magnet power-up: while active, nearby coins (see Pickup) accelerate toward the player and are
    /// collected through the normal trigger/Collect() path - this component only owns the timer and
    /// the tuning coins read, it never grants coins itself.
    /// </summary>
    public sealed class PlayerMagnet : MonoBehaviour
    {
        [SerializeField] private float magnetDuration = 8f;
        [SerializeField] private float magnetRadius = 4f;
        [SerializeField] private float magnetPullSpeed = 6f;
        [Tooltip("Extra pull speed per second a coin has been travelling, so it noticeably snaps in over a long pull instead of crawling at a constant rate.")]
        [SerializeField] private float magnetAcceleration = 4f;

        public bool IsActive => _timeRemaining > 0f;
        public float TimeRemaining => _timeRemaining;
        public float Radius => magnetRadius;
        public float PullSpeed => magnetPullSpeed;
        public float Acceleration => magnetAcceleration;

        private float _timeRemaining;

        /// <summary>Collecting a Magnet refreshes to the full duration rather than stacking on top - simplest option that can't be chained into an unbounded run.</summary>
        public void Activate()
        {
            _timeRemaining = magnetDuration;
            GameEvents.RaiseMagnetTimeChanged(_timeRemaining);
        }

        public void ResetState()
        {
            if (_timeRemaining <= 0f) return;
            _timeRemaining = 0f;
            GameEvents.RaiseMagnetTimeChanged(0f);
        }

        private void Update()
        {
            if (_timeRemaining <= 0f) return;

            _timeRemaining = Mathf.Max(0f, _timeRemaining - Time.deltaTime);
            GameEvents.RaiseMagnetTimeChanged(_timeRemaining);
        }
    }
}

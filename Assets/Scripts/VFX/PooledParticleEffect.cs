using System;
using UnityEngine;

namespace OnePercentBattery.VFX
{
    /// <summary>
    /// A single reusable one-shot particle burst. Play(position, color) repositions, recolors and
    /// fires it; it reports itself back to its pool on its own once its configured lifetime has
    /// elapsed, so callers never track a duration or call anything to release it. Every gameplay VFX
    /// cue (coin, Charger, Magnet, Shield, hits, landings...) shares this same small pool of instances
    /// instead of an Instantiate/Destroy per event - see GameplayVfxController, the only caller.
    ///
    /// Releases on a plain countdown timer (computed once from the ParticleSystem's own
    /// duration+max start lifetime) rather than polling ParticleSystem.IsAlive()/isEmitting: verified
    /// empirically that a non-looping, burst-only system does not reliably report IsAlive()==false on
    /// its own once particleCount reaches 0 - it can stay "alive" indefinitely by that read - so
    /// trusting it here would have silently stopped returning bursts to the pool during real play,
    /// growing the pool without bound. A deterministic timer sidesteps that entirely.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class PooledParticleEffect : MonoBehaviour
    {
        private ParticleSystem _particles;
        private Action<PooledParticleEffect> _release;
        private float _lifetimeSeconds;
        private float _remainingSeconds;
        private bool _awaitingRelease;

        private void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = _particles.main;
            // A small safety margin on top of duration+max lifetime - never wrong to release a frame
            // or two late, but releasing early would snap a still-visible particle to a new position.
            _lifetimeSeconds = main.duration + main.startLifetime.constantMax + 0.1f;
        }

        /// <summary>Set once by the owning controller right after Get() from its pool - never by anything else.</summary>
        public void Configure(Action<PooledParticleEffect> release) => _release = release;

        public void Play(Vector3 position, Color color)
        {
            transform.position = position;
            ParticleSystem.MainModule main = _particles.main;
            main.startColor = color;
            _particles.Clear(true);
            _particles.Play(true);
            _remainingSeconds = _lifetimeSeconds;
            _awaitingRelease = true;
        }

        /// <summary>A Retry needs every in-flight effect gone immediately (see GameplayVfxController.HandleRunStarted), not faded out over its normal lifetime.</summary>
        public void StopImmediately()
        {
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _awaitingRelease = false;
        }

        private void Update()
        {
            if (!_awaitingRelease) return;

            _remainingSeconds -= Time.deltaTime;
            if (_remainingSeconds <= 0f)
            {
                _awaitingRelease = false;
                _release?.Invoke(this);
            }
        }
    }
}

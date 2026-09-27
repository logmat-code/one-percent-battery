using System;
using UnityEngine;

namespace OnePercentBattery.VFX
{
    /// <summary>
    /// A single reusable pulse - a soft ring/glow sprite that scales up and fades out over a short,
    /// fixed duration. Used for "activation" cues (Charger/Magnet/Shield obtained, a blocked hit, a
    /// cleared pit) that read better as an expanding glow than a particle burst. Pooled exactly like
    /// <see cref="PooledParticleEffect"/> - see GameplayVfxController, the only caller.
    /// </summary>
    public sealed class PooledPulseEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float duration = 0.35f;
        [SerializeField] private float startScale = 0.4f;
        [SerializeField] private float endScale = 1.6f;
        [SerializeField] private AnimationCurve alphaOverTime = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private Action<PooledPulseEffect> _release;
        private float _elapsed;
        private bool _active;
        private Color _baseColor;
        private Transform _followTarget;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        public void Configure(Action<PooledPulseEffect> release) => _release = release;

        /// <summary>followTarget is optional - when set, the pulse stays centered on it every frame (e.g. the player) instead of a fixed world position.</summary>
        public void Play(Vector3 position, Color color, Transform followTarget = null)
        {
            _followTarget = followTarget;
            transform.position = position;
            _baseColor = color;
            _elapsed = 0f;
            _active = true;
            transform.localScale = Vector3.one * startScale;
            if (spriteRenderer != null) spriteRenderer.color = color;
        }

        /// <summary>A Retry needs every in-flight pulse gone immediately (see GameplayVfxController.HandleRunStarted).</summary>
        public void StopImmediately()
        {
            _active = false;
            _followTarget = null;
        }

        private void Update()
        {
            if (!_active) return;

            if (_followTarget != null) transform.position = _followTarget.position;

            _elapsed += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(_elapsed / duration) : 1f;
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
            if (spriteRenderer != null)
            {
                float a = alphaOverTime.Evaluate(t);
                spriteRenderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, _baseColor.a * a);
            }

            if (t >= 1f)
            {
                _active = false;
                _followTarget = null;
                _release?.Invoke(this);
            }
        }
    }
}

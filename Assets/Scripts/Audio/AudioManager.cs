using System.Collections.Generic;
using OnePercentBattery.Core;
using OnePercentBattery.Data;
using OnePercentBattery.Utils;
using UnityEngine;

namespace OnePercentBattery.Audio
{
    /// <summary>
    /// Reactive SFX/music layer: subscribes to <see cref="GameEvents"/> exactly like the meta
    /// managers do, so gameplay code never calls into audio directly. Placeholder SFX are
    /// synthesized at startup (short tones/sweeps) so the game has working audio feedback with no
    /// external asset dependency - drop real AudioClips onto the serialized fields to replace them.
    /// Muting is handled by the existing Settings "Sound" toggle via <see cref="AudioListener.volume"/>;
    /// this class does not duplicate that gate.
    /// </summary>
    public sealed class AudioManager : Singleton<AudioManager>
    {
        [Header("Music (optional - safe no-op until clips are assigned)")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameplayMusic;
        [SerializeField] private float musicVolume = 0.5f;

        [Header("Placeholder SFX overrides (leave empty to use the synthesized defaults)")]
        [SerializeField] private AudioClip jumpSfxOverride;
        [SerializeField] private AudioClip landSfxOverride;
        [SerializeField] private AudioClip coinSfxOverride;
        [SerializeField] private AudioClip energySfxOverride;
        [SerializeField] private AudioClip shieldSfxOverride;
        [SerializeField] private AudioClip hitSfxOverride;
        [SerializeField] private AudioClip survivalSfxOverride;
        [SerializeField] private AudioClip deathSfxOverride;
        [SerializeField] private AudioClip uiClickSfxOverride;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        private AudioClip _jumpSfx, _landSfx, _coinSfx, _energySfx, _shieldSfx, _hitSfx, _survivalSfx, _deathSfx, _uiClickSfx;
        private bool _shieldWasActive;

        protected override void Awake()
        {
            base.Awake();

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.volume = musicVolume;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            BuildSynthesizedClips();
        }

        private void OnEnable()
        {
            GameEvents.Jumped += HandleJumped;
            GameEvents.Landed += HandleLanded;
            GameEvents.CoinCollected += HandleCoinCollected;
            GameEvents.EnergyCollected += HandleEnergyCollected;
            GameEvents.ShieldTimeChanged += HandleShieldTimeChanged;
            GameEvents.ObstacleHit += HandleObstacleHit;
            GameEvents.SurvivalModeEntered += HandleSurvivalModeEntered;
            GameEvents.RunEnded += HandleRunEnded;
            GameEvents.RunStarted += HandleRunStarted;
            GameEvents.ReturnedToMenu += HandleReturnedToMenu;
        }

        private void OnDisable()
        {
            GameEvents.Jumped -= HandleJumped;
            GameEvents.Landed -= HandleLanded;
            GameEvents.CoinCollected -= HandleCoinCollected;
            GameEvents.EnergyCollected -= HandleEnergyCollected;
            GameEvents.ShieldTimeChanged -= HandleShieldTimeChanged;
            GameEvents.ObstacleHit -= HandleObstacleHit;
            GameEvents.SurvivalModeEntered -= HandleSurvivalModeEntered;
            GameEvents.RunEnded -= HandleRunEnded;
            GameEvents.RunStarted -= HandleRunStarted;
            GameEvents.ReturnedToMenu -= HandleReturnedToMenu;
        }

        private void HandleJumped() => PlaySfx(_jumpSfx, 0.7f);
        private void HandleLanded() => PlaySfx(_landSfx, 0.5f);
        private void HandleCoinCollected(int runTotal) => PlaySfx(_coinSfx, 0.6f);
        private void HandleEnergyCollected(float amount) => PlaySfx(_energySfx, 0.8f);
        private void HandleObstacleHit() => PlaySfx(_hitSfx, 1f);
        private void HandleSurvivalModeEntered() => PlaySfx(_survivalSfx, 1f);

        private void HandleShieldTimeChanged(float secondsRemaining)
        {
            bool active = secondsRemaining > 0f;
            if (active && !_shieldWasActive)
            {
                PlaySfx(_shieldSfx, 0.7f);
            }
            _shieldWasActive = active;
        }

        private void HandleRunEnded(RunResult result)
        {
            if (result.Cause == RunEndCause.HitObstacle)
            {
                PlaySfx(_deathSfx, 1f);
            }
        }

        private void HandleRunStarted(RunModeType mode) => PlayMusic(gameplayMusic);
        private void HandleReturnedToMenu() => PlayMusic(menuMusic);

        /// <summary>Hook for a UI Button's OnClick to get a consistent click sound; safe to wire even before real audio exists.</summary>
        public void PlayUiClick() => PlaySfx(_uiClickSfx, 0.5f);

        /// <summary>0 (idle) to 1 (max speed) - nudges music pitch so the track feels like it's keeping pace. No-op without a clip.</summary>
        public void SetMusicIntensity(float speedRatio01)
        {
            if (_musicSource == null) return;
            _musicSource.pitch = Mathf.Lerp(0.97f, 1.12f, Mathf.Clamp01(speedRatio01));
        }

        private void PlayMusic(AudioClip clip)
        {
            if (_musicSource == null || clip == null) return;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;

            _musicSource.clip = clip;
            _musicSource.volume = musicVolume;
            _musicSource.pitch = 1f;
            _musicSource.Play();
        }

        private void PlaySfx(AudioClip clip, float volume)
        {
            if (_sfxSource == null || clip == null) return;
            _sfxSource.PlayOneShot(clip, volume);
        }

        private void BuildSynthesizedClips()
        {
            _jumpSfx = jumpSfxOverride != null ? jumpSfxOverride : SynthTone("sfx_jump", 320f, 720f, 0.12f, WaveShape.Sine, 0.02f);
            _landSfx = landSfxOverride != null ? landSfxOverride : SynthTone("sfx_land", 160f, 90f, 0.08f, WaveShape.Sine, 0.005f);
            _coinSfx = coinSfxOverride != null ? coinSfxOverride : SynthTone("sfx_coin", 900f, 1500f, 0.1f, WaveShape.Sine, 0.005f);
            _energySfx = energySfxOverride != null ? energySfxOverride : SynthTone("sfx_energy", 380f, 950f, 0.25f, WaveShape.Sine, 0.02f);
            _shieldSfx = shieldSfxOverride != null ? shieldSfxOverride : SynthTone("sfx_shield", 600f, 1000f, 0.14f, WaveShape.Triangle, 0.01f);
            _hitSfx = hitSfxOverride != null ? hitSfxOverride : SynthTone("sfx_hit", 220f, 60f, 0.16f, WaveShape.Noise, 0.002f);
            _survivalSfx = survivalSfxOverride != null ? survivalSfxOverride : SynthAlarm("sfx_survival");
            _deathSfx = deathSfxOverride != null ? deathSfxOverride : SynthTone("sfx_death", 500f, 90f, 0.45f, WaveShape.Sine, 0.02f);
            _uiClickSfx = uiClickSfxOverride != null ? uiClickSfxOverride : SynthTone("sfx_ui_click", 1800f, 1800f, 0.03f, WaveShape.Square, 0.001f);
        }

        private enum WaveShape { Sine, Square, Triangle, Noise }

        private static AudioClip SynthTone(string name, float startFreq, float endFreq, float duration, WaveShape shape, float attackSeconds)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(duration * sampleRate));
            var samples = new float[sampleCount];
            int attackSamples = Mathf.Clamp(Mathf.RoundToInt(attackSeconds * sampleRate), 0, sampleCount / 2);
            float phase = 0f;
            var rng = new System.Random(name.GetHashCode());

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += freq / sampleRate;

                float raw;
                switch (shape)
                {
                    case WaveShape.Square:
                        raw = Mathf.Sign(Mathf.Sin(phase * 2f * Mathf.PI));
                        break;
                    case WaveShape.Triangle:
                        raw = Mathf.PingPong(phase * 2f, 1f) * 2f - 1f;
                        break;
                    case WaveShape.Noise:
                        raw = (float)(rng.NextDouble() * 2.0 - 1.0);
                        break;
                    default:
                        raw = Mathf.Sin(phase * 2f * Mathf.PI);
                        break;
                }

                float envelope = 1f - t;
                if (i < attackSamples && attackSamples > 0)
                {
                    envelope *= i / (float)attackSamples;
                }
                samples[i] = raw * envelope * 0.85f;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Two-tone alternating alarm - distinct from the single-sweep tones used elsewhere, for the 1% signature moment.</summary>
        private static AudioClip SynthAlarm(string name)
        {
            const int sampleRate = 44100;
            const float duration = 0.3f;
            int sampleCount = Mathf.RoundToInt(duration * sampleRate);
            var samples = new float[sampleCount];
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float freq = (Mathf.FloorToInt(t * 6f) % 2 == 0) ? 880f : 660f;
                phase += freq / sampleRate;
                float envelope = 1f - t * 0.6f;
                samples[i] = Mathf.Sin(phase * 2f * Mathf.PI) * envelope * 0.85f;
            }

            var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}

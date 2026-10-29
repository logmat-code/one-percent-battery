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
        [SerializeField] private AudioClip magnetSfxOverride;
        [SerializeField] private AudioClip shieldBlockSfxOverride;
        [SerializeField] private AudioClip hitSfxOverride;
        [SerializeField] private AudioClip survivalSfxOverride;
        [SerializeField] private AudioClip deathSfxOverride;
        [SerializeField] private AudioClip uiClickSfxOverride;

        [Tooltip("Minimum seconds between two plays of the SAME sfx clip - PlayOneShot layers overlapping calls with no built-in limit, so rapid-fire triggers (e.g. a Magnet sweeping up several coins in one frame) would otherwise stack their amplitude and clip. Extra triggers inside the window are silently dropped rather than queued or attenuated, since a duplicate coin ding a few ms later adds nothing audible anyway.")]
        [SerializeField] private float minSfxRetriggerSeconds = 0.045f;

        [Header("Low-battery warning SFX overrides (leave empty to use the synthesized defaults)")]
        [SerializeField] private AudioClip batteryWarning25SfxOverride;
        [SerializeField] private AudioClip batteryCritical10SfxOverride;
        [SerializeField] private AudioClip batterySevere5SfxOverride;
        [SerializeField] private AudioClip batteryFinal1SfxOverride;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;

        private AudioClip _jumpSfx, _landSfx, _coinSfx, _energySfx, _shieldSfx, _magnetSfx, _shieldBlockSfx, _hitSfx, _survivalSfx, _deathSfx, _uiClickSfx;
        private AudioClip _batteryWarning25Sfx, _batteryCritical10Sfx, _batterySevere5Sfx, _batteryFinal1Sfx;
        private bool _shieldWasActive;
        private bool _magnetWasActive;
        private readonly Dictionary<AudioClip, float> _lastPlayedRealtime = new Dictionary<AudioClip, float>();

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
            GameEvents.MagnetTimeChanged += HandleMagnetTimeChanged;
            GameEvents.ShieldBlockedHit += HandleShieldBlockedHit;
            GameEvents.ObstacleHit += HandleObstacleHit;
            GameEvents.SurvivalModeEntered += HandleSurvivalModeEntered;
            GameEvents.RunEnded += HandleRunEnded;
            GameEvents.RunStarted += HandleRunStarted;
            GameEvents.ReturnedToMenu += HandleReturnedToMenu;
            GameEvents.LowBatteryThresholdCrossed += HandleLowBatteryThresholdCrossed;
            GameEvents.LowBatteryIntensityChanged += HandleLowBatteryIntensityChanged;
        }

        private void OnDisable()
        {
            GameEvents.Jumped -= HandleJumped;
            GameEvents.Landed -= HandleLanded;
            GameEvents.CoinCollected -= HandleCoinCollected;
            GameEvents.EnergyCollected -= HandleEnergyCollected;
            GameEvents.ShieldTimeChanged -= HandleShieldTimeChanged;
            GameEvents.MagnetTimeChanged -= HandleMagnetTimeChanged;
            GameEvents.ShieldBlockedHit -= HandleShieldBlockedHit;
            GameEvents.ObstacleHit -= HandleObstacleHit;
            GameEvents.SurvivalModeEntered -= HandleSurvivalModeEntered;
            GameEvents.RunEnded -= HandleRunEnded;
            GameEvents.RunStarted -= HandleRunStarted;
            GameEvents.ReturnedToMenu -= HandleReturnedToMenu;
            GameEvents.LowBatteryThresholdCrossed -= HandleLowBatteryThresholdCrossed;
            GameEvents.LowBatteryIntensityChanged -= HandleLowBatteryIntensityChanged;
        }

        private void HandleJumped() => PlaySfx(_jumpSfx, 0.7f);
        private void HandleLanded() => PlaySfx(_landSfx, 0.5f);
        private void HandleCoinCollected(int runTotal) => PlaySfx(_coinSfx, 0.55f, 0.08f);
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

        private void HandleMagnetTimeChanged(float secondsRemaining)
        {
            bool active = secondsRemaining > 0f;
            if (active && !_magnetWasActive)
            {
                PlaySfx(_magnetSfx, 0.6f);
            }
            _magnetWasActive = active;
        }

        /// <summary>A shield block is a save, not an impact - ObstacleHit does not fire for this case (see GameEvents.ShieldBlockedHit), so without this the moment was silent.</summary>
        private void HandleShieldBlockedHit(Vector3 position) => PlaySfx(_shieldBlockSfx, 0.65f);

        private void HandleRunEnded(RunResult result)
        {
            if (result.Cause == RunEndCause.HitObstacle || result.Cause == RunEndCause.FellInPit)
            {
                PlaySfx(_deathSfx, 1f);
            }
        }

        /// <summary>The game always boots straight into the Main Menu, but that never raises ReturnedToMenu (nothing "returned" - it's the initial state), so without this menu music would stay silent until the player finished a first run.</summary>
        private void Start() => PlayMusic(menuMusic);

        private void HandleRunStarted(RunModeType mode) => PlayMusic(gameplayMusic);
        private void HandleReturnedToMenu() => PlayMusic(menuMusic);

        private void HandleLowBatteryThresholdCrossed(BatteryWarningTier tier)
        {
            switch (tier)
            {
                case BatteryWarningTier.Warning25:
                    PlaySfx(_batteryWarning25Sfx, 0.6f);
                    break;
                case BatteryWarningTier.Critical10:
                    PlaySfx(_batteryCritical10Sfx, 0.75f);
                    break;
                case BatteryWarningTier.Severe5:
                    PlaySfx(_batterySevere5Sfx, 0.9f);
                    break;
                case BatteryWarningTier.Final1:
                    PlaySfx(_batteryFinal1Sfx, 1f);
                    break;
            }
        }

        /// <summary>Reuses the existing (previously unwired) pitch-nudge hook - low battery reads as
        /// rising tension the same way high speed was already meant to.</summary>
        private void HandleLowBatteryIntensityChanged(float intensity01) => SetMusicIntensity(intensity01);

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

        private void PlaySfx(AudioClip clip, float volume, float pitchVariation = 0f)
        {
            if (_sfxSource == null || clip == null) return;

            float now = Time.unscaledTime;
            if (_lastPlayedRealtime.TryGetValue(clip, out float lastPlayed) && now - lastPlayed < minSfxRetriggerSeconds)
            {
                return;
            }
            _lastPlayedRealtime[clip] = now;

            if (pitchVariation > 0f)
            {
                float prevPitch = _sfxSource.pitch;
                _sfxSource.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
                _sfxSource.PlayOneShot(clip, volume);
                _sfxSource.pitch = prevPitch;
            }
            else
            {
                _sfxSource.PlayOneShot(clip, volume);
            }
        }

        private void BuildSynthesizedClips()
        {
            _jumpSfx = jumpSfxOverride != null ? jumpSfxOverride : SynthTone("sfx_jump", 280f, 680f, 0.10f, WaveShape.Sine, 0.005f);
            _landSfx = landSfxOverride != null ? landSfxOverride : SynthTone("sfx_land", 150f, 60f, 0.06f, WaveShape.Sine, 0.003f);
            _coinSfx = coinSfxOverride != null ? coinSfxOverride : SynthTone("sfx_coin", 980f, 1680f, 0.08f, WaveShape.Sine, 0.003f);
            _energySfx = energySfxOverride != null ? energySfxOverride : SynthTone("sfx_energy", 340f, 1100f, 0.22f, WaveShape.Triangle, 0.01f);
            _shieldSfx = shieldSfxOverride != null ? shieldSfxOverride : SynthTone("sfx_shield", 520f, 1200f, 0.15f, WaveShape.Square, 0.008f);
            _magnetSfx = magnetSfxOverride != null ? magnetSfxOverride : SynthTone("sfx_magnet", 440f, 1320f, 0.16f, WaveShape.Triangle, 0.015f);
            _shieldBlockSfx = shieldBlockSfxOverride != null ? shieldBlockSfxOverride : SynthTone("sfx_shield_block", 1400f, 650f, 0.09f, WaveShape.Square, 0.002f);
            _hitSfx = hitSfxOverride != null ? hitSfxOverride : SynthTone("sfx_hit", 260f, 50f, 0.14f, WaveShape.Noise, 0.002f);
            _survivalSfx = survivalSfxOverride != null ? survivalSfxOverride : SynthAlarm("sfx_survival");
            _deathSfx = deathSfxOverride != null ? deathSfxOverride : SynthTone("sfx_death", 500f, 90f, 0.45f, WaveShape.Sine, 0.02f);
            _uiClickSfx = uiClickSfxOverride != null ? uiClickSfxOverride : SynthTone("sfx_ui_click", 1800f, 1800f, 0.03f, WaveShape.Square, 0.001f);

            _batteryWarning25Sfx = batteryWarning25SfxOverride != null ? batteryWarning25SfxOverride : SynthTone("sfx_battery_25", 700f, 500f, 0.12f, WaveShape.Sine, 0.01f);
            _batteryCritical10Sfx = batteryCritical10SfxOverride != null ? batteryCritical10SfxOverride : SynthTone("sfx_battery_10", 640f, 420f, 0.16f, WaveShape.Triangle, 0.01f);
            _batterySevere5Sfx = batterySevere5SfxOverride != null ? batterySevere5SfxOverride : SynthTone("sfx_battery_5", 580f, 340f, 0.2f, WaveShape.Square, 0.005f);
            _batteryFinal1Sfx = batteryFinal1SfxOverride != null ? batteryFinal1SfxOverride : SynthAlarm("sfx_battery_1");
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

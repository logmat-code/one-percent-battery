using System;
using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using OnePercentBattery.Player;
using OnePercentBattery.Save;
using OnePercentBattery.Services;
using OnePercentBattery.Spawning;
using OnePercentBattery.Utils;
using OnePercentBattery.World;
using UnityEngine;

namespace OnePercentBattery.Core
{
    /// <summary>
    /// Owns the run lifecycle: mode selection, start/pause/resume, death, the Second-Chance revive,
    /// and finalizing stats into <see cref="SaveSystem"/> exactly once per run (a used Second Chance
    /// continues the same run instead of counting as a separate one).
    /// </summary>
    public sealed class GameManager : Singleton<GameManager>
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private CourseSpawner spawner;
        [SerializeField] private ZoneManager zoneManager;
        [SerializeField] private float reviveRefillPercent = 20f;
        [SerializeField] private float reviveInvulnerabilitySeconds = 2f;

        [Header("Battery-driven game speed")]
        [Tooltip("Global speed multiplier at 100% battery (progress = 0 on the curve below).")]
        [SerializeField] private float minGameSpeed = 1f;
        [Tooltip("Global speed multiplier at 1% battery / survival mode (progress = 1 on the curve below). Lowered from 2 - that felt like the game had simply doubled its own speed rather than getting progressively more tense.")]
        [SerializeField] private float maxGameSpeed = 1.55f;
        [Tooltip("Shapes the ramp from minGameSpeed to maxGameSpeed as battery drains from 100% to 0%. Tuned so the first half of the battery barely accelerates, it becomes clearly noticeable under 50%, tense under 30%, and frantic-but-controllable from 10% down - not a symmetric ease-in-out, since that made the 30-50% stretch (its steepest region) feel like the game had suddenly doubled speed.")]
        [SerializeField] private AnimationCurve speedCurve = BuildDefaultSpeedCurve();

        private static AnimationCurve BuildDefaultSpeedCurve()
        {
            // Keyframes are (progress = 1 - battery/100, curve value 0-1) chosen to hit ~1.00x/1.08x/
            // 1.18x/1.30x/1.38x/1.47x/1.55x at battery 100/75/50/30/20/10/1% once fed through
            // Lerp(minGameSpeed, maxGameSpeed, curve.Evaluate(progress)).
            var curve = new AnimationCurve(
                new Keyframe(0.00f, 0.0000f),
                new Keyframe(0.25f, 0.1455f),
                new Keyframe(0.50f, 0.3273f),
                new Keyframe(0.70f, 0.5455f),
                new Keyframe(0.80f, 0.6909f),
                new Keyframe(0.90f, 0.8545f),
                new Keyframe(1.00f, 1.0000f));
            for (int i = 0; i < curve.length; i++)
            {
                curve.SmoothTangents(i, 0f);
            }
            return curve;
        }

        public GameState State { get; private set; } = GameState.MainMenu;
        public RunModeType ActiveMode { get; private set; }
        public RunModeConfig ActiveConfig { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public RunResult LastResult { get; private set; }

        private bool _secondChanceUsedThisRun;
        private bool _doubleCoinsClaimedThisRun;
        // Root cause of the Retry bugs: FinalizeRun()+MaybeShowInterstitial() is an async chain (the
        // interstitial can take real seconds to close), and State stays GameOver for its entire
        // duration. A second RetrySameMode()/ReturnToMainMenu() call in that window used to pass the
        // State check again, double-finalizing the run and queuing a second StartRun - which fired
        // later, silently resetting an already-in-progress new run out from under the player. This
        // flag blocks any further transition until the one already running actually completes.
        private bool _runTransitionInProgress;

        public bool SecondChanceUsedThisRun => _secondChanceUsedThisRun;
        public bool DoubleCoinsClaimedThisRun => _doubleCoinsClaimedThisRun;

        /// <summary>
        /// Battery-driven global speed factor: 1 at full charge, ramping smoothly (via speedCurve) up
        /// to maxGameSpeed as charge approaches 0/Survival Mode. Everything that represents forward
        /// progress derives its pace from the player's own CurrentSpeed (see PlayerController.Step) or
        /// from the player's live position (camera follow, parallax, the spawner's fairness math) rather
        /// than simulating an independent speed - so multiplying it in that one place is enough to keep
        /// the whole world in sync without touching obstacles/coins/parallax individually.
        /// </summary>
        public float CurrentGameSpeedMultiplier
        {
            get
            {
                float batteryNormalized = player.Battery.CurrentPercent / 100f;
                float progress = 1f - batteryNormalized;
                return Mathf.Lerp(minGameSpeed, maxGameSpeed, speedCurve.Evaluate(progress));
            }
        }

        /// <summary>
        /// Public entry point for the two places that start a run cold (MainMenu's Let's Run/Daily
        /// Run) - guarded the same way Retry/Home are, so a fast double-tap can't run the reset logic
        /// twice. RetrySameMode reaches StartRunInternal directly instead of through here, because it
        /// already set the SAME flag before its (possibly async, ad-gated) call chain began; routing
        /// it back through this guard would just have it block on the flag it itself is holding.
        /// </summary>
        public void StartRun(RunModeType mode)
        {
            if (State != GameState.MainMenu || _runTransitionInProgress) return;
            _runTransitionInProgress = true;
            StartRunInternal(mode);
        }

        private void StartRunInternal(RunModeType mode)
        {
            ActiveMode = mode;
            ActiveConfig = RunModeConfig.For(mode);
            ElapsedSeconds = 0f;
            _secondChanceUsedThisRun = false;
            _doubleCoinsClaimedThisRun = false;

            System.Random rng = mode == RunModeType.Daily ? DailyRunManager.Instance.CreateTodaysRng() : new System.Random();

            CurrencyManager.Instance.BeginRun();
            player.gameObject.SetActive(true);
            player.enabled = true;
            spawner.enabled = true;
            player.BeginRun(ActiveConfig);
            spawner.BeginRun(ActiveConfig, rng);
            zoneManager.ResetZone();

            Time.timeScale = 1f;
            State = GameState.Playing;
            _runTransitionInProgress = false;
            GameEvents.RaiseRunStarted(mode);
        }

        private void Update()
        {
            if (State != GameState.Playing) return;

            ElapsedSeconds += Time.deltaTime;
            if (player.Battery.IsDead)
            {
                HandleDeath();
            }
        }

        private void HandleDeath()
        {
            State = GameState.GameOver;
            player.StopRun();
            spawner.StopRun();

            RunResult preview = BuildResult();
            bool canOfferSecondChance = ActiveConfig.AllowSecondChance && !_secondChanceUsedThisRun;
            GameEvents.RaiseRunAwaitingContinue(preview, canOfferSecondChance);
        }

        private RunResult BuildResult()
        {
            float distance = player.DistanceMeters;
            int coins = CurrencyManager.Instance.RunCoins;
            float survivalSeconds = player.Battery.SurvivalModeSeconds;
            bool isNewRecord = distance > SaveSystem.Data.BestDistanceFor(ActiveMode);
            bool isNewSurvivalRecord = survivalSeconds > SaveSystem.Data.bestSurvivalModeSeconds;
            return new RunResult(ActiveMode, distance, coins, survivalSeconds, ElapsedSeconds, player.Battery.DeathCause, isNewRecord, isNewSurvivalRecord);
        }

        /// <summary>Revive ("Second Chance"): one rewarded-ad revive per run, granted only from the ad's reward-earned callback.</summary>
        public void RequestSecondChance(Action onFailedOrSkipped)
        {
            if (State != GameState.GameOver || !ActiveConfig.AllowSecondChance || _secondChanceUsedThisRun)
            {
                onFailedOrSkipped?.Invoke();
                return;
            }

            AdsManager.Instance.ShowRewarded(RewardedAdPlacement.Revive,
                onRewardEarned: () =>
                {
                    _secondChanceUsedThisRun = true;
                    player.Battery.Revive(reviveRefillPercent, reviveInvulnerabilitySeconds);
                    player.enabled = true;
                    spawner.enabled = true;
                    player.ResumeRun();
                    spawner.ResumeRun();
                    State = GameState.Playing;
                    GameEvents.RaiseSecondChanceGranted();
                },
                onFailedOrSkipped: onFailedOrSkipped);
        }

        /// <summary>Double Coins: awards exactly `coinsEarnedThisRun` additional coins, once, only from the reward-earned callback.</summary>
        public void RequestDoubleCoins(int coinsEarnedThisRun, Action onGranted, Action onFailedOrSkipped)
        {
            if (State != GameState.GameOver || _doubleCoinsClaimedThisRun || coinsEarnedThisRun <= 0)
            {
                onFailedOrSkipped?.Invoke();
                return;
            }

            AdsManager.Instance.ShowRewarded(RewardedAdPlacement.DoubleCoins,
                onRewardEarned: () =>
                {
                    _doubleCoinsClaimedThisRun = true;
                    CurrencyManager.Instance.AddCoins(coinsEarnedThisRun);
                    onGranted?.Invoke();
                },
                onFailedOrSkipped: onFailedOrSkipped);
        }

        public void RetrySameMode()
        {
            if (State != GameState.GameOver || _runTransitionInProgress) return;
            _runTransitionInProgress = true;
            FinalizeRun();
            AdsManager.Instance.MaybeShowInterstitial(() => StartRunInternal(ActiveMode));
        }

        public void ReturnToMainMenu()
        {
            if (State == GameState.GameOver)
            {
                if (_runTransitionInProgress) return;
                _runTransitionInProgress = true;
                FinalizeRun();
                AdsManager.Instance.MaybeShowInterstitial(FinishReturnToMenu);
            }
            else
            {
                FinishReturnToMenu();
            }
        }

        private void FinishReturnToMenu()
        {
            player.StopRun();
            spawner.StopRun();
            Time.timeScale = 1f;
            State = GameState.MainMenu;
            _runTransitionInProgress = false;
            GameEvents.RaiseReturnedToMenu();
        }

        /// <summary>Mobile lifecycle safety: never let the battery drain or a hit register while backgrounded.</summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) PauseRun();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) PauseRun();
        }

        public void PauseRun()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            player.enabled = false;
            spawner.enabled = false;
            Time.timeScale = 0f;
            GameEvents.RaiseGamePaused();
        }

        public void ResumeRun()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;
            player.enabled = true;
            spawner.enabled = true;
            Time.timeScale = 1f;
            GameEvents.RaiseGameResumed();
        }

        private void FinalizeRun()
        {
            RunResult result = BuildResult();
            var data = SaveSystem.Data;

            if (result.IsNewRecord)
            {
                data.SetBestDistanceFor(ActiveMode, result.DistanceMeters);
            }
            if (result.IsNewSurvivalRecord)
            {
                data.bestSurvivalModeSeconds = result.SurvivalModeSeconds;
            }
            data.lifetimeDistanceMeters += result.DistanceMeters;
            data.totalRuns += 1;

            data.recentRuns.Insert(0, new RunLogEntry
            {
                mode = ActiveConfig.DisplayName,
                distanceMeters = result.DistanceMeters,
                coinsEarned = result.CoinsCollected,
                survivalModeSeconds = result.SurvivalModeSeconds,
                zoneReached = zoneManager.CurrentZone != null ? zoneManager.CurrentZone.Name : ZoneCatalog.Zones[0].Name,
                endCause = result.Cause.ToString(),
                dateIso = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            });
            if (data.recentRuns.Count > 50)
            {
                data.recentRuns.RemoveRange(50, data.recentRuns.Count - 50);
            }
            SaveSystem.Save();

            if (ActiveMode == RunModeType.Daily)
            {
                DailyRunManager.Instance.ReportRun(result.DistanceMeters);
            }

            LastResult = result;
            AdsManager.Instance.NotifyRunCompleted();
            GameEvents.RaiseRunEnded(result);
        }
    }
}

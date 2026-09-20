using System;
using OnePercentBattery.Save;
using OnePercentBattery.Utils;
using UnityEngine;

namespace OnePercentBattery.Services
{
    /// <summary>
    /// Game-facing advertising entry point - the only ads class gameplay/UI code should talk to.
    /// Owns SDK initialization, the interstitial frequency cap, "only one pending reward at a time"
    /// tracking, the AdsRemoved gate, and muting audio for the duration of a fullscreen ad.
    ///
    /// GAME SYSTEMS -&gt; AdsManager (this class) -&gt; IAdService -&gt; Yodo1MasAdService -&gt; Yodo1 MAS SDK.
    ///
    /// In the Editor this always uses <see cref="FakeAdService"/> so ads can be tested without a
    /// device or a real network - see that class for why it's compiled out of device builds.
    /// </summary>
    public sealed class AdsManager : Singleton<AdsManager>
    {
        [SerializeField] private FakeAdService editorAdService;
        [SerializeField] private Yodo1MasAdService deviceAdService;
        [SerializeField] private Yodo1AdsConfig config;

        public bool IsInitialized => _service != null && _service.IsInitialized;

        private IAdService _service;
        private RewardedAdPlacement? _pendingRewardPlacement;
        private bool _rewardedShowInFlight;
        private bool _interstitialShowInFlight;

        private int _completedRuns;
        private int _runsSinceLastInterstitial;
        private float _lastInterstitialRealtime = float.NegativeInfinity;

        protected override void Awake()
        {
            base.Awake();
#if UNITY_EDITOR
            _service = editorAdService;
#else
            _service = deviceAdService != null ? (IAdService)deviceAdService : editorAdService;
#endif
        }

        private void Start()
        {
            Initialize();
        }

        /// <summary>Safe to call more than once - only the first call does anything (see <see cref="IsInitialized"/>).</summary>
        public void Initialize()
        {
            if (_service == null || IsInitialized) return;

            Debug.Log("[Yodo1MAS] Initializing");
            _service.Initialize(() =>
            {
                Debug.Log(_service.IsInitialized
                    ? "[Yodo1MAS] Initialization successful"
                    : "[Yodo1MAS] Initialization failed or no SDK present - ads will report as unavailable.");
            });
        }

        // ----- Rewarded -----

        public bool IsRewardedAvailable(RewardedAdPlacement placement)
        {
            return AdsEnabled(rewarded: true) && _service != null && _service.IsRewardedAdReady && !_rewardedShowInFlight;
        }

        /// <summary>
        /// onRewardEarned fires ONLY for the real reward-earned callback (never for opened/started/
        /// closed). A stale callback for a placement that's no longer pending - e.g. a duplicate
        /// SDK event - is dropped rather than granting the wrong reward.
        /// </summary>
        public void ShowRewarded(RewardedAdPlacement placement, Action onRewardEarned, Action onFailedOrSkipped)
        {
            if (_rewardedShowInFlight || !IsRewardedAvailable(placement))
            {
                Debug.Log($"[Yodo1MAS] Rewarded not available for {placement}");
                onFailedOrSkipped?.Invoke();
                return;
            }

            _rewardedShowInFlight = true;
            _pendingRewardPlacement = placement;
            Debug.Log($"[Yodo1MAS] Rewarded requested: {placement}");
            MuteForAd();

            _service.ShowRewardedAd(placement,
                onRewardEarned: () =>
                {
                    UnmuteAfterAd();
                    _rewardedShowInFlight = false;
                    if (_pendingRewardPlacement != placement)
                    {
                        Debug.LogWarning($"[Yodo1MAS] Dropped reward callback for {placement} - it was no longer the pending placement.");
                        return;
                    }
                    _pendingRewardPlacement = null;
                    Debug.Log($"[Yodo1MAS] Reward earned: {placement}");
                    onRewardEarned?.Invoke();
                },
                onFailedOrSkipped: () =>
                {
                    UnmuteAfterAd();
                    _rewardedShowInFlight = false;
                    _pendingRewardPlacement = null;
                    Debug.Log($"[Yodo1MAS] Rewarded failed/skipped: {placement}");
                    onFailedOrSkipped?.Invoke();
                });
        }

        // ----- Interstitial -----

        /// <summary>Call exactly once per run that truly ends (not on a revived death) - see GameManager.FinalizeRun.</summary>
        public void NotifyRunCompleted()
        {
            _completedRuns++;
            _runsSinceLastInterstitial++;
        }

        public bool IsInterstitialDue()
        {
            if (!AdsEnabled(rewarded: false) || config == null) return false;
            if (_service == null || !_service.IsInterstitialAdReady) return false;
            if (_completedRuns < config.MinimumRunsBeforeFirstInterstitial) return false;
            if (_runsSinceLastInterstitial < config.RunsBetweenInterstitials) return false;
            if (Time.unscaledTime - _lastInterstitialRealtime < config.InterstitialCooldownSeconds) return false;
            return true;
        }

        /// <summary>
        /// Shows an interstitial if (and only if) the frequency cap allows it right now, then always
        /// calls `continuation` - unavailable/failed/disabled all continue immediately, never blocking
        /// the Retry/Home flow the spec calls out as highest priority.
        /// </summary>
        public void MaybeShowInterstitial(Action continuation)
        {
            if (_interstitialShowInFlight || !IsInterstitialDue())
            {
                continuation?.Invoke();
                return;
            }

            _interstitialShowInFlight = true;
            _runsSinceLastInterstitial = 0;
            _lastInterstitialRealtime = Time.unscaledTime;
            Debug.Log("[Yodo1MAS] Interstitial requested");
            MuteForAd();

            _service.ShowInterstitialAd(() =>
            {
                UnmuteAfterAd();
                _interstitialShowInFlight = false;
                Debug.Log("[Yodo1MAS] Interstitial closed");
                continuation?.Invoke();
            });
        }

        // ----- Banner -----

        public void ShowBanner()
        {
            if (!AdsEnabled(rewarded: false) || _service == null || !_service.SupportsBanner) return;
            _service.ShowBanner();
        }

        public void HideBanner()
        {
            _service?.HideBanner();
        }

        // ----- Gates -----

        private bool AdsEnabled(bool rewarded)
        {
            if (config == null) return true;
            if (rewarded) return config.EnableRewardedAds;
            // Rewarded ads stay available even with AdsRemoved - they're opt-in and give an explicit reward.
            return !SaveSystem.Data.adsRemoved && (config.EnableInterstitialAds || config.EnableBannerAds);
        }

        private static void MuteForAd()
        {
            // AudioListener.pause is independent of AudioListener.volume (the Settings "Sound"
            // toggle's gate), so restoring it afterwards can never re-enable audio the player had
            // off, and never needs to remember/restore a volume value.
            AudioListener.pause = true;
        }

        private static void UnmuteAfterAd()
        {
            AudioListener.pause = false;
        }
    }
}

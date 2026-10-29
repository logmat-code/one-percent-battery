using System;
using System.Collections.Generic;
using UnityEngine;
using Yodo1.MAS;

namespace OnePercentBattery.Services
{
    /// <summary>
    /// Device-facing <see cref="IAdService"/> adapter backed by the official Yodo1 MAS Unity SDK (v4.18.1).
    ///
    /// PROJECT MONETIZATION IDENTITY:
    /// - Android App Key: WTDc7gSXr1 (configured in Yodo1AdsConfig.asset and Resources/Yodo1/Yodo1AdSettings.asset)
    /// - AdMob Application ID: ca-app-pub-5784311004955543~4734304503 (configured in AndroidManifest.xml)
    /// - Package Name: com.logmat.onepercentbattery
    ///
    /// REWARDED AD SPECIFICATION:
    /// - Flow 1 (Main Menu): Watch Ad -> Bonus Coins (+25 coins)
    /// - Flow 2 (Game Over): Second Chance -> Revive (restores player, run distance, zone, coins, HUD)
    /// - Flow 3 (Game Over): Double Coins -> Doubles current run coin reward exactly once
    ///
    /// REWARD LIFECYCLE & FAIRNESS:
    /// - Rewards are granted ONLY after receiving the official SDK OnAdEarnedEvent callback.
    /// - Canceling, closing early, or failing to open an ad will NEVER grant a reward.
    /// - Single in-flight ad protection prevents multiple overlapping ad requests.
    /// - The next rewarded ad is automatically preloaded after an ad finishes or closes.
    /// </summary>
    public sealed class Yodo1MasAdService : MonoBehaviour, IAdService
    {
        [SerializeField] private Yodo1AdsConfig config;

        public bool IsInitialized { get; private set; }

        public bool IsRewardedAdReady
        {
            get
            {
                if (!IsInitialized) return false;
                return Yodo1U3dRewardAd.GetInstance().IsLoaded();
            }
        }

        public bool IsInterstitialAdReady
        {
            get
            {
                if (!IsInitialized) return false;
                return Yodo1U3dInterstitialAd.GetInstance().IsLoaded();
            }
        }

        public bool SupportsBanner => true;

        private bool _initStarted;
        private readonly List<Action> _initCompletionCallbacks = new List<Action>();
        private bool _eventsSubscribed;

        // Rewarded in-flight tracking
        private bool _rewardedInFlight;
        private bool _rewardEarned;
        private RewardedAdPlacement _currentRewardedPlacement;
        private Action _currentRewardEarnedCallback;
        private Action _currentRewardFailedOrSkippedCallback;

        // Interstitial in-flight tracking
        private bool _interstitialInFlight;
        private Action _currentInterstitialClosedCallback;

        // Banner view instance
        private Yodo1U3dBannerAdView _bannerAdView;

        public void Initialize(Action onComplete)
        {
            string appKey = config != null ? config.AppKeyForCurrentPlatform() : "";
            Debug.Log($"[ADS] [Yodo1MasAdService] Initialization requested (AppKey: {appKey})");

            if (config == null || string.IsNullOrEmpty(appKey))
            {
                Debug.LogWarning("[ADS] [Yodo1MasAdService] No Yodo1AdsConfig or App Key assigned - ads will stay unavailable.");
                IsInitialized = false;
                onComplete?.Invoke();
                return;
            }

            if (onComplete != null)
            {
                _initCompletionCallbacks.Add(onComplete);
            }

            if (IsInitialized)
            {
                Debug.Log("[ADS] [Yodo1MasAdService] SDK was already initialized.");
                InvokeInitCallbacks();
                return;
            }

            if (Yodo1U3dMasCallback.isInitialized())
            {
                Debug.Log("[ADS] [Yodo1MasAdService] Yodo1U3dMasCallback already reports initialized.");
                IsInitialized = true;
                SubscribeAdEvents();
                PreloadAds();
                InvokeInitCallbacks();
                return;
            }

            if (!_initStarted)
            {
                _initStarted = true;

                // Subscribe to initialization callback
                Yodo1U3dMasCallback.OnSdkInitializationEvent += OnSdkInitializationCompleted;

                // Configure privacy compliance
                Yodo1U3dMas.SetCOPPA(false);
                Yodo1U3dMas.SetGDPR(true);
                Yodo1U3dMas.SetCCPA(false);

                // Configure build settings with UMP and ATT enabled
                var buildConfig = new Yodo1AdBuildConfig()
                    .enableUserPrivacyDialog(false)
                    .enableUserMessageingPlatform(Yodo1MasUMPState.ENABLE)
                    .enableATTAuthorization(true);
                Yodo1U3dMas.SetAdBuildConfig(buildConfig);

                Debug.Log($"[ADS] [Yodo1MasAdService] Starting Yodo1 MAS SDK initialization with App Key: {appKey}");
                Yodo1U3dMas.InitializeMasSdk();
            }
        }

        private void OnSdkInitializationCompleted(Yodo1MasSdkConfiguration configuration, Yodo1U3dAdError error)
        {
            if (configuration != null)
            {
                Debug.Log($"[ADS] [Yodo1MasAdService] Yodo1 MAS SDK initialized successfully: {configuration}");
                IsInitialized = true;
                SubscribeAdEvents();
                PreloadAds();
            }
            else
            {
                Debug.LogError($"[ADS] [Yodo1MasAdService] Yodo1 MAS SDK initialization failed: {error?.ToString()}");
                IsInitialized = false;
            }

            InvokeInitCallbacks();
            AdsManager.Instance?.RefreshAvailability();
        }

        private void InvokeInitCallbacks()
        {
            var callbacks = new List<Action>(_initCompletionCallbacks);
            _initCompletionCallbacks.Clear();
            foreach (var cb in callbacks)
            {
                cb?.Invoke();
            }
        }

        private void SubscribeAdEvents()
        {
            if (_eventsSubscribed) return;
            _eventsSubscribed = true;

            // Rewarded Ad Events
            var rewardAd = Yodo1U3dRewardAd.GetInstance();
            rewardAd.OnAdLoadedEvent += OnRewardedAdLoaded;
            rewardAd.OnAdLoadFailedEvent += OnRewardedAdLoadFailed;
            rewardAd.OnAdOpenedEvent += OnRewardedAdOpened;
            rewardAd.OnAdOpenFailedEvent += OnRewardedAdOpenFailed;
            rewardAd.OnAdEarnedEvent += OnRewardedAdEarned;
            rewardAd.OnAdClosedEvent += OnRewardedAdClosed;

            // Interstitial Ad Events
            var interstitialAd = Yodo1U3dInterstitialAd.GetInstance();
            interstitialAd.OnAdLoadedEvent += OnInterstitialAdLoaded;
            interstitialAd.OnAdLoadFailedEvent += OnInterstitialAdLoadFailed;
            interstitialAd.OnAdOpenedEvent += OnInterstitialAdOpened;
            interstitialAd.OnAdOpenFailedEvent += OnInterstitialAdOpenFailed;
            interstitialAd.OnAdClosedEvent += OnInterstitialAdClosed;
        }

        private void PreloadAds()
        {
            PreloadRewardedAd();
            PreloadInterstitialAd();
        }

        // =========================================================================
        // REWARDED ADS
        // =========================================================================

        public void ShowRewardedAd(RewardedAdPlacement placement, Action onRewardEarned, Action onFailedOrSkipped)
        {
            if (_rewardedInFlight)
            {
                Debug.LogWarning($"[ADS] [Yodo1MasAdService] Rewarded ad show rejected: another rewarded ad is already in flight for {_currentRewardedPlacement}.");
                onFailedOrSkipped?.Invoke();
                return;
            }

            if (!IsRewardedAdReady)
            {
                Debug.LogWarning($"[ADS] [Yodo1MasAdService] Rewarded ad requested for {placement} but ad is not ready. Failing safely.");
                onFailedOrSkipped?.Invoke();
                return;
            }

            _rewardedInFlight = true;
            _rewardEarned = false;
            _currentRewardedPlacement = placement;
            _currentRewardEarnedCallback = onRewardEarned;
            _currentRewardFailedOrSkippedCallback = onFailedOrSkipped;

            string placementName = placement switch
            {
                RewardedAdPlacement.Revive => "Revive",
                RewardedAdPlacement.DoubleCoins => "DoubleCoins",
                RewardedAdPlacement.BonusCoins => "BonusCoins",
                _ => placement.ToString()
            };

            Debug.Log($"[ADS] [Yodo1MasAdService] Presenting real rewarded ad for {placement} (Placement name: '{placementName}')");
            Yodo1U3dRewardAd.GetInstance().ShowAd(placementName);
        }

        private void OnRewardedAdLoaded(Yodo1U3dRewardAd ad)
        {
            Debug.Log("[ADS] [Yodo1MasAdService] Rewarded ad loaded and ready.");
            AdsManager.Instance?.RefreshAvailability();
        }

        private void OnRewardedAdLoadFailed(Yodo1U3dRewardAd ad, Yodo1U3dAdError error)
        {
            Debug.LogWarning($"[ADS] [Yodo1MasAdService] Rewarded ad failed to load: {error?.ToString()}");
            AdsManager.Instance?.RefreshAvailability();
        }

        private void OnRewardedAdOpened(Yodo1U3dRewardAd ad)
        {
            Debug.Log($"[ADS] [Yodo1MasAdService] Rewarded ad opened on screen for {_currentRewardedPlacement}.");
        }

        private void OnRewardedAdOpenFailed(Yodo1U3dRewardAd ad, Yodo1U3dAdError error)
        {
            Debug.LogWarning($"[ADS] [Yodo1MasAdService] Rewarded ad failed to open: {error?.ToString()}");
            if (_rewardedInFlight)
            {
                Action failedCb = _currentRewardFailedOrSkippedCallback;
                ResetRewardedState();
                failedCb?.Invoke();
            }
            PreloadRewardedAd();
            AdsManager.Instance?.RefreshAvailability();
        }

        private void OnRewardedAdEarned(Yodo1U3dRewardAd ad)
        {
            Debug.Log($"[ADS] [Yodo1MasAdService] Official OnAdEarnedEvent callback received for {_currentRewardedPlacement}!");
            _rewardEarned = true;
        }

        private void OnRewardedAdClosed(Yodo1U3dRewardAd ad)
        {
            Debug.Log($"[ADS] [Yodo1MasAdService] Rewarded ad closed for {_currentRewardedPlacement}. RewardEarned={_rewardEarned}");
            if (_rewardedInFlight)
            {
                Action earnedCb = _currentRewardEarnedCallback;
                Action failedCb = _currentRewardFailedOrSkippedCallback;
                bool wasEarned = _rewardEarned;

                ResetRewardedState();

                if (wasEarned)
                {
                    Debug.Log($"[ADS] [Yodo1MasAdService] Valid rewarded completion confirmed. Granting reward.");
                    earnedCb?.Invoke();
                }
                else
                {
                    Debug.LogWarning($"[ADS] [Yodo1MasAdService] Rewarded ad was skipped or closed before completion. No reward granted.");
                    failedCb?.Invoke();
                }
            }

            PreloadRewardedAd();
            AdsManager.Instance?.RefreshAvailability();
        }

        private void ResetRewardedState()
        {
            _rewardedInFlight = false;
            _rewardEarned = false;
            _currentRewardEarnedCallback = null;
            _currentRewardFailedOrSkippedCallback = null;
        }

        public void PreloadRewardedAd()
        {
            if (!IsInitialized) return;
            if (Yodo1U3dRewardAd.GetInstance().IsLoaded()) return;

            Debug.Log("[ADS] [Yodo1MasAdService] Preloading next rewarded ad...");
            Yodo1U3dRewardAd.GetInstance().LoadAd();
        }

        // =========================================================================
        // INTERSTITIAL ADS
        // =========================================================================

        public void ShowInterstitialAd(Action onClosed)
        {
            if (_interstitialInFlight)
            {
                Debug.LogWarning("[ADS] [Yodo1MasAdService] Interstitial already in flight - continuing immediately.");
                onClosed?.Invoke();
                return;
            }

            if (!IsInterstitialAdReady)
            {
                Debug.LogWarning("[ADS] [Yodo1MasAdService] Interstitial ad not ready - continuing immediately.");
                onClosed?.Invoke();
                return;
            }

            _interstitialInFlight = true;
            _currentInterstitialClosedCallback = onClosed;
            Debug.Log("[ADS] [Yodo1MasAdService] Presenting real interstitial ad.");
            Yodo1U3dInterstitialAd.GetInstance().ShowAd();
        }

        private void OnInterstitialAdLoaded(Yodo1U3dInterstitialAd ad)
        {
            Debug.Log("[ADS] [Yodo1MasAdService] Interstitial ad loaded and ready.");
            AdsManager.Instance?.RefreshAvailability();
        }

        private void OnInterstitialAdLoadFailed(Yodo1U3dInterstitialAd ad, Yodo1U3dAdError error)
        {
            Debug.LogWarning($"[ADS] [Yodo1MasAdService] Interstitial ad failed to load: {error?.ToString()}");
            AdsManager.Instance?.RefreshAvailability();
        }

        private void OnInterstitialAdOpened(Yodo1U3dInterstitialAd ad)
        {
            Debug.Log("[ADS] [Yodo1MasAdService] Interstitial ad opened on screen.");
        }

        private void OnInterstitialAdOpenFailed(Yodo1U3dInterstitialAd ad, Yodo1U3dAdError error)
        {
            Debug.LogWarning($"[ADS] [Yodo1MasAdService] Interstitial ad failed to open: {error?.ToString()}");
            FinishInterstitial();
        }

        private void OnInterstitialAdClosed(Yodo1U3dInterstitialAd ad)
        {
            Debug.Log("[ADS] [Yodo1MasAdService] Interstitial ad closed.");
            FinishInterstitial();
        }

        private void FinishInterstitial()
        {
            if (_interstitialInFlight)
            {
                Action cb = _currentInterstitialClosedCallback;
                _interstitialInFlight = false;
                _currentInterstitialClosedCallback = null;
                cb?.Invoke();
            }

            PreloadInterstitialAd();
            AdsManager.Instance?.RefreshAvailability();
        }

        public void PreloadInterstitialAd()
        {
            if (!IsInitialized) return;
            if (Yodo1U3dInterstitialAd.GetInstance().IsLoaded()) return;

            Debug.Log("[ADS] [Yodo1MasAdService] Preloading next interstitial ad...");
            Yodo1U3dInterstitialAd.GetInstance().LoadAd();
        }

        // =========================================================================
        // BANNER ADS
        // =========================================================================

        public void ShowBanner()
        {
            if (!IsInitialized) return;

            if (_bannerAdView == null)
            {
                Debug.Log("[ADS] [Yodo1MasAdService] Creating and loading banner ad view.");
                _bannerAdView = new Yodo1U3dBannerAdView(
                    Yodo1U3dBannerAdSize.Banner,
                    Yodo1U3dBannerAdPosition.BannerBottom | Yodo1U3dBannerAdPosition.BannerHorizontalCenter);
                _bannerAdView.LoadAd();
            }
            else
            {
                Debug.Log("[ADS] [Yodo1MasAdService] Showing existing banner ad view.");
                _bannerAdView.Show();
            }
        }

        public void HideBanner()
        {
            if (_bannerAdView != null)
            {
                Debug.Log("[ADS] [Yodo1MasAdService] Hiding banner ad view.");
                _bannerAdView.Hide();
            }
        }

        private void OnDestroy()
        {
            if (_bannerAdView != null)
            {
                _bannerAdView.Destroy();
                _bannerAdView = null;
            }
        }
    }
}

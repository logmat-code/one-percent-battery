using System;
using UnityEngine;

namespace OnePercentBattery.Services
{
    /// <summary>
    /// Device-facing <see cref="IAdService"/> adapter for the Yodo1 MAS SDK.
    ///
    /// IMPORTANT - READ BEFORE WIRING THIS UP:
    /// This project does not currently contain the real Yodo1 MAS Unity SDK. The only "Yodo1"
    /// references found anywhere under Assets/ are UGameStudio's own placeholder/config classes
    /// (Assets/UGameStudio/AdsManagerForYodo1MAS/Yodo1AdsManager.cs and friends, plus the
    /// AdsSettingsForYodo1Ads ScriptableObject) - they hold placement IDs and reward lists but do
    /// not call any ad-serving API, and their base class (UGS_AdsSettingsForYodo1Ads) is compiled
    /// into UGameStudio's closed-source UGS_T.dll, not present as inspectable/callable source.
    /// There is no Yodo1 package in Packages/manifest.json, no Assets/Yodo1U3dMas (or similar) SDK
    /// folder, and no Assets/Plugins/Android directory, so none of the native Android integration a
    /// real Yodo1 MAS install requires (AAR, manifest entries, Gradle dependency) is present either.
    ///
    /// Rather than guess at Yodo1's real class/method names (which would not compile against
    /// anything in this project, and risks being silently wrong if it did), this adapter is wired
    /// into the full AdsManager/GameOverController flow as a safe "not available" implementation:
    /// gameplay treats a missing ad exactly like a network failure, which is a real Y case the spec
    /// itself requires handling anyway. Once you import the official Yodo1 MAS Unity SDK
    /// (https://www.yodo1.com/mas - Unity package or .unitypackage from your Yodo1 dashboard), fill
    /// in the six TODOs below using that SDK's own README/sample scene as the source of truth for
    /// exact method names - do not carry over names from older Yodo1 versions or other ad networks.
    /// </summary>
    public sealed class Yodo1MasAdService : MonoBehaviour, IAdService
    {
        [SerializeField] private Yodo1AdsConfig config;

        public bool IsInitialized { get; private set; }
        public bool IsRewardedAdReady => false;
        public bool IsInterstitialAdReady => false;
        public bool SupportsBanner => false;

        public void Initialize(Action onComplete)
        {
            if (config == null || string.IsNullOrEmpty(config.AppKeyForCurrentPlatform()))
            {
                Debug.LogWarning("[Yodo1MasAdService] No Yodo1AdsConfig / App Key assigned - ads will stay unavailable. " +
                                  "This does not block the game: see IAdService's contract.");
                IsInitialized = false;
                onComplete?.Invoke();
                return;
            }

            // TODO(YODO1-SDK): once the real SDK is imported, initialize it here with
            // config.AppKeyForCurrentPlatform() and config.EnableTestMode, subscribe this class to
            // its initialization callback, and only set IsInitialized = true from that callback
            // (success or failure) - never assume success synchronously. Log both outcomes.
            Debug.LogWarning("[Yodo1MasAdService] Real Yodo1 MAS SDK not present in this project - " +
                              "initialization is a no-op. See the class header comment for details.");
            IsInitialized = false;
            onComplete?.Invoke();
        }

        public void ShowRewardedAd(RewardedAdPlacement placement, Action onRewardEarned, Action onFailedOrSkipped)
        {
            // TODO(YODO1-SDK): request/show the rewarded ad for `placement` here. Call
            // onRewardEarned() ONLY from the SDK's reward-earned callback, never from "ad opened"
            // or "ad closed" - see AdsManager for why that distinction matters (one-revive-per-run,
            // double-coins-once). Call onFailedOrSkipped() for every other outcome (unavailable, load
            // failure, show failure, closed without earning the reward).
            Debug.LogWarning($"[Yodo1MasAdService] Rewarded ad requested for {placement} but no SDK is installed - failing safely.");
            onFailedOrSkipped?.Invoke();
        }

        public void ShowInterstitialAd(Action onClosed)
        {
            // TODO(YODO1-SDK): show the interstitial here and call onClosed() when the SDK reports
            // the ad closed (or immediately if it fails to load/show - interstitials must never
            // block the Retry/Home flow).
            Debug.LogWarning("[Yodo1MasAdService] Interstitial requested but no SDK is installed - continuing immediately.");
            onClosed?.Invoke();
        }

        public void ShowBanner()
        {
            // TODO(YODO1-SDK): show the SDK's banner view here, anchored so it cannot cover
            // gameplay-critical UI (see AdsManager.ShowBanner, which already guarantees this is
            // only ever called outside active gameplay).
            Debug.LogWarning("[Yodo1MasAdService] Banner requested but no SDK is installed - ignoring.");
        }

        public void HideBanner()
        {
            // TODO(YODO1-SDK): hide the SDK's banner view here.
        }
    }
}

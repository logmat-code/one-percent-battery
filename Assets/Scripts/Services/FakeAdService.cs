using System;
using System.Collections;
using UnityEngine;

namespace OnePercentBattery.Services
{
    /// <summary>
    /// Editor/development stand-in: "shows" an ad by waiting briefly, then always succeeds. Active
    /// only in the Editor or development builds (see <see cref="AdsManager"/>) so a real device
    /// release build can never ship with a fake automatic reward.
    /// </summary>
    public sealed class FakeAdService : MonoBehaviour, IAdService
    {
        [SerializeField] private float simulatedAdSeconds = 1.5f;

        public bool IsInitialized { get; private set; }
        public bool IsRewardedAdReady => IsInitialized;
        public bool IsInterstitialAdReady => IsInitialized;
        public bool SupportsBanner => true;

        public void Initialize(Action onComplete)
        {
            IsInitialized = true;
            Debug.Log("[FakeAdService] Initialized (editor/dev simulation - not a real ad network).");
            onComplete?.Invoke();
        }

        public void ShowRewardedAd(RewardedAdPlacement placement, Action onRewardEarned, Action onFailedOrSkipped)
        {
            Debug.Log($"[FakeAdService] Simulating rewarded ad for {placement}.");
            StartCoroutine(SimulateAd(onRewardEarned));
        }

        public void ShowInterstitialAd(Action onClosed)
        {
            Debug.Log("[FakeAdService] Simulating interstitial ad.");
            StartCoroutine(SimulateInterstitial(onClosed));
        }

        public void ShowBanner()
        {
            Debug.Log("[FakeAdService] Banner shown (simulated).");
        }

        public void HideBanner()
        {
            Debug.Log("[FakeAdService] Banner hidden (simulated).");
        }

        private IEnumerator SimulateAd(Action onRewardEarned)
        {
            yield return new WaitForSecondsRealtime(simulatedAdSeconds);
            onRewardEarned?.Invoke();
        }

        private IEnumerator SimulateInterstitial(Action onClosed)
        {
            yield return new WaitForSecondsRealtime(simulatedAdSeconds);
            onClosed?.Invoke();
        }
    }
}

using System;
using System.Collections;
using UnityEngine;

namespace OnePercentBattery.Services
{
    /// <summary>
    /// Editor stand-in: "shows" an ad by waiting briefly, then always succeeds. This class has no
    /// build-time guard of its own - the safety net is entirely <see cref="AdsManager"/>.Awake(),
    /// which only ever assigns this to `_service` inside `#if UNITY_EDITOR`. A device build falls
    /// back to this ONLY if `deviceAdService` is left unassigned in the Inspector - verify that
    /// field stays wired to a real IAdService (currently Yodo1MasAdService) before every release
    /// build, since that's the only thing preventing a fake automatic reward from reaching players.
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

using System;
using System.Collections;
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

        [Header("Safety")]
        [Tooltip("Watchdog ceiling used only while FakeAdService (Editor/dev) is the active service - safe to keep short since its own simulated delay is a known, fixed ~1-2s. Covers a lost/never-fired callback (e.g. a domain reload killing its coroutine mid-show) so a run transition can never hang indefinitely in the Editor.")]
        [SerializeField] private float editorAdWatchdogSeconds = 8f;
        [Tooltip("Watchdog ceiling used whenever a real ad service (anything other than FakeAdService, i.e. a future real Yodo1 MAS interstitial) is active. Deliberately long: a real interstitial can legitimately stay on screen well past 8s, and this must never fire while an ad the player can see is still up - it is an EMERGENCY deadlock escape for a genuinely stuck/lost SDK callback, not a normal ad-duration limit. IAdService currently exposes only IsInterstitialAdReady/ShowInterstitialAd(onClosed) - no separate \"is currently visible\" signal - so a long ceiling is the safe choice without inventing visibility-tracking the SDK doesn't cleanly provide.")]
        [SerializeField] private float realAdEmergencyWatchdogSeconds = 60f;

        public static event Action AdAvailabilityChanged;

        public bool IsInitialized => _service != null && _service.IsInitialized;

        private IAdService _service;
        private RewardedAdPlacement? _pendingRewardPlacement;
        private bool _rewardedShowInFlight;
        private bool _interstitialShowInFlight;
        private Coroutine _interstitialWatchdogRoutine;
        private Action _pendingInterstitialContinuation;

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

            string appKey = config != null ? config.AppKeyForCurrentPlatform() : "";
            bool testMode = config != null && config.EnableTestMode;
            Debug.Log($"[ADS] SDK initialization started (Service: {_service.GetType().Name}, AppKey: {appKey}, TestMode: {testMode})");
            _service.Initialize(() =>
            {
                if (_service.IsInitialized)
                {
                    Debug.Log($"[ADS] SDK initialization successful ({_service.GetType().Name}).");
                }
                else
                {
                    Debug.LogWarning($"[ADS] SDK initialization failed or no SDK present ({_service.GetType().Name}) - ads will report as unavailable.");
                }
                NotifyAvailabilityChanged();
            });
        }

        public void RefreshAvailability()
        {
            NotifyAvailabilityChanged();
        }

        private void NotifyAvailabilityChanged()
        {
            Debug.Log($"[ADS] Ad availability changed: Rewarded(BonusCoins={IsRewardedAvailable(RewardedAdPlacement.BonusCoins)}, " +
                      $"Revive={IsRewardedAvailable(RewardedAdPlacement.Revive)}, DoubleCoins={IsRewardedAvailable(RewardedAdPlacement.DoubleCoins)}), " +
                      $"InterstitialReady={_service?.IsInterstitialAdReady}");
            AdAvailabilityChanged?.Invoke();
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
            if (_rewardedShowInFlight)
            {
                Debug.LogWarning($"[ADS] Rewarded ad show rejected: another ad is already in flight for {placement}. Duplicate taps blocked.");
                onFailedOrSkipped?.Invoke();
                return;
            }

            if (!IsRewardedAvailable(placement))
            {
                bool adsEnabled = AdsEnabled(rewarded: true);
                bool ready = _service != null && _service.IsRewardedAdReady;
                Debug.LogWarning($"[ADS] Rewarded ad show rejected: not available for {placement} (adsEnabled={adsEnabled}, ready={ready}).");
                onFailedOrSkipped?.Invoke();
                return;
            }

            _rewardedShowInFlight = true;
            _pendingRewardPlacement = placement;
            NotifyAvailabilityChanged();
            Debug.Log($"[ADS] Rewarded ad show requested: {placement}");
            MuteForAd();

            _service.ShowRewardedAd(placement,
                onRewardEarned: () =>
                {
                    UnmuteAfterAd();
                    _rewardedShowInFlight = false;
                    if (_pendingRewardPlacement != placement)
                    {
                        Debug.LogWarning($"[ADS] Dropped reward callback for {placement} - it was no longer the pending placement (pending was {_pendingRewardPlacement}). Duplicate protection active.");
                        NotifyAvailabilityChanged();
                        return;
                    }
                    _pendingRewardPlacement = null;
                    Debug.Log($"[ADS] Reward earned: {placement}. Granting reward.");
                    onRewardEarned?.Invoke();
                    NotifyAvailabilityChanged();
                },
                onFailedOrSkipped: () =>
                {
                    UnmuteAfterAd();
                    _rewardedShowInFlight = false;
                    _pendingRewardPlacement = null;
                    Debug.Log($"[ADS] Rewarded ad failed/skipped/closed without reward: {placement}");
                    onFailedOrSkipped?.Invoke();
                    NotifyAvailabilityChanged();
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
        /// the Retry/Home flow the spec calls out as highest priority. Once a show attempt actually
        /// starts, a watchdog (see <see cref="CurrentInterstitialWatchdogSeconds"/>/<see cref="CompleteInterstitial"/>)
        /// guarantees `continuation` still fires even if the ad service's own callback never arrives -
        /// e.g. FakeAdService's simulated delay never running to completion, or a real SDK callback
        /// that gets lost - so this can never leave a Retry/Home transition waiting forever.
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
            _pendingInterstitialContinuation = continuation;
            Debug.Log("[ADS] Interstitial show requested");
            MuteForAd();

            _interstitialWatchdogRoutine = StartCoroutine(InterstitialWatchdog(CurrentInterstitialWatchdogSeconds));
            _service.ShowInterstitialAd(() => CompleteInterstitial("closed"));
        }

        /// <summary>
        /// FakeAdService's own delay is a known, short, fixed quantity, so its watchdog can stay short
        /// too - but a real ad service's actual on-screen time isn't something this watchdog should ever
        /// race against, so anything other than FakeAdService gets the long emergency ceiling instead.
        /// </summary>
        private float CurrentInterstitialWatchdogSeconds => _service is FakeAdService ? editorAdWatchdogSeconds : realAdEmergencyWatchdogSeconds;

        private IEnumerator InterstitialWatchdog(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            Debug.LogWarning($"[ADS] Interstitial callback never arrived within the {seconds}s watchdog window - continuing without waiting further.");
            CompleteInterstitial("timed out");
        }

        /// <summary>
        /// The one place either the real ad-closed callback or the watchdog timeout ends an
        /// interstitial attempt. Guarded on <see cref="_interstitialShowInFlight"/> so whichever of the
        /// two fires FIRST wins and the other is a no-op - `continuation` is invoked exactly once no
        /// matter which path completes it, and a stray late callback after a timeout can't double-fire it.
        /// </summary>
        private void CompleteInterstitial(string reason)
        {
            if (!_interstitialShowInFlight) return;

            if (_interstitialWatchdogRoutine != null)
            {
                StopCoroutine(_interstitialWatchdogRoutine);
                _interstitialWatchdogRoutine = null;
            }

            UnmuteAfterAd();
            _interstitialShowInFlight = false;
            Debug.Log($"[ADS] Interstitial {reason}");

            Action continuation = _pendingInterstitialContinuation;
            _pendingInterstitialContinuation = null;
            continuation?.Invoke();
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

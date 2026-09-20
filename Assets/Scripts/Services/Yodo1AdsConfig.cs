using UnityEngine;

namespace OnePercentBattery.Services
{
    /// <summary>
    /// Single home for every advertising setting - nothing ad-related should be hardcoded in
    /// gameplay scripts. Create one instance via Assets > Create > 1% Battery > Yodo1 Ads Config.
    /// </summary>
    [CreateAssetMenu(fileName = "Yodo1AdsConfig", menuName = "1% Battery/Yodo1 Ads Config")]
    public sealed class Yodo1AdsConfig : ScriptableObject
    {
        [Header("Yodo1 MAS App Key (see Yodo1 dashboard)")]
        [Tooltip("Paste the Android App Key from your Yodo1 MAS dashboard here. Leave blank in the Editor - FakeAdService is used instead.")]
        [SerializeField] private string androidAppKey = "";
        [Tooltip("Paste the iOS App Key from your Yodo1 MAS dashboard here, if/when an iOS build is planned.")]
        [SerializeField] private string iosAppKey = "";

        [Header("Feature toggles")]
        [SerializeField] private bool enableRewardedAds = true;
        [SerializeField] private bool enableInterstitialAds = true;
        [SerializeField] private bool enableBannerAds = true;

        [Header("Test mode - must be OFF before a store release build")]
        [SerializeField] private bool enableTestMode = true;

        [Header("Interstitial frequency cap (see AdsManager)")]
        [Tooltip("Completed runs before the very first interstitial can ever show.")]
        [SerializeField] private int minimumRunsBeforeFirstInterstitial = 3;
        [Tooltip("Completed runs required between one interstitial and the next.")]
        [SerializeField] private int runsBetweenInterstitials = 3;
        [Tooltip("Minimum real-time seconds required between one interstitial and the next, regardless of run count.")]
        [SerializeField] private float interstitialCooldownSeconds = 150f;

        public string AndroidAppKey => androidAppKey;
        public string IosAppKey => iosAppKey;

        public bool EnableRewardedAds => enableRewardedAds;
        public bool EnableInterstitialAds => enableInterstitialAds;
        public bool EnableBannerAds => enableBannerAds;
        public bool EnableTestMode => enableTestMode;

        public int MinimumRunsBeforeFirstInterstitial => minimumRunsBeforeFirstInterstitial;
        public int RunsBetweenInterstitials => runsBetweenInterstitials;
        public float InterstitialCooldownSeconds => interstitialCooldownSeconds;

        public string AppKeyForCurrentPlatform()
        {
#if UNITY_IOS
            return iosAppKey;
#else
            return androidAppKey;
#endif
        }
    }
}

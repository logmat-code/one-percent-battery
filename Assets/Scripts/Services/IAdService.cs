using System;

namespace OnePercentBattery.Services
{
    /// <summary>Which reward a rewarded-ad show call is for - lets one callback path grant the right reward.</summary>
    public enum RewardedAdPlacement
    {
        Revive,
        DoubleCoins,
        BonusCoins
    }

    /// <summary>
    /// Raw ad-network abstraction. <see cref="AdsManager"/> is the game-facing entry point (frequency
    /// caps, pending-reward tracking, ads-removed state); this interface is the thin adapter beneath
    /// it, implemented by <see cref="FakeAdService"/> (editor/dev) and <see cref="Yodo1MasAdService"/>
    /// (device). Gameplay code should call <see cref="AdsManager"/>, not this interface, directly.
    /// </summary>
    public interface IAdService
    {
        bool IsInitialized { get; }
        void Initialize(Action onComplete);

        bool IsRewardedAdReady { get; }
        void ShowRewardedAd(RewardedAdPlacement placement, Action onRewardEarned, Action onFailedOrSkipped);

        bool IsInterstitialAdReady { get; }
        void ShowInterstitialAd(Action onClosed);

        bool SupportsBanner { get; }
        void ShowBanner();
        void HideBanner();
    }
}

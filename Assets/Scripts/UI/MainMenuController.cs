using OnePercentBattery.Core;
using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using OnePercentBattery.Save;
using OnePercentBattery.Services;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>Home screen: mode select, Let's Run / Daily Run, top bar stats, and entry points into the other panels.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [Tooltip("Optional - if assigned, Show() fades the panel in instead of popping it instantly.")]
        [SerializeField] private PanelFadeTransition transition;

        [Header("Top bar")]
        [SerializeField] private Text coinsText;
        [Tooltip("Despite the field name (kept as-is to avoid re-wiring the scene reference), this shows the BEST FOR THE CURRENTLY SELECTED MODE, not the overall best - see RefreshBestForSelectedMode.")]
        [SerializeField] private Text personalBestText;
        [SerializeField] private Text dayStreakText;

        [Header("Mode select")]
        [SerializeField] private GameObject easyHighlight;
        [SerializeField] private GameObject frenzyHighlight;
        [SerializeField] private GameObject proHighlight;
        [SerializeField] private Image easyButtonImage;
        [SerializeField] private Image frenzyButtonImage;
        [SerializeField] private Image proButtonImage;
        [SerializeField] private Color modeSelectedColor = new Color(0.09f, 0.13f, 0.135f);
        [SerializeField] private Color modeUnselectedColor = new Color(0.09f, 0.13f, 0.135f);
        [SerializeField] private Text easyButtonLabel;
        [SerializeField] private Text frenzyButtonLabel;
        [SerializeField] private Text proButtonLabel;
        [SerializeField] private Color modeSelectedTextColor = new Color(0.835f, 1f, 0.643f);
        [SerializeField] private Color modeUnselectedTextColor = new Color(0.957f, 0.973f, 1f);
        [SerializeField] private Text modeTaglineText;
        [SerializeField] private Text modeRulesText;

        [Header("Claims")]
        [SerializeField] private Button claimDailyBonusButton;
        [SerializeField] private Button claimGiftButton;
        [SerializeField] private Button watchAdButton;

        [Header("Panels")]
        [SerializeField] private GameObject shopPanelRoot;
        [SerializeField] private GameObject missionsPanelRoot;
        [SerializeField] private GameObject profilePanelRoot;
        [SerializeField] private GameObject settingsPanelRoot;

        [Tooltip("Optional - the gameplay World's ground (with its ParallaxLayer) renders at a fixed world position with nothing gating its visibility, so without this it shows through as a stray bar behind the menu's own art. Hidden while the menu is up, shown again the moment a run actually starts.")]
        [SerializeField] private GameObject gameplayGroundRoot;

        private RunModeType _selectedMode = RunModeType.Easy;

        private void Awake()
        {
            // Subscribed here rather than OnEnable: once Hide() disables this GameObject,
            // OnEnable would never fire again to re-subscribe, so this would stop hearing
            // ReturnedToMenu after the very first run.
            GameEvents.ReturnedToMenu += Show;
            // Root stays active (just not on top) while Shop/Missions/Profile/Settings open as
            // overlays on top of it - a coin change there (e.g. claiming a mission reward) previously
            // never reached this already-visible top bar, since nothing but Show() ever touched
            // coinsText. That's what made CLAIM look like it wasn't awarding anything: the balance
            // WAS updated in SaveData, just never redrawn until the next full Show().
            GameEvents.CoinsChanged += HandleCoinsChanged;
        }

        private void OnDestroy()
        {
            GameEvents.ReturnedToMenu -= Show;
            GameEvents.CoinsChanged -= HandleCoinsChanged;
        }

        private void HandleCoinsChanged(int newTotal)
        {
            if (coinsText != null) coinsText.text = newTotal.ToString();
        }

        private void Start()
        {
            Show();
        }

        public void Show()
        {
            if (transition != null) transition.Show();
            else root.SetActive(true);
            RefreshStats();
            RefreshModeSelection();
            AdsManager.Instance.ShowBanner();
            if (gameplayGroundRoot != null) gameplayGroundRoot.SetActive(false);
        }

        public void Hide()
        {
            root.SetActive(false);
            AdsManager.Instance.HideBanner();
            if (gameplayGroundRoot != null) gameplayGroundRoot.SetActive(true);
        }

        public void OnEasyClicked() => SelectMode(RunModeType.Easy);
        public void OnFrenzyClicked() => SelectMode(RunModeType.Frenzy);
        public void OnProClicked() => SelectMode(RunModeType.Pro);

        private void SelectMode(RunModeType mode)
        {
            _selectedMode = mode;
            RefreshModeSelection();
        }

        private void RefreshModeSelection()
        {
            easyHighlight.SetActive(_selectedMode == RunModeType.Easy);
            frenzyHighlight.SetActive(_selectedMode == RunModeType.Frenzy);
            proHighlight.SetActive(_selectedMode == RunModeType.Pro);

            Tint(easyButtonImage, easyButtonLabel, _selectedMode == RunModeType.Easy);
            Tint(frenzyButtonImage, frenzyButtonLabel, _selectedMode == RunModeType.Frenzy);
            Tint(proButtonImage, proButtonLabel, _selectedMode == RunModeType.Pro);

            RunModeConfig config = RunModeConfig.For(_selectedMode);
            modeTaglineText.text = config.Tagline;
            modeRulesText.text = config.RulesLine;

            RefreshBestForSelectedMode();
        }

        /// <summary>Each mode persists its own best distance (SaveData.BestDistanceFor) - this shows THAT mode's record, not the overall best, so switching modes must update it immediately.</summary>
        private void RefreshBestForSelectedMode()
        {
            float best = SaveSystem.Data.BestDistanceFor(_selectedMode);
            personalBestText.text = $"{Mathf.FloorToInt(best)} m";
        }

        private void Tint(Image buttonImage, Text label, bool selected)
        {
            if (buttonImage != null) buttonImage.color = selected ? modeSelectedColor : modeUnselectedColor;
            if (label != null) label.color = selected ? modeSelectedTextColor : modeUnselectedTextColor;
        }

        public void OnLetsRunClicked()
        {
            Hide();
            GameManager.Instance.StartRun(_selectedMode);
        }

        public void OnDailyRunClicked()
        {
            Hide();
            GameManager.Instance.StartRun(RunModeType.Daily);
        }

        public void OnClaimDailyBonusClicked()
        {
            if (DailyStreakManager.Instance.TryClaimDailyBonus())
            {
                RefreshStats();
            }
        }

        public void OnClaimGiftClicked()
        {
            if (DailyStreakManager.Instance.TryClaimGift())
            {
                RefreshStats();
            }
        }

        public void OnWatchAdClicked()
        {
            if (!AdsManager.Instance.IsRewardedAvailable(RewardedAdPlacement.BonusCoins)) return;
            watchAdButton.interactable = false;
            AdsManager.Instance.ShowRewarded(RewardedAdPlacement.BonusCoins,
                onRewardEarned: () =>
                {
                    CurrencyManager.Instance.AddCoins(25);
                    RefreshStats();
                    watchAdButton.interactable = true;
                },
                onFailedOrSkipped: () => watchAdButton.interactable = true);
        }

        private void RefreshStats()
        {
            var data = SaveSystem.Data;
            coinsText.text = data.coins.ToString();
            int streak = DailyStreakManager.Instance.DayStreak;
            dayStreakText.text = $"{streak} day{(streak == 1 ? "" : "s")}";

            claimDailyBonusButton.interactable = DailyStreakManager.Instance.CanClaimDailyBonus;
            claimGiftButton.interactable = DailyStreakManager.Instance.CanClaimGift;
        }

        public void OpenShop() => shopPanelRoot.SetActive(true);
        public void OpenMissions() => missionsPanelRoot.SetActive(true);
        public void OpenProfile() => profilePanelRoot.SetActive(true);
        public void OpenSettings() => settingsPanelRoot.SetActive(true);
    }
}

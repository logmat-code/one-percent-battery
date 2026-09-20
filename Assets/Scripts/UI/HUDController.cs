using OnePercentBattery.Core;
using OnePercentBattery.Data;
using OnePercentBattery.Player;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>Top bar during a run: charge %, distance, coins, shield badge, mode label, pause button.</summary>
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text batteryPercentText;
        [SerializeField] private Image batteryFillImage;
        [SerializeField] private Text modeLabelText;
        [SerializeField] private Text distanceText;
        [SerializeField] private Text coinsText;
        [SerializeField] private GameObject shieldBadge;
        [SerializeField] private GameObject magnetBadge;
        [SerializeField] private Text zoneLabelText;
        [SerializeField] private GameObject survivalModeBadge;
        [SerializeField] private Text survivalModeText;
        [SerializeField] private PlayerBattery playerBattery;

        [SerializeField] private Color healthyColor = new Color(0.68f, 0.92f, 0.36f);
        [SerializeField] private Color lowColor = new Color(0.95f, 0.65f, 0.2f);
        [SerializeField] private Color criticalColor = new Color(0.992f, 0.643f, 0.686f);

        private void Awake()
        {
            // Subscriptions live here rather than OnEnable: this controller's own GameObject
            // starts inactive (it IS the panel being shown/hidden), and OnEnable never fires
            // for a component on a GameObject that starts disabled - Awake does.
            GameEvents.RunStarted += HandleRunStarted;
            GameEvents.BatteryChanged += HandleBatteryChanged;
            GameEvents.DistanceChanged += HandleDistanceChanged;
            GameEvents.CoinCollected += HandleCoinCollected;
            GameEvents.ShieldTimeChanged += HandleShieldChanged;
            GameEvents.MagnetTimeChanged += HandleMagnetChanged;
            GameEvents.ZoneChanged += HandleZoneChanged;
            GameEvents.RunAwaitingContinue += HandleRunAwaitingContinue;
            GameEvents.ReturnedToMenu += HandleReturnedToMenu;
            GameEvents.SurvivalModeEntered += HandleSurvivalModeEntered;
        }

        private void Update()
        {
            if (survivalModeBadge != null && survivalModeBadge.activeSelf && playerBattery != null)
            {
                survivalModeText.text = $"SURVIVE! {playerBattery.SurvivalModeSeconds:0.0}s";
            }
        }

        private void OnDestroy()
        {
            GameEvents.RunStarted -= HandleRunStarted;
            GameEvents.BatteryChanged -= HandleBatteryChanged;
            GameEvents.DistanceChanged -= HandleDistanceChanged;
            GameEvents.CoinCollected -= HandleCoinCollected;
            GameEvents.ShieldTimeChanged -= HandleShieldChanged;
            GameEvents.MagnetTimeChanged -= HandleMagnetChanged;
            GameEvents.ZoneChanged -= HandleZoneChanged;
            GameEvents.RunAwaitingContinue -= HandleRunAwaitingContinue;
            GameEvents.ReturnedToMenu -= HandleReturnedToMenu;
            GameEvents.SurvivalModeEntered -= HandleSurvivalModeEntered;
        }

        private void HandleRunStarted(RunModeType mode)
        {
            root.SetActive(true);
            modeLabelText.text = RunModeConfig.For(mode).DisplayName;
            coinsText.text = "0";
            shieldBadge.SetActive(false);
            if (magnetBadge != null) magnetBadge.SetActive(false);
            if (survivalModeBadge != null) survivalModeBadge.SetActive(false);
        }

        private void HandleSurvivalModeEntered()
        {
            if (survivalModeBadge != null) survivalModeBadge.SetActive(true);
        }

        private void HandleRunAwaitingContinue(RunResult preview, bool canOfferSecondChance)
        {
            root.SetActive(false);
        }

        private void HandleReturnedToMenu()
        {
            root.SetActive(false);
        }

        private void HandleBatteryChanged(float currentPercent, float delta)
        {
            batteryPercentText.text = $"{Mathf.CeilToInt(currentPercent)}%";
            batteryFillImage.fillAmount = currentPercent / 100f;
            Color tierColor = currentPercent <= 15f ? criticalColor : currentPercent <= 40f ? lowColor : healthyColor;
            batteryFillImage.color = tierColor;
            batteryPercentText.color = tierColor;
        }

        private void HandleDistanceChanged(float meters)
        {
            distanceText.text = $"{Mathf.FloorToInt(meters)} m";
        }

        private void HandleCoinCollected(int runTotal)
        {
            coinsText.text = runTotal.ToString();
        }

        private void HandleShieldChanged(float secondsRemaining)
        {
            shieldBadge.SetActive(secondsRemaining > 0f);
        }

        private void HandleMagnetChanged(float secondsRemaining)
        {
            if (magnetBadge != null) magnetBadge.SetActive(secondsRemaining > 0f);
        }

        private void HandleZoneChanged(ZoneDefinition zone)
        {
            if (zoneLabelText != null)
            {
                zoneLabelText.text = zone.Name;
            }
        }
    }
}

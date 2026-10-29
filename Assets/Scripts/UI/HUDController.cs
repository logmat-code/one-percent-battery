using OnePercentBattery.Core;
using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using OnePercentBattery.Player;
using OnePercentBattery.Save;
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

        [Header("Charger pulse (brief, cosmetic only - see GameEvents.EnergyCollected)")]
        [SerializeField] private float chargerPulseScale = 1.15f;
        [SerializeField] private float chargerPulseSeconds = 0.25f;

        [Header("High score feedback")]
        [SerializeField] private GameObject newHighScoreBanner;
        [SerializeField] private Text newHighScoreText;
        [SerializeField] private float newHighScoreDisplaySeconds = 1.8f;

        private Vector3 _batteryTextBaseScale = Vector3.one;
        private float _chargerPulseTimer;
        private float _previousBestAtRunStart;
        private bool _hasShownNewHighScore;
        private Coroutine _highScoreBannerRoutine;

        private void Awake()
        {
            if (batteryPercentText != null) _batteryTextBaseScale = batteryPercentText.transform.localScale;
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
            GameEvents.SecondChanceGranted += HandleSecondChanceGranted;
            GameEvents.ReturnedToMenu += HandleReturnedToMenu;
            GameEvents.SurvivalModeEntered += HandleSurvivalModeEntered;
            GameEvents.EnergyCollected += HandleEnergyCollected;
        }

        private void Update()
        {
            if (survivalModeBadge != null && survivalModeBadge.activeSelf && playerBattery != null)
            {
                survivalModeText.text = $"SURVIVE! {playerBattery.SurvivalModeSeconds:0.0}s";
            }

            UpdateChargerPulse();
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
            GameEvents.SecondChanceGranted -= HandleSecondChanceGranted;
            GameEvents.ReturnedToMenu -= HandleReturnedToMenu;
            GameEvents.SurvivalModeEntered -= HandleSurvivalModeEntered;
            GameEvents.EnergyCollected -= HandleEnergyCollected;
        }

        private void HandleEnergyCollected(float amount) => _chargerPulseTimer = chargerPulseSeconds;

        private void UpdateChargerPulse()
        {
            if (batteryPercentText == null) return;

            if (_chargerPulseTimer > 0f)
            {
                _chargerPulseTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(_chargerPulseTimer / chargerPulseSeconds);
                batteryPercentText.transform.localScale = Vector3.Lerp(_batteryTextBaseScale, _batteryTextBaseScale * chargerPulseScale, t);
            }
            else if (batteryPercentText.transform.localScale != _batteryTextBaseScale)
            {
                batteryPercentText.transform.localScale = _batteryTextBaseScale;
            }
        }

        private void HandleRunStarted(RunModeType mode)
        {
            root.SetActive(true);
            _chargerPulseTimer = 0f;
            _previousBestAtRunStart = SaveSystem.Data.BestDistanceFor(mode);
            _hasShownNewHighScore = false;
            if (_highScoreBannerRoutine != null)
            {
                StopCoroutine(_highScoreBannerRoutine);
                _highScoreBannerRoutine = null;
            }
            if (newHighScoreBanner != null) newHighScoreBanner.SetActive(false);

            if (batteryPercentText != null) batteryPercentText.transform.localScale = _batteryTextBaseScale;
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
            if (_highScoreBannerRoutine != null)
            {
                StopCoroutine(_highScoreBannerRoutine);
                _highScoreBannerRoutine = null;
            }
            if (newHighScoreBanner != null) newHighScoreBanner.SetActive(false);
            root.SetActive(false);
        }

        private void HandleSecondChanceGranted()
        {
            if (root != null) root.SetActive(true);

            if (_highScoreBannerRoutine != null)
            {
                StopCoroutine(_highScoreBannerRoutine);
                _highScoreBannerRoutine = null;
            }
            if (newHighScoreBanner != null) newHighScoreBanner.SetActive(false);

            if (playerBattery != null)
            {
                HandleBatteryChanged(playerBattery.CurrentPercent, 0f);
                if (shieldBadge != null) shieldBadge.SetActive(playerBattery.IsInvulnerable);
            }

            if (coinsText != null && CurrencyManager.Instance != null)
            {
                coinsText.text = CurrencyManager.Instance.RunCoins.ToString();
            }

            if (magnetBadge != null)
            {
                magnetBadge.SetActive(false);
            }
        }

        private void HandleReturnedToMenu()
        {
            if (_highScoreBannerRoutine != null)
            {
                StopCoroutine(_highScoreBannerRoutine);
                _highScoreBannerRoutine = null;
            }
            if (newHighScoreBanner != null) newHighScoreBanner.SetActive(false);
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

            if (!_hasShownNewHighScore && _previousBestAtRunStart > 0f && meters > _previousBestAtRunStart)
            {
                _hasShownNewHighScore = true;
                ShowNewHighScoreBanner();
            }
        }

        private void ShowNewHighScoreBanner()
        {
            if (newHighScoreBanner == null)
            {
                CreateFallbackHighScoreBanner();
            }

            if (newHighScoreBanner == null) return;

            if (_highScoreBannerRoutine != null) StopCoroutine(_highScoreBannerRoutine);
            _highScoreBannerRoutine = StartCoroutine(AnimateHighScoreBanner());
        }

        private System.Collections.IEnumerator AnimateHighScoreBanner()
        {
            newHighScoreBanner.SetActive(true);
            var cg = newHighScoreBanner.GetComponent<CanvasGroup>();
            if (cg == null) cg = newHighScoreBanner.AddComponent<CanvasGroup>();

            Vector3 baseScale = Vector3.one;
            float elapsed = 0f;
            float fadeInDuration = 0.25f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                cg.alpha = t;
                newHighScoreBanner.transform.localScale = Vector3.Lerp(baseScale * 0.85f, baseScale * 1.05f, t);
                yield return null;
            }

            newHighScoreBanner.transform.localScale = baseScale;
            cg.alpha = 1f;

            yield return new WaitForSeconds(newHighScoreDisplaySeconds);

            elapsed = 0f;
            float fadeOutDuration = 0.35f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                cg.alpha = 1f - t;
                yield return null;
            }

            cg.alpha = 0f;
            newHighScoreBanner.SetActive(false);
            _highScoreBannerRoutine = null;
        }

        private void CreateFallbackHighScoreBanner()
        {
            if (root == null) return;

            var go = new GameObject("NewHighScoreBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(root.transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -220f);
            rt.sizeDelta = new Vector2(460f, 60f);

            var img = go.GetComponent<Image>();
            img.color = new Color(0.96f, 0.78f, 0.25f, 0.95f);
            img.raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            textGo.transform.SetParent(go.transform, false);

            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.anchoredPosition = Vector2.zero;

            var txt = textGo.GetComponent<Text>();
            txt.font = distanceText != null ? distanceText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = "NEW HIGH SCORE!";
            txt.fontSize = 28;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.12f, 0.09f, 0.02f, 1f);
            txt.raycastTarget = false;

            var outline = textGo.GetComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.5f);
            outline.effectDistance = new Vector2(1f, -1f);

            go.SetActive(false);
            newHighScoreBanner = go;
            newHighScoreText = txt;
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

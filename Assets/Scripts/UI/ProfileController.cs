using System.Collections;
using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using OnePercentBattery.Save;
using OnePercentBattery.Services;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>Profile screen: aggregate stats (grouped into Main/Mode records/Progress - see Refresh),
    /// recent runs log, and the achievements grid. Every stat shown is backed by a real SaveData field -
    /// there is no lifetime "missions completed" counter anywhere in SaveData (MissionPeriodState only
    /// keeps the CURRENT rotation's entries, replaced wholesale on each rotation), so that metric is
    /// deliberately not shown here rather than faked from a misleading proxy.</summary>
    public sealed class ProfileController : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        [Header("Main stats")]
        [SerializeField] private Text bestText;
        [SerializeField] private Text totalDistanceText;
        [SerializeField] private Text runsText;
        [SerializeField] private Text coinsText;

        [Header("Mode records")]
        [SerializeField] private Text easyBestText;
        [SerializeField] private Text frenzyBestText;
        [SerializeField] private Text proBestText;

        [Header("Progress")]
        [SerializeField] private Text skinsOwnedText;
        [SerializeField] private Text trailsOwnedText;

        [Header("Recent runs")]
        [SerializeField] private Text recentRunsHeaderText;
        [SerializeField] private Transform recentRunsContentParent;
        [SerializeField] private Text runLogLinePrefab;
        [SerializeField] private int maxRecentRunsShown = 10;

        [Header("Achievements")]
        [SerializeField] private Text achievementsHeaderText;
        [SerializeField] private Transform achievementsContentParent;
        [SerializeField] private AchievementItemView achievementItemPrefab;

        [Header("Sharing")]
        [SerializeField] private Button shareStatsButton;
        [SerializeField] private Text shareStatsLabel;
        private Coroutine _shareFeedbackRoutine;

        private void Awake()
        {
            EnsureShareButton();
        }

        private void OnEnable()
        {
            EnsureShareButton();
            AchievementManager.Instance.Unlocked += HandleAchievementUnlocked;
            Refresh();
        }

        private void OnDisable()
        {
            AchievementManager.Instance.Unlocked -= HandleAchievementUnlocked;
        }

        private void HandleAchievementUnlocked(AchievementDefinition definition) => Refresh();

        private void Refresh()
        {
            var data = SaveSystem.Data;
            bestText.text = $"{Mathf.FloorToInt(data.OverallBestDistance())}m";
            totalDistanceText.text = $"{Mathf.FloorToInt(data.lifetimeDistanceMeters)}m";
            runsText.text = data.totalRuns.ToString();
            coinsText.text = data.coins.ToString();

            easyBestText.text = $"{Mathf.FloorToInt(data.bestDistanceEasy)}m";
            frenzyBestText.text = $"{Mathf.FloorToInt(data.bestDistanceFrenzy)}m";
            proBestText.text = $"{Mathf.FloorToInt(data.bestDistancePro)}m";

            skinsOwnedText.text = $"{data.unlockedSkinIds.Count}/{ShopCatalog.Skins.Length}";
            trailsOwnedText.text = $"{data.unlockedTrailIds.Count}/{ShopCatalog.Trails.Length}";

            recentRunsHeaderText.text = $"{Mathf.Min(data.recentRuns.Count, 50)}/50";
            for (int i = recentRunsContentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(recentRunsContentParent.GetChild(i).gameObject);
            }
            int shown = Mathf.Min(data.recentRuns.Count, maxRecentRunsShown);
            for (int i = 0; i < shown; i++)
            {
                var entry = data.recentRuns[i];
                Text line = Instantiate(runLogLinePrefab, recentRunsContentParent);
                line.text = $"{Mathf.FloorToInt(entry.distanceMeters)}m · {entry.mode} · +{entry.coinsEarned} · {entry.zoneReached} · {entry.dateIso}";
            }

            int unlockedCount = 0;
            foreach (var a in AchievementCatalog.All)
            {
                if (AchievementManager.Instance.IsUnlocked(a.Id)) unlockedCount++;
            }
            achievementsHeaderText.text = $"{unlockedCount}/{AchievementCatalog.All.Length} UNLOCKED";

            for (int i = achievementsContentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(achievementsContentParent.GetChild(i).gameObject);
            }
            foreach (var a in AchievementCatalog.All)
            {
                AchievementItemView view = Instantiate(achievementItemPrefab, achievementsContentParent);
                view.Bind(a);
            }
        }

        public void Close() => root.SetActive(false);

        public void OnShareStatsClicked()
        {
            NativeShareService.ShareHomeStats((ok, msg) =>
            {
                if (shareStatsLabel != null && gameObject.activeInHierarchy)
                {
                    if (_shareFeedbackRoutine != null) StopCoroutine(_shareFeedbackRoutine);
                    _shareFeedbackRoutine = StartCoroutine(ShowShareFeedbackRoutine(ok ? "SAVED / SHARED!" : "RETRY SHARE"));
                }
            });
        }

        private IEnumerator ShowShareFeedbackRoutine(string feedback)
        {
            if (shareStatsLabel == null) yield break;
            shareStatsLabel.text = feedback;
            yield return new WaitForSecondsRealtime(2f);
            if (shareStatsLabel != null) shareStatsLabel.text = "SHARE STATS";
        }

        private void EnsureShareButton()
        {
            if (shareStatsButton != null)
            {
                shareStatsButton.onClick.RemoveListener(OnShareStatsClicked);
                shareStatsButton.onClick.AddListener(OnShareStatsClicked);
                return;
            }
            if (root == null) return;

            Transform existing = root.transform.Find("ShareStatsButton");
            if (existing != null)
            {
                shareStatsButton = existing.GetComponent<Button>();
                shareStatsLabel = existing.GetComponentInChildren<Text>();
                if (shareStatsButton != null)
                {
                    shareStatsButton.onClick.RemoveListener(OnShareStatsClicked);
                    shareStatsButton.onClick.AddListener(OnShareStatsClicked);
                }
                return;
            }

            var btnGo = new GameObject("ShareStatsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            btnGo.transform.SetParent(root.transform, false);

            var rt = (RectTransform)btnGo.transform;
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-136f, -60f);
            rt.sizeDelta = new Vector2(220f, 68f);

            var img = btnGo.GetComponent<Image>();
            img.color = new Color(0.14f, 0.24f, 0.16f, 1f);

            var outline = btnGo.GetComponent<Outline>();
            outline.effectColor = new Color(0.68f, 0.92f, 0.36f, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(8f, 4f);
            labelRt.offsetMax = new Vector2(-8f, -4f);

            var txt = labelGo.GetComponent<Text>();
            txt.font = bestText != null ? bestText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = "SHARE STATS";
            txt.fontSize = 22;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.835f, 1f, 0.643f, 1f);
            txt.raycastTarget = false;

            shareStatsButton = btnGo.GetComponent<Button>();
            shareStatsLabel = txt;
            shareStatsButton.onClick.AddListener(OnShareStatsClicked);
        }
    }
}

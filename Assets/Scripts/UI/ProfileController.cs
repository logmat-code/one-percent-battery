using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using OnePercentBattery.Save;
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

        private void OnEnable()
        {
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
    }
}

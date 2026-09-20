using System;
using OnePercentBattery.Core;
using OnePercentBattery.Data;
using OnePercentBattery.Save;
using OnePercentBattery.Utils;
using UnityEngine;

namespace OnePercentBattery.Meta
{
    /// <summary>The 8 Profile-screen achievements. One-time unlocks (no daily/monthly reset, unlike missions).</summary>
    public sealed class AchievementManager : Singleton<AchievementManager>
    {
        public event Action<AchievementDefinition> Unlocked;

        private void OnEnable()
        {
            GameEvents.RunEnded += HandleRunEnded;
            GameEvents.ZoneChanged += HandleZoneChanged;
        }

        private void OnDisable()
        {
            GameEvents.RunEnded -= HandleRunEnded;
            GameEvents.ZoneChanged -= HandleZoneChanged;
        }

        public bool IsUnlocked(string id) => SaveSystem.Data.unlockedAchievementIds.Contains(id);

        private void HandleRunEnded(RunResult result)
        {
            TryUnlockWhere(a => a.Metric == AchievementMetric.RunsCompleted && SaveSystem.Data.totalRuns >= a.Target);
            TryUnlockWhere(a => a.Metric == AchievementMetric.SurviveCriticalSecondsInOneRun && result.SurvivalModeSeconds >= a.Target);
            TryUnlockWhere(a => a.Metric == AchievementMetric.SurviveSecondsInOneRun && result.ElapsedSeconds >= a.Target);
            TryUnlockWhere(a => a.Metric == AchievementMetric.DistanceInOneRunMeters && result.DistanceMeters >= a.Target);
            TryUnlockWhere(a => a.Metric == AchievementMetric.LifetimeDistanceMeters && SaveSystem.Data.lifetimeDistanceMeters >= a.Target);
            TryUnlockWhere(a => a.Metric == AchievementMetric.CompletedProRunAtOrAboveMeters
                && result.Mode == RunModeType.Pro && result.DistanceMeters >= a.Target);
        }

        private void HandleZoneChanged(ZoneDefinition zone)
        {
            TryUnlockWhere(a => a.Metric == AchievementMetric.ReachZone && a.TargetZoneName == zone.Name);
        }

        private void TryUnlockWhere(Func<AchievementDefinition, bool> predicate)
        {
            foreach (var achievement in AchievementCatalog.All)
            {
                if (IsUnlocked(achievement.Id)) continue;
                if (!predicate(achievement)) continue;

                SaveSystem.Data.unlockedAchievementIds.Add(achievement.Id);
                CurrencyManager.Instance.AddCoins(achievement.RewardCoins);
                SaveSystem.Save();
                Unlocked?.Invoke(achievement);
            }
        }
    }
}

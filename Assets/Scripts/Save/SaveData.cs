using System;
using System.Collections.Generic;

namespace OnePercentBattery.Save
{
    [Serializable]
    public sealed class MissionProgressEntry
    {
        public string id;
        public float progress;
        public bool claimed;
    }

    [Serializable]
    public sealed class MissionPeriodState
    {
        /// <summary>UTC instant (DateTime.O round-trip format) this rotation was generated. Empty/unparseable (fresh or pre-rotation save) is treated as "generate now". Elapsed time is measured from this, NOT a calendar-day/month boundary.</summary>
        public string rotationTimestampUtc = "";
        /// <summary>The 5 mission ids randomly selected from MissionCatalog.DailyPool/MonthlyPool for the current rotation - resolved to full MissionDefinitions by MissionManager, never persisted as full objects.</summary>
        public List<string> activeMissionIds = new List<string>();
        public List<MissionProgressEntry> entries = new List<MissionProgressEntry>();
    }

    [Serializable]
    public sealed class RunLogEntry
    {
        public string mode;
        public float distanceMeters;
        public int coinsEarned;
        public float survivalModeSeconds;
        public string zoneReached;
        public string endCause;
        public string dateIso;
    }

    /// <summary>
    /// Everything the source game says it stores locally on-device (see Settings > Privacy:
    /// "Your scores, coins and settings are saved on this device"). One JSON blob in PlayerPrefs.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int coins;
        public int fragments;

        public float bestDistanceEasy;
        public float bestDistanceFrenzy;
        public float bestDistancePro;
        public float bestDistanceDaily;
        public float bestDistanceDailyAllTime;

        public float lifetimeDistanceMeters;
        public int totalRuns;
        public float bestSurvivalModeSeconds;

        public int dayStreak;
        public string lastPlayedDateIso = "";
        public string lastDailyBonusDateIso = "";
        public string lastGiftDateIso = "";
        public string lastDailyRunDateIso = "";

        public List<string> unlockedSkinIds = new List<string> { "skin_default" };
        public List<string> unlockedTrailIds = new List<string> { "trail_none" };
        public string equippedSkinId = "skin_default";
        public string equippedTrailId = "trail_none";

        public List<string> unlockedAchievementIds = new List<string>();

        public MissionPeriodState dailyMissions = new MissionPeriodState();
        public MissionPeriodState monthlyMissions = new MissionPeriodState();

        public List<RunLogEntry> recentRuns = new List<RunLogEntry>();

        public bool soundEffectsEnabled = true;
        public bool hapticsEnabled = true;
        public bool practiceMode;
        public bool hasCompletedTutorial;

        /// <summary>Future "Remove Ads" purchase flag. No purchase flow exists yet - AdsManager already respects it.</summary>
        public bool adsRemoved;

        public float BestDistanceFor(Core.RunModeType mode)
        {
            switch (mode)
            {
                case Core.RunModeType.Easy: return bestDistanceEasy;
                case Core.RunModeType.Frenzy: return bestDistanceFrenzy;
                case Core.RunModeType.Pro: return bestDistancePro;
                case Core.RunModeType.Daily: return bestDistanceDaily;
                default: return 0f;
            }
        }

        public void SetBestDistanceFor(Core.RunModeType mode, float value)
        {
            switch (mode)
            {
                case Core.RunModeType.Easy: bestDistanceEasy = value; break;
                case Core.RunModeType.Frenzy: bestDistanceFrenzy = value; break;
                case Core.RunModeType.Pro: bestDistancePro = value; break;
                case Core.RunModeType.Daily:
                    bestDistanceDaily = value;
                    if (value > bestDistanceDailyAllTime) bestDistanceDailyAllTime = value;
                    break;
            }
        }

        public float OverallBestDistance()
        {
            return UnityEngine.Mathf.Max(
                bestDistanceEasy,
                UnityEngine.Mathf.Max(
                    bestDistanceFrenzy,
                    UnityEngine.Mathf.Max(bestDistancePro, UnityEngine.Mathf.Max(bestDistanceDaily, bestDistanceDailyAllTime))));
        }
    }
}

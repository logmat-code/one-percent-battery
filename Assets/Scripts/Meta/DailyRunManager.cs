using System;
using OnePercentBattery.Save;
using OnePercentBattery.Utils;

namespace OnePercentBattery.Meta
{
    /// <summary>Seeds the Daily Run so every player who plays it today gets the same obstacle/pickup layout.</summary>
    public sealed class DailyRunManager : Singleton<DailyRunManager>
    {
        private static string TodayIso => DailyResetService.TodayUtcDayKey;

        public System.Random CreateTodaysRng()
        {
            EnsureCurrentDay();
            return new System.Random(TodayIso.GetHashCode());
        }

        public float BestDistanceToday => SaveSystem.Data.lastDailyRunDateIso == TodayIso ? SaveSystem.Data.bestDistanceDaily : 0f;

        /// <summary>Ensures today's Daily Run record is reset on UTC day rollover before a run starts, while preserving lifetime best in bestDistanceDailyAllTime.</summary>
        public void EnsureCurrentDay()
        {
            var data = SaveSystem.Data;
            if (data.bestDistanceDaily > data.bestDistanceDailyAllTime)
            {
                data.bestDistanceDailyAllTime = data.bestDistanceDaily;
            }
            if (data.lastDailyRunDateIso != TodayIso)
            {
                data.lastDailyRunDateIso = TodayIso;
                data.bestDistanceDaily = 0f;
                SaveSystem.Save();
            }
        }

        public void ReportRun(float distanceMeters)
        {
            EnsureCurrentDay();
            var data = SaveSystem.Data;
            if (distanceMeters > data.bestDistanceDaily)
            {
                data.bestDistanceDaily = distanceMeters;
            }
            if (distanceMeters > data.bestDistanceDailyAllTime)
            {
                data.bestDistanceDailyAllTime = distanceMeters;
            }
            SaveSystem.Save();
        }
    }
}

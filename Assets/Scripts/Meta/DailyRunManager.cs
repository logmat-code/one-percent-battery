using System;
using OnePercentBattery.Save;
using OnePercentBattery.Utils;

namespace OnePercentBattery.Meta
{
    /// <summary>Seeds the Daily Run so every player who plays it today gets the same obstacle/pickup layout.</summary>
    public sealed class DailyRunManager : Singleton<DailyRunManager>
    {
        private static string TodayIso => DateTime.Now.ToString("yyyy-MM-dd");

        public System.Random CreateTodaysRng()
        {
            return new System.Random(TodayIso.GetHashCode());
        }

        public float BestDistanceToday => SaveSystem.Data.lastDailyRunDateIso == TodayIso ? SaveSystem.Data.bestDistanceDaily : 0f;

        public void ReportRun(float distanceMeters)
        {
            var data = SaveSystem.Data;
            if (data.lastDailyRunDateIso != TodayIso)
            {
                data.lastDailyRunDateIso = TodayIso;
                data.bestDistanceDaily = 0f;
            }

            if (distanceMeters > data.bestDistanceDaily)
            {
                data.bestDistanceDaily = distanceMeters;
            }
            SaveSystem.Save();
        }
    }
}

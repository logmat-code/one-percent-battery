using System;
using System.Globalization;

namespace OnePercentBattery.Meta
{
    /// <summary>
    /// Centralized, robust UTC calendar-day service for Daily Bonus, Daily Gift, and Daily Run.
    /// Uses InvariantCulture ISO strings (yyyy-MM-dd) to ensure culture and calendar independence,
    /// and gracefully handles device clock manipulation, timezones, and app restarts.
    /// </summary>
    public static class DailyResetService
    {
        public const string DateFormat = "yyyy-MM-dd";

        /// <summary>Current UTC day key formatted as yyyy-MM-dd (e.g. "2026-10-20").</summary>
        public static string TodayUtcDayKey => DateTime.UtcNow.ToString(DateFormat, CultureInfo.InvariantCulture);

        /// <summary>
        /// Checks whether a daily reward is available today based on the last claimed ISO date.
        /// Returns true if never claimed, or if the current UTC day is strictly after the last claim date.
        /// Returns false if already claimed today, or if the clock has been moved backward past the last claim date.
        /// </summary>
        public static bool IsClaimAvailable(string lastClaimDateIso)
        {
            if (string.IsNullOrEmpty(lastClaimDateIso)) return true;

            string today = TodayUtcDayKey;
            // If already claimed today, or if today is earlier than last recorded claim (clock rolled backward)
            if (string.CompareOrdinal(today, lastClaimDateIso) <= 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Calculates consecutive day streak based on last played date.
        /// </summary>
        public static int CalculateStreak(int currentStreak, string lastPlayedDateIso)
        {
            string today = TodayUtcDayKey;
            if (string.IsNullOrEmpty(lastPlayedDateIso))
            {
                return 1;
            }

            if (lastPlayedDateIso == today)
            {
                return Math.Max(1, currentStreak);
            }

            if (DateTime.TryParseExact(lastPlayedDateIso, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastDate) &&
                DateTime.TryParseExact(today, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime nowDate))
            {
                double daysDiff = (nowDate - lastDate).TotalDays;
                if (daysDiff >= 1.0 && daysDiff < 2.0)
                {
                    // Exactly yesterday
                    return Math.Max(1, currentStreak) + 1;
                }
                else if (daysDiff < 0)
                {
                    // Clock rolled backward
                    return Math.Max(1, currentStreak);
                }
                else
                {
                    // Missed more than 1 day
                    return 1;
                }
            }

            return 1;
        }

        /// <summary>Time remaining until next UTC midnight reset.</summary>
        public static TimeSpan TimeUntilNextResetUtc
        {
            get
            {
                DateTime now = DateTime.UtcNow;
                DateTime tomorrow = now.Date.AddDays(1);
                TimeSpan diff = tomorrow - now;
                return diff < TimeSpan.Zero ? TimeSpan.Zero : diff;
            }
        }
    }
}

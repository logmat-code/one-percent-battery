using System;
using OnePercentBattery.Save;
using OnePercentBattery.Utils;

namespace OnePercentBattery.Meta
{
    /// <summary>Day streak + the two daily claims shown on the home screen (Daily Bonus, and a Gift of coins + a fragment).</summary>
    public sealed class DailyStreakManager : Singleton<DailyStreakManager>
    {
        public const int DailyBonusCoins = 50;
        public const int GiftCoins = 30;
        public const int GiftFragments = 1;

        public int DayStreak => SaveSystem.Data.dayStreak;
        public bool CanClaimDailyBonus => SaveSystem.Data.lastDailyBonusDateIso != TodayIso;
        public bool CanClaimGift => SaveSystem.Data.lastGiftDateIso != TodayIso;

        private static string TodayIso => DateTime.Now.ToString("yyyy-MM-dd");

        protected override void Awake()
        {
            base.Awake();
            RegisterVisitToday();
        }

        private void RegisterVisitToday()
        {
            var data = SaveSystem.Data;
            string today = TodayIso;
            if (data.lastPlayedDateIso == today)
            {
                return;
            }

            if (data.lastPlayedDateIso == DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"))
            {
                data.dayStreak += 1;
            }
            else
            {
                data.dayStreak = 1;
            }

            data.lastPlayedDateIso = today;
            SaveSystem.Save();
        }

        public bool TryClaimDailyBonus()
        {
            if (!CanClaimDailyBonus) return false;
            SaveSystem.Data.lastDailyBonusDateIso = TodayIso;
            CurrencyManager.Instance.AddCoins(DailyBonusCoins);
            return true;
        }

        public bool TryClaimGift()
        {
            if (!CanClaimGift) return false;
            SaveSystem.Data.lastGiftDateIso = TodayIso;
            CurrencyManager.Instance.AddCoins(GiftCoins);
            CurrencyManager.Instance.AddFragments(GiftFragments);
            return true;
        }
    }
}

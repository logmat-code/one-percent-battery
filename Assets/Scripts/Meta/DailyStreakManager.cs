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

        public event Action Claimed;

        public int DayStreak => SaveSystem.Data.dayStreak;
        public bool CanClaimDailyBonus => DailyResetService.IsClaimAvailable(SaveSystem.Data.lastDailyBonusDateIso);
        public bool CanClaimGift => DailyResetService.IsClaimAvailable(SaveSystem.Data.lastGiftDateIso);

        protected override void Awake()
        {
            base.Awake();
            RegisterVisitToday();
        }

        public void RegisterVisitToday()
        {
            var data = SaveSystem.Data;
            string today = DailyResetService.TodayUtcDayKey;
            if (data.lastPlayedDateIso == today)
            {
                return;
            }

            data.dayStreak = DailyResetService.CalculateStreak(data.dayStreak, data.lastPlayedDateIso);
            data.lastPlayedDateIso = today;
            SaveSystem.Save();
        }

        public bool TryClaimDailyBonus()
        {
            if (!CanClaimDailyBonus) return false;
            SaveSystem.Data.lastDailyBonusDateIso = DailyResetService.TodayUtcDayKey;
            CurrencyManager.Instance.AddCoins(DailyBonusCoins);
            SaveSystem.Save();
            Claimed?.Invoke();
            return true;
        }

        public bool TryClaimGift()
        {
            if (!CanClaimGift) return false;
            SaveSystem.Data.lastGiftDateIso = DailyResetService.TodayUtcDayKey;
            CurrencyManager.Instance.AddCoins(GiftCoins);
            CurrencyManager.Instance.AddFragments(GiftFragments);
            SaveSystem.Save();
            Claimed?.Invoke();
            return true;
        }
    }
}

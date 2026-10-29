using OnePercentBattery.Core;
using OnePercentBattery.Save;
using OnePercentBattery.Utils;

namespace OnePercentBattery.Meta
{
    /// <summary>Coins (spendable in the Shop) and fragments (gift currency). Coins-only economy - no IAP.</summary>
    public sealed class CurrencyManager : Singleton<CurrencyManager>
    {
        public int TotalCoins => SaveSystem.Data.coins;
        public int TotalFragments => SaveSystem.Data.fragments;
        public int RunCoins { get; private set; }

        public void BeginRun()
        {
            RunCoins = 0;
        }

        /// <summary>Coins picked up during the active run - counted immediately towards the wallet and towards mission progress.</summary>
        public void AddCoinsForRun(int amount)
        {
            if (amount <= 0) return;
            RunCoins += amount;
            AddCoins(amount);
            GameEvents.RaiseCoinCollected(RunCoins);
        }

        /// <summary>Doubles the coins earned during the active run by adding the current RunCoins to the wallet and doubling RunCoins.</summary>
        public int DoubleRunCoins()
        {
            if (RunCoins <= 0) return 0;
            int bonus = RunCoins;
            RunCoins += bonus;
            AddCoins(bonus);
            GameEvents.RaiseCoinCollected(RunCoins);
            return bonus;
        }

        public void AddCoins(int amount)
        {
            if (amount == 0) return;
            SaveSystem.Data.coins += amount;
            SaveSystem.Save();
            GameEvents.RaiseCoinsChanged(SaveSystem.Data.coins);
        }

        public void AddFragments(int amount)
        {
            if (amount == 0) return;
            SaveSystem.Data.fragments += amount;
            SaveSystem.Save();
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0 || SaveSystem.Data.coins < amount) return false;
            SaveSystem.Data.coins -= amount;
            SaveSystem.Save();
            GameEvents.RaiseCoinsChanged(SaveSystem.Data.coins);
            return true;
        }
    }
}

using System;
using OnePercentBattery.Data;
using OnePercentBattery.Save;
using OnePercentBattery.Utils;

namespace OnePercentBattery.Meta
{
    /// <summary>Owns which skins/trails are unlocked and equipped. Purchases are coins-only (see CurrencyManager).</summary>
    public sealed class ShopManager : Singleton<ShopManager>
    {
        public event Action CollectionChanged;

        public bool IsUnlocked(CosmeticDefinition item)
        {
            var data = SaveSystem.Data;
            return item.CostCoins == 0
                || (item.Category == CosmeticCategory.Trail ? data.unlockedTrailIds.Contains(item.Id) : data.unlockedSkinIds.Contains(item.Id));
        }

        public bool IsEquipped(CosmeticDefinition item)
        {
            var data = SaveSystem.Data;
            return item.Category == CosmeticCategory.Trail ? data.equippedTrailId == item.Id : data.equippedSkinId == item.Id;
        }

        public bool TryPurchase(CosmeticDefinition item)
        {
            if (IsUnlocked(item)) return true;
            if (!CurrencyManager.Instance.TrySpend(item.CostCoins)) return false;

            var data = SaveSystem.Data;
            if (item.Category == CosmeticCategory.Trail) data.unlockedTrailIds.Add(item.Id);
            else data.unlockedSkinIds.Add(item.Id);
            SaveSystem.Save();
            CollectionChanged?.Invoke();
            return true;
        }

        public void Equip(CosmeticDefinition item)
        {
            if (!IsUnlocked(item)) return;

            var data = SaveSystem.Data;
            if (item.Category == CosmeticCategory.Trail) data.equippedTrailId = item.Id;
            else data.equippedSkinId = item.Id;
            SaveSystem.Save();
            CollectionChanged?.Invoke();
        }

        /// <summary>Unconditional grant - used for mission/achievement/seasonal rewards.</summary>
        public void Unlock(CosmeticDefinition item)
        {
            if (IsUnlocked(item)) return;

            var data = SaveSystem.Data;
            if (item.Category == CosmeticCategory.Trail) data.unlockedTrailIds.Add(item.Id);
            else data.unlockedSkinIds.Add(item.Id);
            SaveSystem.Save();
            CollectionChanged?.Invoke();
        }

        public CosmeticDefinition EquippedSkin => ShopCatalog.FindSkin(SaveSystem.Data.equippedSkinId);
        public CosmeticDefinition EquippedTrail => ShopCatalog.FindTrail(SaveSystem.Data.equippedTrailId);
    }
}

using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>One tile in the Customize (Skins/Trails/Exclusive) grid.</summary>
    public sealed class ShopItemView : MonoBehaviour
    {
        [SerializeField] private Image swatch;
        [SerializeField] private Text nameText;
        [SerializeField] private Text costText;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject equippedCheckmark;
        [SerializeField] private Button button;

        private CosmeticDefinition _item;

        public void Bind(CosmeticDefinition item)
        {
            _item = item;
            swatch.color = item.AccentColor.a > 0f ? item.AccentColor : Color.gray;
            nameText.text = item.DisplayName;

            bool unlocked = ShopManager.Instance.IsUnlocked(item);
            bool equipped = ShopManager.Instance.IsEquipped(item);

            costText.gameObject.SetActive(!unlocked);
            costText.text = item.CostCoins.ToString();
            lockedOverlay.SetActive(!unlocked);
            equippedCheckmark.SetActive(unlocked && equipped);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClicked);
        }

        private void HandleClicked()
        {
            if (!ShopManager.Instance.IsUnlocked(_item))
            {
                if (!ShopManager.Instance.TryPurchase(_item)) return;
            }
            ShopManager.Instance.Equip(_item);
            Bind(_item);
        }
    }
}

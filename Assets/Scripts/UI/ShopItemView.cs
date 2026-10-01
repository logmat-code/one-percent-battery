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
        private SkinPreviewController _preview;

        public void Bind(CosmeticDefinition item, SkinPreviewController preview)
        {
            _item = item;
            _preview = preview;
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
            bool unlocked = ShopManager.Instance.IsUnlocked(_item);

            // Every locked item (Skins, Trails, and Exclusive - which are just Skins/Trails from a
            // curated list, see ShopCatalog.Exclusive) opens a preview instead of purchasing
            // immediately. The preview never touches SaveData itself; it only calls
            // ShopManager.TryPurchase/Equip once the player presses Buy there, so nothing changes
            // here unless/until that happens.
            if (!unlocked && _preview != null)
            {
                _preview.Show(_item);
                return;
            }

            if (!unlocked)
            {
                if (!ShopManager.Instance.TryPurchase(_item)) return;
            }
            ShopManager.Instance.Equip(_item);
            Bind(_item, _preview);
        }
    }
}

using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>The Customize modal: Skins / Trails / Exclusive tabs, each a grid of ShopItemView tiles.</summary>
    public sealed class ShopController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text coinsText;
        [SerializeField] private Transform contentParent;
        [SerializeField] private ShopItemView itemPrefab;

        [Header("Tabs")]
        [SerializeField] private Image tabSkinsImage;
        [SerializeField] private Text tabSkinsLabel;
        [SerializeField] private Image tabTrailsImage;
        [SerializeField] private Text tabTrailsLabel;
        [SerializeField] private Image tabExclusiveImage;
        [SerializeField] private Text tabExclusiveLabel;
        [SerializeField] private Color tabSelectedColor = new Color(0.70f, 0.93f, 0.38f);
        [SerializeField] private Color tabUnselectedColor = new Color(0.12f, 0.16f, 0.165f);
        [SerializeField] private Color tabSelectedTextColor = new Color(0.05f, 0.08f, 0.03f);
        [SerializeField] private Color tabUnselectedTextColor = new Color(0.95f, 0.96f, 0.95f);

        private CosmeticDefinition[] _currentList = ShopCatalog.Skins;
        private CosmeticCategory _currentCategory = CosmeticCategory.Skin;

        private void OnEnable()
        {
            ShopManager.Instance.CollectionChanged += Refresh;
            ShowSkins();
        }

        private void OnDisable()
        {
            ShopManager.Instance.CollectionChanged -= Refresh;
        }

        public void ShowSkins() { _currentList = ShopCatalog.Skins; _currentCategory = CosmeticCategory.Skin; Refresh(); }
        public void ShowTrails() { _currentList = ShopCatalog.Trails; _currentCategory = CosmeticCategory.Trail; Refresh(); }
        public void ShowExclusive() { _currentList = ShopCatalog.Exclusive; _currentCategory = CosmeticCategory.Exclusive; Refresh(); }

        private void Refresh()
        {
            coinsText.text = CurrencyManager.Instance.TotalCoins.ToString();
            RefreshTabHighlight();

            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(contentParent.GetChild(i).gameObject);
            }

            foreach (var item in _currentList)
            {
                ShopItemView view = Instantiate(itemPrefab, contentParent);
                view.Bind(item);
            }
        }

        private void RefreshTabHighlight()
        {
            Tint(tabSkinsImage, tabSkinsLabel, _currentCategory == CosmeticCategory.Skin);
            Tint(tabTrailsImage, tabTrailsLabel, _currentCategory == CosmeticCategory.Trail);
            Tint(tabExclusiveImage, tabExclusiveLabel, _currentCategory == CosmeticCategory.Exclusive);
        }

        private void Tint(Image tabImage, Text label, bool selected)
        {
            if (tabImage != null) tabImage.color = selected ? tabSelectedColor : tabUnselectedColor;
            if (label != null) label.color = selected ? tabSelectedTextColor : tabUnselectedTextColor;
        }

        public void Close() => root.SetActive(false);
    }
}

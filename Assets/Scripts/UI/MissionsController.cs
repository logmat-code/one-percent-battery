using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using UnityEngine;

namespace OnePercentBattery.UI
{
    /// <summary>Daily missions + monthly challenges list.</summary>
    public sealed class MissionsController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform dailyContentParent;
        [SerializeField] private Transform monthlyContentParent;
        [SerializeField] private MissionItemView itemPrefab;

        private void OnEnable()
        {
            MissionManager.Instance.EnsureCurrentPeriods();
            MissionManager.Instance.ProgressChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            MissionManager.Instance.ProgressChanged -= Refresh;
        }

        private void Refresh()
        {
            Populate(dailyContentParent, MissionCatalog.Daily);
            Populate(monthlyContentParent, MissionCatalog.Monthly);
        }

        private void Populate(Transform parent, MissionDefinition[] definitions)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }

            foreach (var def in definitions)
            {
                MissionItemView view = Instantiate(itemPrefab, parent);
                view.Bind(def);
            }
        }

        public void Close() => root.SetActive(false);
    }
}

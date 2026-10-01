using System.Collections.Generic;
using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using UnityEngine;

namespace OnePercentBattery.UI
{
    /// <summary>Daily missions + monthly challenges list - shows only the current 5-mission rotation for each period (see MissionManager.GetActiveDailyMissions/GetActiveMonthlyMissions), never the full pool.</summary>
    public sealed class MissionsController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Transform dailyContentParent;
        [SerializeField] private Transform monthlyContentParent;
        [SerializeField] private MissionItemView itemPrefab;

        private void OnEnable()
        {
            // Panel open is one of the three checkpoints (app start/resume are the others, both already
            // routed through MissionManager.Awake/HandleRunStarted) where a 24h/30d-elapsed rotation
            // should actually take effect - EnsureCurrentPeriods() is a cheap no-op otherwise.
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
            Populate(dailyContentParent, MissionManager.Instance.GetActiveDailyMissions());
            Populate(monthlyContentParent, MissionManager.Instance.GetActiveMonthlyMissions());
        }

        private void Populate(Transform parent, List<MissionDefinition> definitions)
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

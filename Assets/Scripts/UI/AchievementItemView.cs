using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>One tile in the Profile screen's achievements grid.</summary>
    public sealed class AchievementItemView : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Text rewardText;
        [SerializeField] private GameObject lockedOverlay;

        public void Bind(AchievementDefinition definition)
        {
            nameText.text = definition.DisplayName;
            rewardText.text = $"+{definition.RewardCoins}";
            lockedOverlay.SetActive(!AchievementManager.Instance.IsUnlocked(definition.Id));
        }
    }
}

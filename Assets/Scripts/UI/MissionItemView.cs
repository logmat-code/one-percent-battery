using OnePercentBattery.Data;
using OnePercentBattery.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>One row in the daily/monthly Missions list.</summary>
    public sealed class MissionItemView : MonoBehaviour
    {
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text progressText;
        [SerializeField] private Text rewardText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Button claimButton;
        [SerializeField] private GameObject claimedCheckmark;

        private MissionDefinition _definition;

        public void Bind(MissionDefinition definition)
        {
            _definition = definition;
            var progress = MissionManager.Instance.GetProgress(definition.Id);
            float current = progress != null ? progress.progress : 0f;
            bool claimed = progress != null && progress.claimed;
            bool canClaim = !claimed && current >= definition.Target;

            descriptionText.text = definition.Description;
            progressText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(definition.Target)}";
            rewardText.text = $"+{definition.RewardCoins}";
            progressSlider.minValue = 0f;
            progressSlider.maxValue = definition.Target;
            progressSlider.value = current;

            claimedCheckmark.SetActive(claimed);
            claimButton.gameObject.SetActive(!claimed);
            claimButton.interactable = canClaim;

            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(HandleClaimClicked);
        }

        private void HandleClaimClicked()
        {
            if (MissionManager.Instance.TryClaim(_definition))
            {
                Bind(_definition);
            }
        }
    }
}

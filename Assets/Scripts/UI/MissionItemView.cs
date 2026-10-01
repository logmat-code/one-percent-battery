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
        private RectTransform _cardRect;
        private LayoutElement _layoutElement;

        // Captured once from the prefab's own authored single-line layout, rather than hardcoded, so a
        // future Inspector resize of the card or DescriptionText can't silently desync from this baseline -
        // see ResizeForDescription.
        private float _baseCardHeight;
        private float _baseDescriptionHeight;

        private void Awake()
        {
            _cardRect = (RectTransform)transform;
            _layoutElement = GetComponent<LayoutElement>();
            _baseCardHeight = _cardRect.sizeDelta.y;
            _baseDescriptionHeight = descriptionText.rectTransform.sizeDelta.y;
        }

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

            ResizeForDescription();
        }

        /// <summary>
        /// DescriptionText carries its own ContentSizeFitter (vertical: PreferredSize) so wrapping to 2-3
        /// lines grows ITS box instead of truncating - but nothing above automatically knows to grow the
        /// CARD to match. This reads the description's post-wrap height and grows the card (and reports
        /// it via LayoutElement) by the same delta, so the parent list's VerticalLayoutGroup (childControlHeight
        /// = false, so it trusts each card's own reported height) reserves the right amount of space and
        /// pushes the next card down instead of letting it overlap this one. The bottom-anchored rows
        /// (progress/claim) need no repositioning of their own: anchored to the card's bottom edge with a
        /// fixed offset, they ride down automatically as the card's bottom edge moves with it.
        /// </summary>
        private void ResizeForDescription()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionText.rectTransform);
            float extraHeight = Mathf.Max(0f, descriptionText.rectTransform.rect.height - _baseDescriptionHeight);
            float newHeight = _baseCardHeight + extraHeight;

            _cardRect.sizeDelta = new Vector2(_cardRect.sizeDelta.x, newHeight);
            if (_layoutElement != null) _layoutElement.preferredHeight = newHeight;

            LayoutRebuilder.MarkLayoutForRebuild(_cardRect.parent as RectTransform);
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

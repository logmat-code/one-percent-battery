using System.Collections;
using OnePercentBattery.Core;
using OnePercentBattery.Save;
using OnePercentBattery.Services;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>"OUT OF CHARGE" screen: stats, Revive / Double Coins / Retry / Home, and share stubs.</summary>
    public sealed class GameOverController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text causeText;
        [SerializeField] private GameObject newRecordBadge;
        [SerializeField] private Text distanceText;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text bestText;
        [SerializeField] private Text survivalText;
        [SerializeField] private Button secondChanceButton;
        [SerializeField] private Button doubleCoinsButton;
        [SerializeField] private Text doubleCoinsButtonLabel;
        [SerializeField] private Text adFailureMessageText;
        [SerializeField] private float adFailureMessageSeconds = 2.5f;

        private int _coinsEarnedThisRun;
        private Coroutine _failureMessageRoutine;

        private void Awake()
        {
            // This component lives on the panel it toggles, which starts inactive - OnEnable
            // would never fire, so subscriptions happen in Awake instead (fires once at scene load).
            GameEvents.RunAwaitingContinue += HandleRunAwaitingContinue;
            GameEvents.SecondChanceGranted += HandleSecondChanceGranted;
        }

        private void OnDestroy()
        {
            GameEvents.RunAwaitingContinue -= HandleRunAwaitingContinue;
            GameEvents.SecondChanceGranted -= HandleSecondChanceGranted;
        }

        private void HandleRunAwaitingContinue(RunResult preview, bool canOfferSecondChance)
        {
            root.SetActive(true);
            titleText.text = "OUT OF CHARGE";
            causeText.text = preview.Cause == RunEndCause.HitObstacle ? "HIT AN OBSTACLE" : "RAN OUT OF ENERGY";
            newRecordBadge.SetActive(preview.IsNewRecord);

            _coinsEarnedThisRun = preview.CoinsCollected;
            distanceText.text = $"{Mathf.FloorToInt(preview.DistanceMeters)} m";
            coinsText.text = $"+{preview.CoinsCollected}";
            bestText.text = $"{Mathf.FloorToInt(SaveSystem.Data.BestDistanceFor(preview.Mode))} m";
            survivalText.text = $"{preview.SurvivalModeSeconds:0.0}s";

            if (adFailureMessageText != null) adFailureMessageText.gameObject.SetActive(false);

            secondChanceButton.gameObject.SetActive(canOfferSecondChance);
            secondChanceButton.interactable = canOfferSecondChance && AdsManager.Instance.IsRewardedAvailable(RewardedAdPlacement.Revive);

            if (doubleCoinsButton != null)
            {
                bool canOfferDoubleCoins = preview.CoinsCollected > 0 && !GameManager.Instance.DoubleCoinsClaimedThisRun;
                doubleCoinsButton.gameObject.SetActive(canOfferDoubleCoins);
                doubleCoinsButton.interactable = canOfferDoubleCoins && AdsManager.Instance.IsRewardedAvailable(RewardedAdPlacement.DoubleCoins);
                if (doubleCoinsButtonLabel != null) doubleCoinsButtonLabel.text = $"WATCH AD -> +{preview.CoinsCollected}";
            }
        }

        private void HandleSecondChanceGranted()
        {
            root.SetActive(false);
        }

        public void OnSecondChanceClicked()
        {
            secondChanceButton.interactable = false;
            GameManager.Instance.RequestSecondChance(onFailedOrSkipped: () =>
            {
                // Root may already be hidden if the run somehow ended between the click and this
                // callback (e.g. a scene/UI teardown) - guard rather than touch a dead panel.
                if (secondChanceButton == null || !root.activeSelf) return;
                secondChanceButton.interactable = AdsManager.Instance.IsRewardedAvailable(RewardedAdPlacement.Revive);
                ShowAdFailureMessage();
            });
        }

        public void OnDoubleCoinsClicked()
        {
            if (doubleCoinsButton == null) return;
            doubleCoinsButton.interactable = false;
            GameManager.Instance.RequestDoubleCoins(_coinsEarnedThisRun,
                onGranted: () =>
                {
                    if (!root.activeSelf) return;
                    coinsText.text = $"+{_coinsEarnedThisRun * 2}";
                    doubleCoinsButton.gameObject.SetActive(false);
                },
                onFailedOrSkipped: () =>
                {
                    if (doubleCoinsButton == null || !root.activeSelf) return;
                    doubleCoinsButton.interactable = AdsManager.Instance.IsRewardedAvailable(RewardedAdPlacement.DoubleCoins);
                    ShowAdFailureMessage();
                });
        }

        private void ShowAdFailureMessage()
        {
            if (adFailureMessageText == null) return;
            if (_failureMessageRoutine != null) StopCoroutine(_failureMessageRoutine);
            _failureMessageRoutine = StartCoroutine(ShowAdFailureMessageRoutine());
        }

        private IEnumerator ShowAdFailureMessageRoutine()
        {
            adFailureMessageText.text = "Ad unavailable. Try again.";
            adFailureMessageText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(adFailureMessageSeconds);
            adFailureMessageText.gameObject.SetActive(false);
        }

        public void OnRetryClicked()
        {
            root.SetActive(false);
            GameManager.Instance.RetrySameMode();
        }

        public void OnHomeClicked()
        {
            root.SetActive(false);
            GameManager.Instance.ReturnToMainMenu();
        }

        /// <summary>Stub - wire up a native share sheet / social SDK here.</summary>
        public void OnShareClicked()
        {
            Debug.Log("[GameOverController] Share requested - hook up a native share sheet here.");
        }

        /// <summary>Stub - wire up an Instagram/Snapchat story share SDK here.</summary>
        public void OnStoryClicked()
        {
            Debug.Log("[GameOverController] Story share requested - hook up a social SDK here.");
        }
    }
}

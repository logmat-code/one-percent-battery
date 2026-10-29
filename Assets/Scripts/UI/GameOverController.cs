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
        [Tooltip("Optional - if assigned, the panel fades in instead of popping instantly when a run ends.")]
        [SerializeField] private PanelFadeTransition transition;
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
        private RunResult _currentRunResult;
        private Coroutine _failureMessageRoutine;

        private void Awake()
        {
            // This component lives on the panel it toggles, which starts inactive - OnEnable
            // would never fire, so subscriptions happen in Awake instead (fires once at scene load).
            GameEvents.RunAwaitingContinue += HandleRunAwaitingContinue;
            GameEvents.SecondChanceGranted += HandleSecondChanceGranted;
            AdsManager.AdAvailabilityChanged += RefreshAdButtons;
        }

        private void OnDestroy()
        {
            GameEvents.RunAwaitingContinue -= HandleRunAwaitingContinue;
            GameEvents.SecondChanceGranted -= HandleSecondChanceGranted;
            AdsManager.AdAvailabilityChanged -= RefreshAdButtons;
        }

        private void HandleRunAwaitingContinue(RunResult preview, bool canOfferSecondChance)
        {
            if (transition != null) transition.Show();
            else root.SetActive(true);
            titleText.text = "OUT OF CHARGE";
            causeText.text = DescribeCause(preview.Cause);
            if (newRecordBadge != null)
            {
                newRecordBadge.SetActive(preview.IsNewRecord);
                var badgeText = newRecordBadge.GetComponentInChildren<Text>();
                if (badgeText != null) badgeText.text = "NEW HIGH SCORE";
            }

            _currentRunResult = preview;
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

        private void RefreshAdButtons()
        {
            if (root == null || !root.activeSelf) return;

            if (secondChanceButton != null && secondChanceButton.gameObject.activeSelf)
            {
                bool canOfferSecondChance = GameManager.Instance != null 
                    && GameManager.Instance.ActiveConfig.AllowSecondChance 
                    && !GameManager.Instance.SecondChanceUsedThisRun;
                secondChanceButton.interactable = canOfferSecondChance 
                    && AdsManager.Instance != null 
                    && AdsManager.Instance.IsRewardedAvailable(RewardedAdPlacement.Revive);
            }

            if (doubleCoinsButton != null && doubleCoinsButton.gameObject.activeSelf)
            {
                bool canOfferDoubleCoins = _coinsEarnedThisRun > 0 
                    && GameManager.Instance != null 
                    && !GameManager.Instance.DoubleCoinsClaimedThisRun;
                doubleCoinsButton.interactable = canOfferDoubleCoins 
                    && AdsManager.Instance != null 
                    && AdsManager.Instance.IsRewardedAvailable(RewardedAdPlacement.DoubleCoins);
            }
        }

        private static string DescribeCause(RunEndCause cause)
        {
            switch (cause)
            {
                case RunEndCause.HitObstacle: return "HIT AN OBSTACLE";
                case RunEndCause.FellInPit: return "FELL INTO A PIT";
                default: return "RAN OUT OF ENERGY";
            }
        }

        private void HandleSecondChanceGranted()
        {
            root.SetActive(false);
        }

        public void OnSecondChanceClicked()
        {
            if (secondChanceButton == null || !secondChanceButton.interactable) return;
            secondChanceButton.interactable = false;
            GameManager.Instance.RequestSecondChance(onFailedOrSkipped: () =>
            {
                // Root may already be hidden if the run somehow ended between the click and this
                // callback (e.g. a scene/UI teardown) - guard rather than touch a dead panel.
                if (secondChanceButton == null || !root.activeSelf) return;
                RefreshAdButtons();
                ShowAdFailureMessage();
            });
        }

        public void OnDoubleCoinsClicked()
        {
            if (doubleCoinsButton == null || !doubleCoinsButton.interactable) return;
            doubleCoinsButton.interactable = false;
            GameManager.Instance.RequestDoubleCoins(_coinsEarnedThisRun,
                onGranted: () =>
                {
                    if (!root.activeSelf) return;
                    _currentRunResult = GameManager.Instance.LastResult;
                    _coinsEarnedThisRun = _currentRunResult.CoinsCollected;
                    coinsText.text = $"+{_coinsEarnedThisRun}";
                    doubleCoinsButton.gameObject.SetActive(false);
                    if (secondChanceButton != null) secondChanceButton.gameObject.SetActive(false);
                },
                onFailedOrSkipped: () =>
                {
                    if (doubleCoinsButton == null || !root.activeSelf) return;
                    RefreshAdButtons();
                    ShowAdFailureMessage();
                });
        }

        private void ShowAdFailureMessage()
        {
            ShowStatusMessage("Ad unavailable. Try again.");
        }

        private void ShowStatusMessage(string message)
        {
            if (adFailureMessageText == null) return;
            if (_failureMessageRoutine != null) StopCoroutine(_failureMessageRoutine);
            _failureMessageRoutine = StartCoroutine(ShowStatusMessageRoutine(message));
        }

        private IEnumerator ShowStatusMessageRoutine(string message)
        {
            adFailureMessageText.text = message;
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

        /// <summary>Generates a 1080x1920 vertical pixel-art social card for the run that just ended and opens native share.</summary>
        public void OnShareClicked()
        {
            RunResult resultToShare = GameManager.Instance != null ? GameManager.Instance.LastResult : _currentRunResult;
            NativeShareService.ShareRunResult(resultToShare, (ok, msg) =>
            {
                if (root != null && root.activeSelf && !string.IsNullOrEmpty(msg))
                {
                    ShowStatusMessage(msg);
                }
            });
        }

        /// <summary>Shares the vertical 9:16 story card for the completed run.</summary>
        public void OnStoryClicked()
        {
            OnShareClicked();
        }
    }
}

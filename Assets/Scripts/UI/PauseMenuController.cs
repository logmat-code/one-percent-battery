using OnePercentBattery.Core;
using UnityEngine;

namespace OnePercentBattery.UI
{
    /// <summary>Shows/hides the pause overlay. Buttons wire directly to GameManager's public methods in the Inspector.</summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        private void Awake()
        {
            // This component lives on the panel it toggles, which starts inactive - OnEnable
            // would never fire, so subscriptions happen in Awake instead (fires once at scene load).
            GameEvents.GamePaused += HandlePaused;
            GameEvents.GameResumed += HandleResumed;
            GameEvents.ReturnedToMenu += HandleResumed;
        }

        private void OnDestroy()
        {
            GameEvents.GamePaused -= HandlePaused;
            GameEvents.GameResumed -= HandleResumed;
            GameEvents.ReturnedToMenu -= HandleResumed;
        }

        private void HandlePaused() => root.SetActive(true);
        private void HandleResumed() => root.SetActive(false);
    }
}

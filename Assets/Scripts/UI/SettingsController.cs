using OnePercentBattery.Save;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>Sound/Haptics/Practice Mode toggles and local data deletion.</summary>
    public sealed class SettingsController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private Toggle practiceModeToggle;
        [SerializeField] private GameObject resetConfirmPanel;

        [Header("Support links (leave blank pre-release - the buttons stay disabled until set)")]
        [Tooltip("Your published privacy policy URL. Google Play requires a working one - this MUST be set before a real submission.")]
        [SerializeField] private string privacyPolicyUrl;
        [Tooltip("Support contact URL (a mailto: link works) shown to players who need help.")]
        [SerializeField] private string contactSupportUrl;
        [SerializeField] private Button privacyPolicyButton;
        [SerializeField] private Button contactSupportButton;

        private void OnEnable()
        {
            var data = SaveSystem.Data;
            soundToggle.SetIsOnWithoutNotify(data.soundEffectsEnabled);
            hapticsToggle.SetIsOnWithoutNotify(data.hapticsEnabled);
            practiceModeToggle.SetIsOnWithoutNotify(data.practiceMode);

            soundToggle.onValueChanged.AddListener(OnSoundChanged);
            hapticsToggle.onValueChanged.AddListener(OnHapticsChanged);
            practiceModeToggle.onValueChanged.AddListener(OnPracticeModeChanged);

            // Disabled rather than hidden: players (and Play reviewers) expect these to exist, but they
            // must not look tappable-yet-broken while no real URL has been supplied.
            if (privacyPolicyButton != null) privacyPolicyButton.interactable = !string.IsNullOrEmpty(privacyPolicyUrl);
            if (contactSupportButton != null) contactSupportButton.interactable = !string.IsNullOrEmpty(contactSupportUrl);

            if (resetConfirmPanel != null) resetConfirmPanel.SetActive(false);
        }

        private void OnDisable()
        {
            soundToggle.onValueChanged.RemoveListener(OnSoundChanged);
            hapticsToggle.onValueChanged.RemoveListener(OnHapticsChanged);
            practiceModeToggle.onValueChanged.RemoveListener(OnPracticeModeChanged);
        }

        private void OnSoundChanged(bool value)
        {
            SaveSystem.Data.soundEffectsEnabled = value;
            AudioListener.volume = value ? 1f : 0f;
            SaveSystem.Save();
        }

        private void OnHapticsChanged(bool value)
        {
            SaveSystem.Data.hapticsEnabled = value;
            SaveSystem.Save();
        }

        private void OnPracticeModeChanged(bool value)
        {
            SaveSystem.Data.practiceMode = value;
            SaveSystem.Save();
        }

        public void OnRequestResetData() => resetConfirmPanel.SetActive(true);
        public void OnCancelResetData() => resetConfirmPanel.SetActive(false);

        public void OnConfirmResetData()
        {
            SaveSystem.ResetAll();
            resetConfirmPanel.SetActive(false);
            OnEnable();
        }

        /// <summary>No-ops (and the button stays disabled - see OnEnable) until privacyPolicyUrl is set.</summary>
        public void OnPrivacyPolicyClicked()
        {
            if (string.IsNullOrEmpty(privacyPolicyUrl))
            {
                Debug.LogWarning("[SettingsController] No privacy policy URL configured - set privacyPolicyUrl before release.");
                return;
            }
            Application.OpenURL(privacyPolicyUrl);
        }

        /// <summary>No-ops (and the button stays disabled - see OnEnable) until contactSupportUrl is set.</summary>
        public void OnContactSupportClicked()
        {
            if (string.IsNullOrEmpty(contactSupportUrl))
            {
                Debug.LogWarning("[SettingsController] No support contact URL configured - set contactSupportUrl before release.");
                return;
            }
            Application.OpenURL(contactSupportUrl);
        }

        public void Close() => root.SetActive(false);
    }
}

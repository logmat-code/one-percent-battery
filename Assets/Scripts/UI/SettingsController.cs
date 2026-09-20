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

        private void OnEnable()
        {
            var data = SaveSystem.Data;
            soundToggle.SetIsOnWithoutNotify(data.soundEffectsEnabled);
            hapticsToggle.SetIsOnWithoutNotify(data.hapticsEnabled);
            practiceModeToggle.SetIsOnWithoutNotify(data.practiceMode);

            soundToggle.onValueChanged.AddListener(OnSoundChanged);
            hapticsToggle.onValueChanged.AddListener(OnHapticsChanged);
            practiceModeToggle.onValueChanged.AddListener(OnPracticeModeChanged);

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

        /// <summary>Stub - point this at your real privacy policy URL.</summary>
        public void OnPrivacyPolicyClicked()
        {
            Debug.Log("[SettingsController] Open the privacy policy URL here once you have one.");
        }

        /// <summary>Stub - point this at your real support contact (email/URL).</summary>
        public void OnContactSupportClicked()
        {
            Debug.Log("[SettingsController] Open your support contact flow here.");
        }

        public void Close() => root.SetActive(false);
    }
}

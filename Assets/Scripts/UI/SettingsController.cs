using OnePercentBattery.Save;
using UnityEngine;
using UnityEngine.UI;

namespace OnePercentBattery.UI
{
    /// <summary>Sound/Haptics/Practice Mode toggles and local data deletion.</summary>
    public sealed class SettingsController : MonoBehaviour
    {
        private const string PrivacyPolicyUrl = "https://sites.google.com/view/1-battery/inicio?read_current=1";
        private const string SupportEmail = "matiasrigozzi@gmail.com";

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

        public void OnPrivacyPolicyClicked() => Application.OpenURL(PrivacyPolicyUrl);

        public void OnContactSupportClicked() => Application.OpenURL("mailto:" + SupportEmail);

        public void Close() => root.SetActive(false);
    }
}

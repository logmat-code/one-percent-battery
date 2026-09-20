using System;
using UnityEngine;

namespace OnePercentBattery.Save
{
    /// <summary>Loads/persists the single <see cref="SaveData"/> blob. All local storage, no backend/account.</summary>
    public static class SaveSystem
    {
        private const string PlayerPrefsKey = "OnePercentBattery.SaveData.v1";

        private static SaveData _cached;

        public static SaveData Data
        {
            get
            {
                if (_cached == null)
                {
                    Load();
                }
                return _cached;
            }
        }

        public static event Action Saved;

        private static void Load()
        {
            string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                _cached = new SaveData();
                return;
            }

            try
            {
                _cached = JsonUtility.FromJson<SaveData>(json);
                if (_cached == null)
                {
                    _cached = new SaveData();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] Corrupt save data, starting fresh. {e.Message}");
                _cached = new SaveData();
            }
        }

        public static void Save()
        {
            string json = JsonUtility.ToJson(Data);
            PlayerPrefs.SetString(PlayerPrefsKey, json);
            PlayerPrefs.Save();
            Saved?.Invoke();
        }

        /// <summary>Data deletion, matching the "Privacy policy &amp; data deletion" option in Settings.</summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            _cached = new SaveData();
            Saved?.Invoke();
        }
    }
}

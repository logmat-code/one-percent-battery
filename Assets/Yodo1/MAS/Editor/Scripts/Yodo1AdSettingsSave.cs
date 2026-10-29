using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Yodo1.MAS
{
    public static class Yodo1AdSettingsSave
    {
        private const string YODO1_RESOURCE_PATH = "Assets/Resources/Yodo1/";
        private const string YODO1_ADS_SETTINGS_PATH = YODO1_RESOURCE_PATH + "Yodo1AdSettings.asset";

        public static Yodo1AdSettings Load()
        {
            var settings = AssetDatabase.LoadAssetAtPath<Yodo1AdSettings>(YODO1_ADS_SETTINGS_PATH);
            if (settings != null)
            {
                return settings;
            }

            settings = ScriptableObject.CreateInstance<Yodo1AdSettings>();
            try
            {
                string resPath = Path.GetFullPath(YODO1_RESOURCE_PATH);
                if (!Directory.Exists(resPath))
                {
                    Directory.CreateDirectory(resPath);
                }

                AssetDatabase.CreateAsset(settings, YODO1_ADS_SETTINGS_PATH);
                Save(settings);
            }
            catch (Exception e)
            {
                Debug.LogError(Yodo1U3dMas.TAG + "Failed to create the Yodo1 Ad Settings asset: " + e.Message);
            }

            return settings;
        }

        public static void Save(Yodo1AdSettings settings)
        {
            if (settings == null)
            {
                Debug.LogWarning(Yodo1U3dMas.TAG + "Cannot save null Yodo1AdSettings.");
                return;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        public static bool CheckConfiguration_iOS(Yodo1AdSettings settings)
        {
            if (settings == null)
            {
                return LogConfigError("MAS iOS settings is null, please check the configuration.");
            }

            if (string.IsNullOrWhiteSpace(settings.iOSSettings.AppKey))
            {
                return LogConfigError("MAS iOS AppKey is null, please check the configuration.");
            }

            if (settings.iOSSettings.GlobalRegion && string.IsNullOrWhiteSpace(settings.iOSSettings.AdmobAppID))
            {
                return LogConfigError("MAS iOS AdMob App ID is null, please check the configuration.");
            }

            return true;
        }

        /// <summary>
        /// Logs a configuration error and shows an alert dialog.
        /// Always returns false for convenient use in validation methods.
        /// </summary>
        private static bool LogConfigError(string message)
        {
            Debug.LogError(Yodo1U3dMas.TAG + message);
            Yodo1AdUtils.ShowAlert("Error", message, "Ok");
            return false;
        }
    }
}

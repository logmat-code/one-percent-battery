namespace Yodo1.MAS
{
    using System;
    using System.Threading;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Syncs app info from the MAS server to local Yodo1AdSettings.
    /// Pairs with Yodo1AdSettingsSave which handles persistence.
    /// </summary>
    public static class Yodo1AdSettingsSync
    {
        /// <summary>
        /// Apply app info to the corresponding platform settings and save.
        /// </summary>
        public static void ApplyToSettings(Yodo1AdSettings settings, Yodo1AppInfo appInfo)
        {
            if (settings == null || appInfo == null || !appInfo.HasRequiredFields)
            {
                return;
            }

            if (appInfo.IsIOS)
            {
                settings.iOSSettings.AppKey = appInfo.AppKey;
                settings.iOSSettings.AdmobAppID = appInfo.AdmobKey;
                settings.iOSSettings.BundleID = appInfo.BundleId;
            }
            else if (appInfo.IsAndroid)
            {
                settings.androidSettings.AppKey = appInfo.AppKey;
                settings.androidSettings.AdmobAppID = appInfo.AdmobKey;
                settings.androidSettings.BundleID = appInfo.BundleId;
            }

            Yodo1AdSettingsSave.Save(settings);
        }

        /// <summary>
        /// Fetch app info by bundle ID on a background thread,
        /// then apply to settings on the main thread.
        /// </summary>
        public static void FetchAndApplyAsync(string platform, string bundleId)
        {
            Yodo1Net net = Yodo1Net.GetInstance();

            Thread thread = new Thread(() =>
            {
                Yodo1AppInfo appInfo = null;
                try
                {
                    appInfo = net.GetAppInfoByBundleID(platform, bundleId);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(Yodo1U3dMas.TAG + "Failed to fetch " + platform + " app info: " + e.Message);
                }

                EditorApplication.delayCall += () =>
                {
                    Yodo1AdSettings settings = Yodo1AdSettingsSave.Load();
                    ApplyToSettings(settings, appInfo);
                };
            });
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// Fetch app info for both Android and iOS on background threads and apply.
        /// No-op if network is unreachable.
        /// </summary>
        public static void RefreshAllAsync()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                return;
            }

            FetchAndApplyAsync("android", Yodo1AdUtils.GetAndroidBundleId());
            FetchAndApplyAsync("iOS", Yodo1AdUtils.GetIOSBundleId());
        }

        /// <summary>
        /// Synchronous pre-build sync: fetches the latest app info by appKey
        /// and updates settings if the AdMob ID has been customized.
        /// Called by Yodo1IdSync during the build pipeline.
        /// </summary>
        public static void SyncBeforeBuild()
        {
#if UNITY_ANDROID
            if (!Yodo1AdUtils.IsGooglePlayVersion())
            {
                return;
            }
#endif

            Yodo1AdSettings settings = Yodo1AdSettingsSave.Load();
            if (settings == null)
            {
                Debug.LogWarning(Yodo1U3dMas.TAG + "Yodo1AdSettings is null, skipping pre-build sync.");
                return;
            }

            string appKey = string.Empty;
            string admobAppID = string.Empty;
            string defaultAdmobAppID = string.Empty;
#if UNITY_ANDROID
            appKey = settings.androidSettings.AppKey;
            admobAppID = settings.androidSettings.AdmobAppID;
            defaultAdmobAppID = "ca-app-pub-5580537606944457~4465578836";
#elif UNITY_IOS
            appKey = settings.iOSSettings.AppKey;
            admobAppID = settings.iOSSettings.AdmobAppID;
            defaultAdmobAppID = "ca-app-pub-5580537606944457~2166718551";
#endif

            if (string.IsNullOrEmpty(appKey))
            {
                return;
            }

            Yodo1AppInfo appInfo;
            try
            {
                appInfo = Yodo1Net.GetInstance().GetAppInfoByAppKey(appKey);
            }
            catch (Exception e)
            {
                Debug.LogWarning(Yodo1U3dMas.TAG + "Failed to fetch app info during build: " + e.Message);
                return;
            }

            if (appInfo == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(appInfo.BundleId))
            {
                Debug.Log(Yodo1U3dMas.TAG + "Update the store link when your game is live on Play Store or App Store.");
                return;
            }

            if (!string.Equals(admobAppID, defaultAdmobAppID))
            {
                ApplyToSettings(settings, appInfo);
            }
        }
    }
}

namespace Yodo1.MAS
{
    using UnityEngine;
    using UnityEditor;

    public enum Yodo1PlatfromTarget
    {
        Android,
        iOS
    }

    public class Yodo1AdWindows : EditorWindow
    {
        private static EditorWindow window;
        private static Yodo1PlatfromTarget selectedPlatform;
        private static bool isAdmobRequired = false;

        private Yodo1AdSettings adSettings;
        private bool hasInitialSync = false;
        private string syncErrorMessage = string.Empty;
        private Vector2 scrollPosition;

        private Yodo1PlatformSettings CurrentSettings => selectedPlatform == Yodo1PlatfromTarget.iOS ? (Yodo1PlatformSettings)adSettings.iOSSettings : adSettings.androidSettings;

        public static void Initialize(Yodo1PlatfromTarget platfromTab)
        {
            if (window != null)
            {
                window.Close();
                window = null;
            }

            window = GetWindow(typeof(Yodo1AdWindows), false, platfromTab.ToString() + " Setting", true);
            window.minSize = new Vector2(550, 180);
            window.Show();

            selectedPlatform = platfromTab;
            isAdmobRequired = Yodo1AdUtils.IsAdMobValid(selectedPlatform);
        }

        #region Lifecycle

        private void OnEnable()
        {
            Yodo1AdSettingsSync.RefreshAllAsync();
            this.adSettings = Yodo1AdSettingsSave.Load();
        }

        private void OnDisable()
        {
            this.SaveConfig();
            this.adSettings = null;
        }

        private void OnGUI()
        {
            this.scrollPosition = GUILayout.BeginScrollView(this.scrollPosition);
            DrawConfigureContent();

            GUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(10, 10, 10, 0) });
            if (GUILayout.Button("Save Configuration"))
            {
                this.SaveConfig();
            }
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }

        #endregion

        #region Draw

        private void DrawConfigureContent()
        {
            GUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(20, 10, 20, 0) });

            var sectionStyle = new GUIStyle { padding = new RectOffset(0, 10, 10, 0) };
            DrawAppKeyContent(sectionStyle);

            if (isAdmobRequired)
            {
                DrawAdMobAppIdContent(sectionStyle);
            }

            GUILayout.EndVertical();
        }

        private void DrawAppKeyContent(GUIStyle sectionStyle)
        {
            var settings = CurrentSettings;

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal(sectionStyle);

            GUILayout.Label("MAS App Key", GUILayout.Width(120));

            string newKey = GUILayout.TextField(settings.AppKey.Trim());
            if (newKey != settings.AppKey)
            {
                settings.AppKey = newKey;
                syncErrorMessage = SyncAppInfo(newKey);
            }

            if (!string.IsNullOrEmpty(settings.AppKey) &&
                !hasInitialSync &&
                string.IsNullOrWhiteSpace(settings.AdmobAppID))
            {
                syncErrorMessage = SyncAppInfo(settings.AppKey);
                hasInitialSync = true;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            if (string.IsNullOrWhiteSpace(settings.AppKey))
            {
                EditorGUILayout.HelpBox("Please fill in the MAS app key correctly, you can find your app key on the MAS dashboard.", MessageType.Error);
                GUILayout.Space(15);
            }

            GUILayout.EndVertical();
        }

        private void DrawAdMobAppIdContent(GUIStyle sectionStyle)
        {
            var settings = CurrentSettings;

            GUILayout.BeginVertical(sectionStyle);
            GUILayout.BeginHorizontal(sectionStyle);

            GUILayout.Label("AdMob App ID", GUILayout.Width(120));
            settings.AdmobAppID = GUILayout.TextField(settings.AdmobAppID);

            GUILayout.Space(20);
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                syncErrorMessage = SyncAppInfo(settings.AppKey);
                this.SaveConfig();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            if (string.IsNullOrWhiteSpace(settings.AdmobAppID))
            {
                if (string.IsNullOrEmpty(syncErrorMessage))
                {
                    EditorGUILayout.HelpBox("A null or incorrect value will cause a crash when it builds. Please make sure to copy Admob App ID from MAS dashboard.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(syncErrorMessage, MessageType.Error);
                }
            }

            GUILayout.EndVertical();
        }

        #endregion

        #region Config

        private void SaveConfig()
        {
            if (selectedPlatform == Yodo1PlatfromTarget.Android)
            {
#if UNITY_ANDROID
                if (Yodo1PostProcessAndroid.CheckConfiguration_Android(this.adSettings))
                {
#if UNITY_2019_1_OR_NEWER
#else
                    Yodo1PostProcessAndroid.ValidateManifest(this.adSettings);
                    Yodo1PostProcessAndroid.GenerateGradle();
#endif
                }
                else
                {
                    return;
                }
#endif
            }
            if (selectedPlatform == Yodo1PlatfromTarget.iOS)
            {
                if (!Yodo1AdSettingsSave.CheckConfiguration_iOS(this.adSettings))
                {
                    return;
                }
            }

            Yodo1AdSettingsSave.Save(this.adSettings);
        }

        #endregion

        #region Network

        private string SyncAppInfo(string appKey)
        {
            if (!isAdmobRequired)
            {
                return string.Empty;
            }
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                return "Please check your network. You can also fill in manually.";
            }
            if (string.IsNullOrEmpty(appKey))
            {
                return "Please enter the correct MAS App Key.";
            }

            Yodo1AppInfo appInfo;
            try
            {
                appInfo = Yodo1Net.GetInstance().GetAppInfoByAppKey(appKey);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(Yodo1U3dMas.TAG + "Failed to fetch app info in settings window: " + e.Message);
                return "Network request failed. You can also fill in manually.";
            }

            if (appInfo == null)
            {
                return "MAS App Key not found. please fill in correctly.";
            }

            Yodo1AdSettingsSync.ApplyToSettings(this.adSettings, appInfo);
            return string.Empty;
        }

        #endregion
    }
}

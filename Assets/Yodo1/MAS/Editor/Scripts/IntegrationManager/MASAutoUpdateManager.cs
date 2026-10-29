using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Yodo1.MAS;

[InitializeOnLoad]
public static class MASAutoUpdateManager
{
    private const string PREF_LAST_CHECK_TIME = "MASLastUpdateCheckTime";
    private const string PREF_AUTO_UPDATE = "MASSDKAutoUpdate";
    private const int SECONDS_IN_A_DAY = 86400;

    private static UnityWebRequest webRequest;

    static MASAutoUpdateManager()
    {
        var now = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

        if (EditorPrefs.HasKey(PREF_LAST_CHECK_TIME))
        {
            var elapsedTime = now - EditorPrefs.GetInt(PREF_LAST_CHECK_TIME);
            if (elapsedTime < SECONDS_IN_A_DAY) return;
        }

        EditorPrefs.SetInt(PREF_LAST_CHECK_TIME, now);

        if (!EditorPrefs.GetBool(PREF_AUTO_UPDATE, true)) return;

#if UNITY_ANDROID
        if (!Yodo1AdUtils.IsGooglePlayVersion()) return;
#endif

        Yodo1AdNetworkManager.GetInstance().InitAdNetworkConfig();
        var adNetworkConfig = Yodo1AdNetworkManager.GetInstance().GetAdNetworkConfig();
        if (adNetworkConfig == null) return;

        string currentVersion = Yodo1AdNetworkManager.GetInstance().GetCurMakSdkVersion();
        string latestVersion = adNetworkConfig.latestSdkversion;

        if (Yodo1AdNetworkUtil.IsPrerelease(currentVersion)) return;
        if (Yodo1AdNetworkUtil.CompareVersions(currentVersion, latestVersion) != -1) return;

        int option = EditorUtility.DisplayDialogComplex(
            "Yodo1 MAS SDK Update",
            "A new version of MAS SDK is available for download. Update now?",
            "Download",
            "Not Now",
            "Don't Ask Again");

        switch (option)
        {
            case 0:
                string downloadUrl = adNetworkConfig.sdkDownloadUrl;
                string packageName = ExtractPackageName(downloadUrl);
                EditorCoroutineRunner.StartEditorCoroutine(DownloadPlugin(downloadUrl, packageName));
                break;
            case 2:
                EditorPrefs.SetBool(PREF_AUTO_UPDATE, false);
                break;
        }
    }

    private static string ExtractPackageName(string downloadUrl)
    {
        var parts = downloadUrl.Split(new[] { ".unitypackage" }, StringSplitOptions.None);
        var name = parts[0].Substring(parts[0].LastIndexOf("/") + 1);
        if (name.Contains("-"))
        {
            name = name.Split(new[] { "-beta" }, StringSplitOptions.None)[0];
        }
        return name;
    }

    private static IEnumerator DownloadPlugin(string downloadUrl, string packageName)
    {
        var path = Path.Combine(Application.temporaryCachePath, packageName + ".unitypackage");
        var downloadHandler = new DownloadHandlerFile(path);
        webRequest = new UnityWebRequest(downloadUrl)
        {
            method = UnityWebRequest.kHttpVerbGET,
            downloadHandler = downloadHandler
        };

        var operation = webRequest.SendWebRequest();
        while (!operation.isDone)
        {
            yield return null;
        }

#if UNITY_2020_1_OR_NEWER
        if (webRequest.result != UnityWebRequest.Result.Success)
#elif UNITY_2017_2_OR_NEWER
        if (webRequest.isNetworkError || webRequest.isHttpError)
#else
        if (webRequest.isError)
#endif
        {
            Debug.LogWarning(Yodo1U3dMas.TAG + "Auto-update download failed: " + webRequest.error);
        }
        else
        {
            AssetDatabase.ImportPackage(path, true);
        }

        webRequest.Dispose();
        webRequest = null;
    }
}

using System;
using System.IO;
using OnePercentBattery.Core;
using OnePercentBattery.Data;
using OnePercentBattery.Save;
using UnityEngine;

namespace OnePercentBattery.Services
{
    /// <summary>
    /// Generates 1080x1920 social share cards and opens the platform's native share sheet.
    /// On Android, writes to temporary cache / MediaStore and launches Intent.ACTION_SEND.
    /// In Editor / Desktop, saves the PNG to persistentDataPath and logs the file location.
    /// </summary>
    public static class NativeShareService
    {
        public static string LastSavedSharePath { get; private set; }

        public static void ShareHomeStats(Action<bool, string> onCompleted = null)
        {
            try
            {
                byte[] pngBytes = ShareCardGenerator.GenerateHomeStatsCardPng();
                var data = SaveSystem.Data;
                int best = Mathf.FloorToInt(data.OverallBestDistance());
                string message = $"My personal best in 1% BATTERY is {best}m across {data.totalRuns} runs! Can you survive at 1%?";
                string fileName = "OnePercentBattery_Stats.png";
                SharePng(pngBytes, fileName, "1% BATTERY - Player Stats", message, onCompleted);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NativeShareService] Failed to share stats card: {ex.Message}");
                onCompleted?.Invoke(false, "Could not generate share card.");
            }
        }

        public static void ShareRunResult(RunResult result, Action<bool, string> onCompleted = null)
        {
            try
            {
                byte[] pngBytes = ShareCardGenerator.GenerateRunResultCardPng(result);
                int dist = Mathf.FloorToInt(result.DistanceMeters);
                string modeName = RunModeConfig.For(result.Mode).DisplayName;
                string recordTag = result.IsNewRecord ? " (NEW RECORD!)" : "";
                string message = $"I ran {dist}m in {modeName} Mode{recordTag} and earned +{result.CoinsCollected} coins in 1% BATTERY!";
                string fileName = "OnePercentBattery_Run.png";
                SharePng(pngBytes, fileName, "1% BATTERY - Run Result", message, onCompleted);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NativeShareService] Failed to share run card: {ex.Message}");
                onCompleted?.Invoke(false, "Could not generate share card.");
            }
        }

        private static void SharePng(byte[] pngBytes, string fileName, string subject, string shareText, Action<bool, string> onCompleted)
        {
            string dir = !string.IsNullOrEmpty(Application.temporaryCachePath)
                ? Application.temporaryCachePath
                : Application.persistentDataPath;
            Directory.CreateDirectory(dir);
            string filePath = Path.Combine(dir, fileName);
            File.WriteAllBytes(filePath, pngBytes);
            LastSavedSharePath = filePath;

            // Also keep a copy in persistentDataPath so testers/Editor can inspect easily
            try
            {
                if (!string.IsNullOrEmpty(Application.persistentDataPath) && dir != Application.persistentDataPath)
                {
                    string persistentCopy = Path.Combine(Application.persistentDataPath, fileName);
                    File.WriteAllBytes(persistentCopy, pngBytes);
                    LastSavedSharePath = persistentCopy;
                }
            }
            catch
            {
                // Ignore secondary copy errors
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            bool shared = TryShareAndroid(filePath, pngBytes, subject, shareText);
            if (shared)
            {
                onCompleted?.Invoke(true, "Opening share sheet...");
            }
            else
            {
                onCompleted?.Invoke(false, "Share unavailable on this device.");
            }
#else
            GUIUtility.systemCopyBuffer = shareText;
            Debug.Log($"[NativeShareService] Share card saved to: {LastSavedSharePath} (text copied to clipboard: \"{shareText}\")");
            onCompleted?.Invoke(true, "SHARE CARD SAVED!");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool TryShareAndroid(string filePath, byte[] pngBytes, string subject, string shareText)
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
                {
                    AndroidJavaObject uri = TryInsertMediaStoreImage(activity, pngBytes, subject);
                    if (uri == null)
                    {
                        // Fallback: relax StrictMode VM policy for file:// URI
                        using (var strictMode = new AndroidJavaClass("android.os.StrictMode"))
                        using (var builder = new AndroidJavaObject("android.os.StrictMode$VmPolicy$Builder"))
                        using (var policy = builder.Call<AndroidJavaObject>("build"))
                        {
                            strictMode.CallStatic("setVmPolicy", policy);
                        }
                        using (var fileObj = new AndroidJavaObject("java.io.File", filePath))
                        using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                        {
                            uri = uriClass.CallStatic<AndroidJavaObject>("fromFile", fileObj);
                        }
                    }

                    if (uri != null)
                    {
                        intent.Call<AndroidJavaObject>("setType", "image/png");
                        intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.STREAM", uri);
                        intent.Call<AndroidJavaObject>("addFlags", 1); // FLAG_GRANT_READ_URI_PERMISSION
                    }
                    else
                    {
                        intent.Call<AndroidJavaObject>("setType", "text/plain");
                    }

                    intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.SUBJECT", subject);
                    intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", shareText);

                    using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                    using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, "Share 1% BATTERY"))
                    {
                        activity.Call("startActivity", chooser);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[NativeShareService] Android share failed: {ex.Message}");
                return false;
            }
        }

        private static AndroidJavaObject TryInsertMediaStoreImage(AndroidJavaObject activity, byte[] pngBytes, string title)
        {
            try
            {
                using (var bitmapFactory = new AndroidJavaClass("android.graphics.BitmapFactory"))
                using (var bitmap = bitmapFactory.CallStatic<AndroidJavaObject>("decodeByteArray", pngBytes, 0, pngBytes.Length))
                {
                    if (bitmap == null) return null;
                    using (var resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
                    using (var mediaClass = new AndroidJavaClass("android.provider.MediaStore$Images$Media"))
                    {
                        string uriString = mediaClass.CallStatic<string>("insertImage", resolver, bitmap, title + "_" + DateTime.UtcNow.Ticks, "1% BATTERY Share Card");
                        if (string.IsNullOrEmpty(uriString)) return null;
                        using (var uriClass = new AndroidJavaClass("android.net.Uri"))
                        {
                            return uriClass.CallStatic<AndroidJavaObject>("parse", uriString);
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }
#endif
    }
}

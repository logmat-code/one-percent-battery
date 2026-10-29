using OnePercentBattery.Core;
using OnePercentBattery.Data;
using OnePercentBattery.Save;
using OnePercentBattery.Spawning;
using OnePercentBattery.Utils;
using UnityEngine;

namespace OnePercentBattery.Meta
{
    /// <summary>
    /// Central haptic feedback service on real devices.
    /// Provides differentiated pulses:
    /// - Damage: stronger, longer pulse on impact
    /// - Power-up: lighter, shorter pulse on collecting Energy/Shield/Magnet
    /// Coins produce no haptic feedback.
    /// Duplicate hit/pickup triggers are debounced.
    /// Gated by the Settings "Haptics" toggle (SaveData.hapticsEnabled).
    /// </summary>
    public sealed class HapticsManager : Singleton<HapticsManager>
    {
        private static float _lastDamageTime = -1f;
        private static float _lastPowerUpTime = -1f;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static bool _vibratorInitialized;
        private static int _sdkInt = -1;
#endif

        private void OnEnable()
        {
            GameEvents.ObstacleHit += HandleObstacleHit;
            GameEvents.PickupCollected += HandlePickupCollected;
        }

        private void OnDisable()
        {
            GameEvents.ObstacleHit -= HandleObstacleHit;
            GameEvents.PickupCollected -= HandlePickupCollected;
        }

        private void HandleObstacleHit()
        {
            Damage();
        }

        private void HandlePickupCollected(PickupType type, Vector3 position)
        {
            if (type == PickupType.Energy || type == PickupType.Shield || type == PickupType.Magnet)
            {
                PowerUp();
            }
        }

        public void TriggerDamage() => Damage();
        public void TriggerPowerUp() => PowerUp();

        /// <summary>Stronger, slightly longer haptic pulse triggered when actual damage is taken.</summary>
        public static void Damage()
        {
            if (!SaveSystem.Data.hapticsEnabled) return;
            if (Time.unscaledTime - _lastDamageTime < 0.25f) return;
            _lastDamageTime = Time.unscaledTime;

            Vibrate(durationMs: 65, amplitude: 255);
        }

        /// <summary>Lighter, shorter positive haptic pulse triggered when collecting a power-up (Energy/Shield/Magnet).</summary>
        public static void PowerUp()
        {
            if (!SaveSystem.Data.hapticsEnabled) return;
            if (Time.unscaledTime - _lastPowerUpTime < 0.15f) return;
            _lastPowerUpTime = Time.unscaledTime;

            Vibrate(durationMs: 25, amplitude: 120);
        }

        private static void Vibrate(long durationMs, int amplitude)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                InitAndroidVibrator();
                if (_vibrator != null)
                {
                    if (_sdkInt >= 26)
                    {
                        using (var vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                        using (var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", durationMs, amplitude))
                        {
                            _vibrator.Call("vibrate", effect);
                            return;
                        }
                    }
                    else
                    {
                        _vibrator.Call("vibrate", durationMs);
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[HapticsManager] Android native vibration failed: {ex.Message}");
            }
            Handheld.Vibrate();
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#else
            // In Editor or unsupported platforms: silently do nothing.
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void InitAndroidVibrator()
        {
            if (_vibratorInitialized) return;
            _vibratorInitialized = true;
            try
            {
                using (var versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    _sdkInt = versionClass.GetStatic<int>("SDK_INT");
                }
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[HapticsManager] Could not initialize Android vibrator: {ex.Message}");
                _vibrator = null;
            }
        }
#endif
    }
}

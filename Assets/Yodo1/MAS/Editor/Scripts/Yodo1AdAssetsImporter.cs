namespace Yodo1.MAS
{
    using UnityEngine;
    using UnityEditor;
    using System;

    [InitializeOnLoad]
    public class Yodo1AdAssetsImporter
    {
        /// <summary>
        /// Polls a value and signals a callback with the change after the specified delay
        /// time.
        /// </summary>
        internal class PropertyPoller<T>
        {
            /// <summary>
            /// Delegate that is called when a value changes.
            /// </summary>
            /// <param name="previousValue">Previous value of the property that
            /// changed.</param>
            /// <param name="currentValue">Current value of the property that
            /// changed.</param>
            public delegate void Changed(T previousValue, T currentValue);

            // Whether the previous value has been initialized.
            private bool previousValueInitialized = false;
            // Previous value of the property.
            private T previousValue = default(T);
            // Previous value of the property when it was last polled.
            private T previousPollValue = default(T);
            // Last time the property was polled.
            private DateTime previousPollTime = DateTime.Now;
            // Time to wait before signalling a change.
            private int delayTimeInSeconds;
            // Name of the property being polled.
            private string propertyName;
            // Previous time we checked the property value for a change.
            private DateTime previousCheckTime = DateTime.Now;
            // Time to wait before checking a property.
            private int checkIntervalInSeconds;

            /// <summary>
            /// Create the poller.
            /// </summary>
            /// <param name="propertyName">Name of the property being polled.</param>
            /// <param name="delayTimeInSeconds">Time to wait before signalling that the value
            /// has changed.</param>
            /// <param name="checkIntervalInSeconds">Time to check the value of the property for
            /// changes.</param>
            public PropertyPoller(string propertyName,
                                  int delayTimeInSeconds = 3,
                                  int checkIntervalInSeconds = 1)
            {
                this.propertyName = propertyName;
                this.delayTimeInSeconds = delayTimeInSeconds;
                this.checkIntervalInSeconds = checkIntervalInSeconds;
            }

            /// <summary>
            /// Poll the specified value for changes.
            /// </summary>
            /// <param name="getCurrentValue">Delegate that returns the value being polled.</param>
            /// <param name="changed">Delegate that is called if the value changes.</param>
            public void Poll(Func<T> getCurrentValue, Changed changed)
            {
                var currentTime = DateTime.Now;
                if (currentTime.Subtract(previousCheckTime).TotalSeconds <
                    checkIntervalInSeconds)
                {
                    return;
                }
                previousCheckTime = currentTime;
                T currentValue = getCurrentValue();
                // If the poller isn't initailized, store the current value before polling for
                // changes.
                if (!previousValueInitialized)
                {
                    previousValueInitialized = true;
                    previousValue = currentValue;
                    return;
                }
                if (!currentValue.Equals(previousValue))
                {
                    if (currentValue.Equals(previousPollValue))
                    {
                        if (currentTime.Subtract(previousPollTime).TotalSeconds >=
                            delayTimeInSeconds)
                        {
                            Debug.Log(String.Format("{0} changed: {1} -> {2}", propertyName, previousValue, currentValue));
                            changed(previousValue, currentValue);
                            previousValue = currentValue;
                        }
                    }
                    else
                    {
                        previousPollValue = currentValue;
                        previousPollTime = currentTime;
                    }
                }
            }
        }

        private static PropertyPoller<string> androidBundleIdPoller = new PropertyPoller<string>(Yodo1U3dMas.TAG + "Android Bundle ID");
        private static PropertyPoller<string> iosBundleIdPoller = new PropertyPoller<string>(Yodo1U3dMas.TAG + "iOS Bundle ID");

        static Yodo1AdAssetsImporter()
        {
            // Delay initialization until the editor is not in play mode.
            Google.EditorInitializer.InitializeOnMainThread(condition: () =>
            {
                return !EditorApplication.isPlayingOrWillChangePlaymode;
            }, initializer: Initialize, name: "Yodo1AdAssetsImporter", logger: null);
        }

        /// <summary>
        /// Initialize the module. This should be called on the main thread only if
        /// current active build target is Android and not in play mode.
        /// </summary>
        private static bool Initialize()
        {
            Google.RunOnMainThread.OnUpdate -= PollBundleId;
            Google.RunOnMainThread.OnUpdate += PollBundleId;
            return false;
        }

        /// <summary>
        /// Polls both Android and iOS bundle IDs for changes.
        /// When a change is detected, fetches app info on a background thread.
        /// </summary>
        private static void PollBundleId()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                return;
            }

            androidBundleIdPoller.Poll(Yodo1AdUtils.GetAndroidBundleId, (previousValue, currentValue) =>
            {
                Yodo1AdSettingsSync.FetchAndApplyAsync("android", currentValue);
            });

            iosBundleIdPoller.Poll(Yodo1AdUtils.GetIOSBundleId, (previousValue, currentValue) =>
            {
                Yodo1AdSettingsSync.FetchAndApplyAsync("iOS", currentValue);
            });
        }

        /// <summary>
        /// Called on every domain reload (Unity start, script recompilation).
        /// Uses SessionState to ensure the heavy network operations only run
        /// once per editor session, avoiding editor stutter on every recompile.
        /// SessionState resets automatically when Unity restarts.
        /// </summary>
        [InitializeOnLoadMethod]
        static void OnProjectLoadedInEditor()
        {
            // Only run once per editor session to avoid re-triggering on every script recompile
            const string sessionKey = "Yodo1MAS_ProjectLoaded";
            if (SessionState.GetBool(sessionKey, false))
            {
                return;
            }
            SessionState.SetBool(sessionKey, true);

            // Defer until domain reload completes — calling AssetDatabase
            // or synchronous network operations during reload can cause issues.
            // delayCall fires once on the next editor update and auto-removes.
            EditorApplication.delayCall += () =>
            {
                Yodo1AdSettingsSync.RefreshAllAsync();
                IntegrationManager.UpdateAdNetworkAndDependencies();
            };
        }
    }
}

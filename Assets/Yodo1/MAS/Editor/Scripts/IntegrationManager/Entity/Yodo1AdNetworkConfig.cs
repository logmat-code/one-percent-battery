using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yodo1.MAS
{
    /// <summary>
    /// One editor-only block rule in <see cref="Yodo1AdNetworkConfig.editorBlockRules"/> (remote / CI JSON).
    /// </summary>
    [Serializable]
    public class Yodo1EditorBlockRulePayload
    {
        /// <summary>Mediation network id; same convention as <see cref="Yodo1AdNetwork.name"/>.</summary>
        public string networkName;

        /// <summary>
        /// Targets: <c>"ios"</c>, <c>"android"</c> (Android standard mediation), or both (e.g. two entries).
        /// </summary>
        public string[] platforms;

        /// <summary>
        /// When null/empty, the block always applies on the selected platforms (any Unity version).
        /// When set, the block applies only if the editor version is &gt;= this string (see <c>Yodo1AdNetworkUtil.IsUnityEditorVersionAtLeast</c>).
        /// </summary>
        public string blockFromUnityVersion;

        /// <summary>
        /// Human-readable explanation when set: N/A tooltip and removal dialog suffix. If omitted or empty, N/A uses a generic Integration Manager tooltip, removal dialog lists the network without a suffix; Unity logs a warning when rules are loaded.
        /// </summary>
        public string blockReason;
    }

    [Serializable]
    public class Yodo1AdNetworkConfig : ISerializationCallbackReceiver
    {

        public string sdkVersion;
        public string latestSdkversion;
        public string sdkDownloadUrl;
        public Yodo1AdNetwork[] android;
        public Yodo1AdNetwork[] ios;
        public List<Yodo1AdNetwork> androidStandard;
        public List<Yodo1AdNetwork> iosStandard;

        /// <summary>
        /// Editor-only block rules from version-mapping / CI (<c>editor-block-rules.json</c>). Sole source in the plugin; omitted or empty means no blocks.
        /// </summary>
        public Yodo1EditorBlockRulePayload[] editorBlockRules;

        /// <summary>
        /// Historical <see cref="Yodo1AdNetwork.supported"/> discriminator for Android Family in remote payloads; merged into <see cref="androidStandard"/> in the editor.
        /// </summary>
        public const int LegacyAndroidFamilySupportedId = 1;

        public Yodo1AdNetworkConfig()
        {
        }

        private void FilterNetworksBySdkGroup()
        {
            if(android != null && android.Length > 0)
            {
                androidStandard = new List<Yodo1AdNetwork>();
                foreach (Yodo1AdNetwork network in android)
                {
                    if(network == null || network.supported == null || network.supported.Length == 0)
                    {
                        return;
                    }

                    foreach(int temp in network.supported)
                    {
                        if (temp == (int)SdkGroupType.AndroidStandard || temp == LegacyAndroidFamilySupportedId)
                        {
                            if (!androidStandard.Contains(network))
                            {
                                androidStandard.Add(network);
                            }
                        }
                    }
                }
            }

            if(ios != null && ios.Length > 0)
            {
                iosStandard = new List<Yodo1AdNetwork>();
                foreach (Yodo1AdNetwork network in ios)
                {
                    if (network == null || network.supported == null || network.supported.Length == 0)
                    {
                        return;
                    }

                    foreach (int temp in network.supported)
                    {
                        if ((int)SdkGroupType.IosStandard == temp)
                        {
                            iosStandard.Add(network);
                        }
                    }
                }
            }
        }

        public void OnAfterDeserialize()
        {
            FilterNetworksBySdkGroup();
        }

        public void OnBeforeSerialize()
        {
        }
    }

    /// <summary>
    /// Identifies which MAS mediation SDK grouping a network or editor selection belongs to.
    /// Underlying values are persisted in <see cref="Yodo1AdDynamicNetworkSettings.sdkType"/> and in remote <c>supported</c> arrays.
    /// </summary>
    public enum SdkGroupType : int
    {
        /// <summary>Android standard mediation stack (persisted as <c>0</c>).</summary>
        AndroidStandard = 0,

        /// <summary>iOS standard mediation stack (persisted as <c>2</c>).</summary>
        IosStandard = 2,
    }
}

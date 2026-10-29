using System;
using System.Collections.Generic;

namespace Yodo1.MAS
{
    /// <summary>
    /// In-memory snapshot of the developer's selected mediation networks for one platform (Android or iOS) in the editor.
    /// </summary>
    public class Yodo1AdNetworkConfigCacheData
    {
        /// <summary>
        /// Which MAS SDK group this selection applies to; distinct from the persisted int field name on <see cref="Yodo1AdDynamicNetworkSettings"/>.
        /// </summary>
        public SdkGroupType sdkGroupType;

        public string sdkVersion;
        public string latestSdkVersion;
        public List<string> networks;

        public Yodo1AdNetworkConfigCacheData()
        {
        }

        public Yodo1AdNetworkConfigCacheData(SdkGroupType sdkGroupType, string sdkVersion, string latestSdkVersion, List<string> networks)
        {
            this.sdkGroupType = sdkGroupType;
            this.sdkVersion = sdkVersion;
            this.latestSdkVersion = latestSdkVersion;
            this.networks = networks;
        }
    }
}

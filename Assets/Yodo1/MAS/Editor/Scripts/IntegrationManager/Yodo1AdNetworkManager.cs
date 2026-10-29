using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;
using Yodo1.MAS;

public class Yodo1AdNetworkManager
{
    private static readonly string TARGET_PATH = Path.GetFullPath(".") + "/Assets/Yodo1/MAS/Editor/Dependencies/";
    private static readonly string VERSION_PATH = Path.GetFullPath(".") + "/Assets/Yodo1/MAS/version.xml";
    private static readonly string IOS_MIN_TARGET_SDK = "13.0";

    public Yodo1AdNetworkConfig yodo1AdNetworkConfig;

    private static readonly Yodo1AdNetworkManager instance = new Yodo1AdNetworkManager();

    public static Yodo1AdNetworkManager GetInstance()
    {
        return instance;
    }

    public void InitAdNetworkConfig()
    {
        string curSDKVersion = Yodo1AdNetworkUtil.GetCurSdkVersion(VERSION_PATH);
        yodo1AdNetworkConfig = FetchRemoteConfig(curSDKVersion);
        ApplyEditorAvailability();
    }

    public Yodo1AdNetworkConfig GetAdNetworkConfig()
    {
        return yodo1AdNetworkConfig;
    }

    public string GetCurMakSdkVersion()
    {
        return Yodo1AdNetworkUtil.GetCurSdkVersion(VERSION_PATH);
    }

    #region Cached Network Info

    public Yodo1AdNetworkConfigCacheData GetCachedAndroidNetworks()
    {
        return GetCachedAdNetworksInfo(settings => settings.androidDynamicNetworkSettings);
    }

    public Yodo1AdNetworkConfigCacheData GetCachedIosNetworks()
    {
        return GetCachedAdNetworksInfo(settings => settings.iosDynamicNetworkSettings);
    }

    private Yodo1AdNetworkConfigCacheData GetCachedAdNetworksInfo(Func<Yodo1AdSettings, Yodo1AdDynamicNetworkSettings> selector)
    {
        Yodo1AdSettings settings = Yodo1AdSettingsSave.Load();
        var source = selector(settings);
        SdkGroupType sdkGroupType = NormalizeSdkGroupType(source.sdkType);
        return new Yodo1AdNetworkConfigCacheData(
            sdkGroupType,
            source.sdkVersion,
            source.latestSdkVersion,
            source.networks
        );
    }

    /// <summary>
    /// Maps persisted <see cref="Yodo1AdDynamicNetworkSettings.sdkType"/> to <see cref="SdkGroupType"/>.
    /// Legacy value <c>1</c> (removed Android Family) is treated as <see cref="SdkGroupType.AndroidStandard"/>.
    /// </summary>
    private static SdkGroupType NormalizeSdkGroupType(int raw)
    {
        if (raw == (int)SdkGroupType.IosStandard) return SdkGroupType.IosStandard;
        if (raw == Yodo1AdNetworkConfig.LegacyAndroidFamilySupportedId) return SdkGroupType.AndroidStandard;
        return SdkGroupType.AndroidStandard;
    }

    public void UpdateAdNetworksInfo(Yodo1AdNetworkConfigCacheData data)
    {
        if (data == null || data.networks == null || data.networks.Count == 0)
        {
            Debug.LogError(Yodo1U3dMas.TAG + ": need selected networks!");
            return;
        }
        UpdateLocalCache(data);
        UpdateDependenciesFile(data.sdkGroupType, data.networks);
    }

    private void UpdateLocalCache(Yodo1AdNetworkConfigCacheData data)
    {
        Yodo1AdSettings settings = Yodo1AdSettingsSave.Load();

        Yodo1AdDynamicNetworkSettings target;
        if (data.sdkGroupType == SdkGroupType.AndroidStandard)
        {
            target = settings.androidDynamicNetworkSettings;
        }
        else if (data.sdkGroupType == SdkGroupType.IosStandard)
        {
            target = settings.iosDynamicNetworkSettings;
        }
        else
        {
            return;
        }

        target.sdkType = (int)data.sdkGroupType;
        target.sdkVersion = data.sdkVersion;
        target.latestSdkVersion = data.latestSdkVersion;
        target.networks = data.networks;

        Yodo1AdSettingsSave.Save(settings);
    }

    #endregion

    #region Check & Merge New Networks

    public void SyncDependenciesWithCache()
    {
        Yodo1AdNetworkConfigCacheData androidData = GetCachedAndroidNetworks();
        Yodo1AdNetworkConfigCacheData iosData = GetCachedIosNetworks();

        string sdkVersion = androidData?.sdkVersion;
        if (string.IsNullOrEmpty(sdkVersion))
        {
            sdkVersion = iosData?.sdkVersion;
        }

        if (!string.IsNullOrEmpty(sdkVersion))
        {
            Yodo1AdNetworkConfig oldConfig = FetchRemoteConfig(sdkVersion);
            if (oldConfig == null || yodo1AdNetworkConfig == null)
            {
                return;
            }
            MergeNewNetworks(androidData, oldConfig.android, yodo1AdNetworkConfig.android);
            MergeNewNetworks(iosData, oldConfig.ios, yodo1AdNetworkConfig.ios);
        }

        if (androidData?.networks != null && androidData.networks.Count > 0)
        {
            StripBlockedNetworks(androidData.networks, SdkGroupType.AndroidStandard);
            UpdateAndroidDeps(androidData.networks);
        }
        if (iosData?.networks != null && iosData.networks.Count > 0)
        {
            StripBlockedNetworks(iosData.networks, SdkGroupType.IosStandard);
            UpdateIosDeps(iosData.networks);
        }
    }

    private void MergeNewNetworks(
        Yodo1AdNetworkConfigCacheData cachedData,
        Yodo1AdNetwork[] oldNetworks,
        Yodo1AdNetwork[] currentNetworks)
    {
        if (string.IsNullOrEmpty(cachedData?.sdkVersion)
            || oldNetworks == null || oldNetworks.Length == 0
            || currentNetworks == null || currentNetworks.Length == 0)
        {
            return;
        }

        var oldNames = new HashSet<string>(GetNetworkNames(oldNetworks));
        var currentNames = new HashSet<string>(GetNetworkNames(currentNetworks));
        currentNames.ExceptWith(oldNames);
        currentNames.ExceptWith(cachedData.networks);

        if (currentNames.Count == 0) return;

        var currentLookup = currentNetworks.Where(n => !string.IsNullOrEmpty(n.name))
            .ToDictionary(n => n.name);

        foreach (string name in currentNames)
        {
            if (ShouldOmitNetwork(name, cachedData.sdkGroupType))
            {
                continue;
            }
            if (currentLookup.TryGetValue(name, out var network) && network.status != 1)
            {
                cachedData.networks.Add(name);
            }
        }

        UpdateAdNetworksInfo(cachedData);
    }

    private static List<string> GetNetworkNames(Yodo1AdNetwork[] networks)
    {
        if (networks == null) return new List<string>();
        return networks
            .Where(n => !string.IsNullOrEmpty(n.name))
            .Select(n => n.name)
            .ToList();
    }

    #endregion

    #region Dependency File Generation

    private void UpdateDependenciesFile(SdkGroupType sdkGroupType, List<string> networks)
    {
        if (sdkGroupType == SdkGroupType.AndroidStandard)
        {
            UpdateAndroidDeps(networks);
        }
        else if (sdkGroupType == SdkGroupType.IosStandard)
        {
            UpdateIosDeps(networks);
        }
    }

    private struct VersionInfo
    {
        public string Env;
        public string Version;
    }

    private VersionInfo ReadPlatformVersion(string platformNodeName)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(VERSION_PATH);
        XmlElement node = (XmlElement)doc.SelectSingleNode("versions/" + platformNodeName);
        return new VersionInfo
        {
            Env = node.GetAttribute("env"),
            Version = node.GetAttribute("version")
        };
    }

    private struct MediationFlags
    {
        public bool Applovin;
        public bool Admob;
        public bool IronSource;
        public bool Tobid;
        public bool Topon;
    }

    private static MediationFlags DetectMediations(List<string> networkNames, bool isIOS)
    {
        var set = new HashSet<string>(networkNames);
        return new MediationFlags
        {
            Applovin = set.Contains("APPLOVIN"),
            Admob = set.Contains("ADMOB"),
            IronSource = set.Contains("IRONSOURCE"),
            Tobid = isIOS && set.Contains("TOBID"),
            Topon = set.Contains("TOPON_HYPERBID") || set.Contains("TOPON")
        };
    }

    private static void CollectDependencies(
        List<string> dependencyList,
        Yodo1AdNetwork network,
        MediationFlags flags)
    {
        dependencyList.Add(network.dependency);

        if (flags.Admob && !string.IsNullOrEmpty(network.admobAdapterDependency))
            dependencyList.Add(network.admobAdapterDependency);
        if (flags.Applovin && !string.IsNullOrEmpty(network.applovinAdapterDependency))
            dependencyList.Add(network.applovinAdapterDependency);
        if (flags.IronSource && !string.IsNullOrEmpty(network.ironsourceAdapterDependency))
            dependencyList.Add(network.ironsourceAdapterDependency);
        if (flags.Tobid && !string.IsNullOrEmpty(network.tobidAdapterDependency))
            dependencyList.Add(network.tobidAdapterDependency);
        if (flags.Topon && !string.IsNullOrEmpty(network.toponAdapterDependency))
            dependencyList.Add(network.toponAdapterDependency);
    }

    private static void DeleteFileWithMeta(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            string metaPath = filePath + ".meta";
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }
        }
    }

    private void UpdateAndroidDeps(List<string> networkNames)
    {
        if (yodo1AdNetworkConfig?.android == null || yodo1AdNetworkConfig.android.Length == 0)
        {
            Debug.LogError(Yodo1U3dMas.TAG + ": updateAndroidDependenciesFile yodo1AdNetworkConfig is null or android data is null ");
            return;
        }

        string destFile = TARGET_PATH + "Yodo1MasAndroidDependencies.xml";
        DeleteFileWithMeta(destFile);

        List<Yodo1AdNetwork> yodo1AdNetworks = yodo1AdNetworkConfig.androidStandard;

        VersionInfo versionInfo = ReadPlatformVersion("android");
        string version = versionInfo.Version;
        if (!versionInfo.Env.Equals("Release"))
        {
            version += "-SNAPSHOT";
        }

        var networkLookup = yodo1AdNetworks.Where(n => !string.IsNullOrEmpty(n.name))
            .ToDictionary(n => n.name);

        MediationFlags flags = DetectMediations(networkNames, isIOS: false);
        List<string> repoUrlList = new List<string>();
        List<string> dependencyList = new List<string>();

        foreach (string name in networkNames)
        {
            if (ShouldOmitNetwork(name, SdkGroupType.AndroidStandard)) continue;
            if (!networkLookup.TryGetValue(name, out var network)) continue;

            if (!string.IsNullOrEmpty(network.repoUrl))
            {
                repoUrlList.Add(network.repoUrl);
            }

            CollectDependencies(dependencyList, network, flags);

            if (flags.Applovin && !string.IsNullOrEmpty(network.admanagerAdapterDependency))
            {
                dependencyList.Add(network.admanagerAdapterDependency);
            }
        }

        XmlDocument xmlDoc = new XmlDocument();
        XmlDeclaration declaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
        XmlNode dependencies = xmlDoc.CreateElement("dependencies");

        XmlNode androidPackages = xmlDoc.CreateElement("androidPackages");
        dependencies.AppendChild(androidPackages);

        XmlNode repositories = xmlDoc.CreateElement("repositories");
        androidPackages.AppendChild(repositories);

        foreach (string repoUrl in repoUrlList)
        {
            XmlNode repoNode = xmlDoc.CreateElement("repository");
            repoNode.InnerText = repoUrl;
            repositories.AppendChild(repoNode);
        }

        XmlNode appHarbrMaven = xmlDoc.CreateElement("repository");
        appHarbrMaven.InnerText = "https://jitpack.io";
        repositories.AppendChild(appHarbrMaven);

        if (!versionInfo.Env.Equals("Release"))
        {
            XmlNode maven = xmlDoc.CreateElement("repository");
            maven.InnerText = "https://oss.sonatype.org/content/repositories/snapshots/";
            repositories.AppendChild(maven);
            XmlNode maven2 = xmlDoc.CreateElement("repository");
            maven2.InnerText = "https://central.sonatype.com/repository/maven-snapshots/";
            repositories.AppendChild(maven2);
        }

        if (dependencyList.Count > 0)
        {
            dependencyList.Insert(0, "com.yodo1.mas:gplibrary");
            dependencyList.Insert(0, "com.yodo1.mas:core");
            dependencyList.Add("com.yodo1.mas.mediation:yodo1");
        }

        foreach (string dependency in dependencyList)
        {
            XmlNode packageNode = xmlDoc.CreateElement("androidPackage");
            androidPackages.AppendChild(packageNode);
            XmlAttribute spec = xmlDoc.CreateAttribute("spec");
            spec.Value = dependency + ":" + version;
            packageNode.Attributes.Append(spec);
        }

        xmlDoc.AppendChild(dependencies);
        xmlDoc.InsertBefore(declaration, xmlDoc.DocumentElement);
        xmlDoc.Save(destFile);

        AssetDatabase.Refresh();
    }

    private void UpdateIosDeps(List<string> networkNames)
    {
        if (yodo1AdNetworkConfig?.ios == null || yodo1AdNetworkConfig.ios.Length == 0)
        {
            Debug.LogError(Yodo1U3dMas.TAG + ": updateIOSDependenciesFile yodo1AdNetworkConfig is null or ios data is null ");
            return;
        }

        string destFile = TARGET_PATH + "Yodo1MasiOSDependencies.xml";
        DeleteFileWithMeta(destFile);

        List<Yodo1AdNetwork> yodo1AdNetworks = yodo1AdNetworkConfig.iosStandard;

        VersionInfo versionInfo = ReadPlatformVersion("ios");
        string version = versionInfo.Version;

        var networkLookup = yodo1AdNetworks.Where(n => !string.IsNullOrEmpty(n.name))
            .ToDictionary(n => n.name);

        MediationFlags flags = DetectMediations(networkNames, isIOS: true);
        List<string> dependencyList = new List<string>();

        foreach (string name in networkNames)
        {
            if (ShouldOmitNetwork(name, SdkGroupType.IosStandard))
            {
                continue;
            }
            if (!networkLookup.TryGetValue(name, out var network)) continue;
            CollectDependencies(dependencyList, network, flags);
        }

        XmlDocument xmlDoc = new XmlDocument();
        XmlDeclaration declaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
        XmlNode dependencies = xmlDoc.CreateElement("dependencies");

        XmlNode iosPods = xmlDoc.CreateElement("iosPods");
        dependencies.AppendChild(iosPods);

        XmlNode sources = xmlDoc.CreateElement("sources");
        iosPods.AppendChild(sources);

        XmlNode masSource = xmlDoc.CreateElement("source");
        if (versionInfo.Env.Equals("Dev"))
        {
            masSource.InnerText = "https://github.com/Yodo1Games/MAS-Spec-Dev.git";
        }
        else if (versionInfo.Env.Equals("Pre"))
        {
            masSource.InnerText = "https://github.com/Yodo1Games/MAS-Spec-Pre.git";
        }
        else
        {
            masSource.InnerText = "https://github.com/Yodo1Games/MAS-Spec.git";
        }
        sources.AppendChild(masSource);

        XmlNode cocoaPodsSource = xmlDoc.CreateElement("source");
        cocoaPodsSource.InnerText = "https://github.com/CocoaPods/Specs.git";
        sources.AppendChild(cocoaPodsSource);

        foreach (string dependency in dependencyList)
        {
            AppendIosPod(xmlDoc, iosPods, dependency, version);
        }
        AppendIosPod(xmlDoc, iosPods, "Yodo1MasMediationYodo1", version);

        xmlDoc.AppendChild(dependencies);
        xmlDoc.InsertBefore(declaration, xmlDoc.DocumentElement);
        xmlDoc.Save(destFile);

        AssetDatabase.Refresh();
    }

    private void AppendIosPod(XmlDocument doc, XmlNode parent, string podName, string version)
    {
        XmlNode pod = doc.CreateElement("iosPod");
        parent.AppendChild(pod);

        XmlAttribute nameAttr = doc.CreateAttribute("name");
        nameAttr.Value = podName;
        pod.Attributes.Append(nameAttr);

        XmlAttribute versionAttr = doc.CreateAttribute("version");
        versionAttr.Value = version;
        pod.Attributes.Append(versionAttr);

        XmlAttribute minTargetAttr = doc.CreateAttribute("minTargetSdk");
        minTargetAttr.Value = IOS_MIN_TARGET_SDK;
        pod.Attributes.Append(minTargetAttr);
    }

    #endregion

    #region Remote configuration and editor availability

    /// <summary>
    /// Removes networks that are unavailable in the current editor from saved Android/iOS selections, persists, and queues user notice if needed.
    /// </summary>
    private void ApplyEditorAvailability()
    {
        Yodo1AdSettings settings = Yodo1AdSettingsSave.Load();
        if (settings == null) return;
        bool dirty = false;

        if (settings.androidDynamicNetworkSettings.sdkType == Yodo1AdNetworkConfig.LegacyAndroidFamilySupportedId)
        {
            settings.androidDynamicNetworkSettings.sdkType = (int)SdkGroupType.AndroidStandard;
            dirty = true;
        }

        List<string> iosRemoved = Yodo1AdNetworkEditorAvailability.RemoveBlockedNetworks(
            settings.iosDynamicNetworkSettings.networks,
            SdkGroupType.IosStandard);
        if (iosRemoved.Count > 0) dirty = true;

        SdkGroupType androidGroup = NormalizeSdkGroupType(settings.androidDynamicNetworkSettings.sdkType);
        List<string> androidRemoved = Yodo1AdNetworkEditorAvailability.RemoveBlockedNetworks(
            settings.androidDynamicNetworkSettings.networks,
            androidGroup);
        if (androidRemoved.Count > 0) dirty = true;

        if (dirty)
        {
            Yodo1AdSettingsSave.Save(settings);
        }

        if (iosRemoved.Count > 0)
        {
            Yodo1AdNetworkEditorAvailability.NotifyNetworksRemoved(iosRemoved, SdkGroupType.IosStandard);
        }
        if (androidRemoved.Count > 0)
        {
            Yodo1AdNetworkEditorAvailability.NotifyNetworksRemoved(androidRemoved, androidGroup);
        }
    }

    /// <summary>
    /// Drops editor-unavailable network ids from an in-memory cached selection list, saves settings when anything was removed, and notifies.
    /// </summary>
    private static void StripBlockedNetworks(List<string> networkNames, SdkGroupType sdkGroupType)
    {
        if (networkNames == null || networkNames.Count == 0) return;
        List<string> removed = Yodo1AdNetworkEditorAvailability.RemoveBlockedNetworks(
            networkNames, sdkGroupType);
        if (removed.Count == 0) return;
        Yodo1AdSettingsSave.Save(Yodo1AdSettingsSave.Load());
        Yodo1AdNetworkEditorAvailability.NotifyNetworksRemoved(removed, sdkGroupType);
    }

    private static bool ShouldOmitNetwork(string networkName, SdkGroupType sdkGroupType)
    {
        return Yodo1AdNetworkEditorAvailability.IsNetworkBlockedInEditor(networkName, sdkGroupType);
    }

    private static Yodo1AdNetworkConfig FetchRemoteConfig(string curSDKVersion)
    {
        bool isOfficialVersion = !System.Text.RegularExpressions.Regex.IsMatch(curSDKVersion, @"[A-Za-z]");
        string url = isOfficialVersion
            ? "https://sdk-mas.yodo1.com/v1/unity/version-mapping"
            : "https://sdk-mas.yodo1.me/v1/unity/version-mapping";

        var body = new Dictionary<string, string> { { "version", curSDKVersion } };

        try
        {
            string requestBody = Yodo1JSON.Serialize(body);
            //Debug.Log(Yodo1U3dMas.TAG + "Fetching ad network config: " + url + " body: " + requestBody);
            string response = Yodo1Net.HttpPost(url, requestBody);
            //Debug.Log(Yodo1U3dMas.TAG + "Ad network config response: " + (response != null ? response.Substring(0, Mathf.Min(response.Length, 500)) : "null"));
            Yodo1AdNetworkConfig config = JsonUtility.FromJson<Yodo1AdNetworkConfig>(response);
            if (config != null)
            {
                config.OnAfterDeserialize();
                Yodo1AdNetworkEditorAvailability.RebuildMergedBlockRules(config);
                Yodo1AdNetworkEditorAvailability.ApplyHiddenStatus(config);
            }
            else
            {
                Yodo1AdNetworkEditorAvailability.RebuildMergedBlockRules(null);
            }
            return config;
        }
        catch (Exception e)
        {
            Debug.LogWarning(Yodo1U3dMas.TAG + "Failed to fetch ad network config: " + e.Message);
            Yodo1AdNetworkEditorAvailability.RebuildMergedBlockRules(null);
            return null;
        }
    }

    #endregion
}

/// <summary>
/// Central place for <strong>editor-side availability</strong> of mediation networks: which adapters are treated as
/// unavailable in the current Unity environment, and helpers to strip invalid selections and mark metadata accordingly.
/// Declarative rules inside this type determine that availability; the public API surface uses “blocked / unavailable” wording.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Layering:</strong> <see cref="IntegrationManager"/> is the UI surface;
/// <see cref="Yodo1AdNetworkManager"/> orchestrates config fetch, caches, and dependency files;
/// this type holds only which networks are unavailable in the editor and user notification when saved selections change.
/// </para>
/// <para>
/// Co-located in <c>Yodo1AdNetworkManager.cs</c> because <see cref="Yodo1AdNetworkManager"/> is the primary caller; kept as a
/// separate static type to preserve a clear boundary between orchestration and availability rules.
/// </para>
/// <para>
/// Which networks are blocked depends on rules in <see cref="Yodo1AdNetworkConfig.editorBlockRules"/> from the
/// version-mapping response (populated via CI <c>editor-block-rules.json</c> and OSS). There are no hard-coded rules in the plugin;
/// if the config is missing or the array is empty, nothing is blocked in the editor.
/// </para>
/// <para>
/// Typical reasons products gate networks in the editor include: Unity version or LTS compatibility, required
/// Unity modules or host OS, minimum Xcode or Android SDK levels, scripting backend constraints, or policy-driven
/// feature flags. Not all of these are implemented here.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class Yodo1AdNetworkEditorAvailability
{
    [Flags]
    private enum EditorAvailabilityPlatform
    {
        None = 0,
        Ios = 1,
        AndroidStandard = 2,
    }

    private const EditorAvailabilityPlatform ValidRemotePlatformMask =
        EditorAvailabilityPlatform.Ios | EditorAvailabilityPlatform.AndroidStandard;

    private readonly struct EditorNetworkBlockRule
    {
        public readonly string NetworkName;
        public readonly EditorAvailabilityPlatform Platforms;

        /// <summary>
        /// When empty, the rule always applies (for matching platforms). When set, applies only if editor version &gt;= this
        /// (see <see cref="Yodo1AdNetworkUtil.IsUnityEditorVersionAtLeast"/>); not a product min-Unity requirement.
        /// </summary>
        public readonly string BlockFromUnityVersion;

        /// <summary>Optional payload copy; when empty, removal dialog shows the bullet only and N/A uses Integration Manager default tooltip.</summary>
        public readonly string BlockReason;

        public EditorNetworkBlockRule(string networkName, EditorAvailabilityPlatform platforms, string blockFromUnityVersion, string blockReason)
        {
            NetworkName = networkName;
            Platforms = platforms;
            BlockFromUnityVersion = blockFromUnityVersion;
            BlockReason = blockReason ?? string.Empty;
        }
    }

    private static EditorNetworkBlockRule[] s_effectiveRules;

    private static bool s_removalDialogQueued;

    private sealed class PendingRemoval
    {
        public string Key;
        public string NetworkName;
        public string PlatformLabel;
        public SdkGroupType SdkGroup;
    }

    private static readonly List<PendingRemoval> s_pendingRemovals = new List<PendingRemoval>();

    private static bool RuleAppliesToSdkGroup(EditorNetworkBlockRule rule, SdkGroupType sdkGroup)
    {
        switch (sdkGroup)
        {
            case SdkGroupType.IosStandard:
                return rule.Platforms.HasFlag(EditorAvailabilityPlatform.Ios);
            case SdkGroupType.AndroidStandard:
                return rule.Platforms.HasFlag(EditorAvailabilityPlatform.AndroidStandard);
            default:
                return false;
        }
    }

    private static bool NameMatches(string ruleName, string candidate)
    {
        return !string.IsNullOrEmpty(candidate)
               && !string.IsNullOrEmpty(ruleName)
               && candidate.Equals(ruleName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRuleActiveForCurrentEditor(EditorNetworkBlockRule rule)
    {
        if (string.IsNullOrEmpty(rule.BlockFromUnityVersion))
        {
            return true;
        }
        return Yodo1AdNetworkUtil.IsUnityEditorVersionAtLeast(rule.BlockFromUnityVersion);
    }

    private static bool TryParseRemoteBlockRule(Yodo1EditorBlockRulePayload payload, out EditorNetworkBlockRule rule)
    {
        rule = default;
        if (payload == null) return false;
        string name = payload.networkName != null ? payload.networkName.Trim() : null;
        if (string.IsNullOrEmpty(name)) return false;
        EditorAvailabilityPlatform platforms = EditorAvailabilityPlatform.None;
        if (payload.platforms == null || payload.platforms.Length == 0) return false;
        for (int i = 0; i < payload.platforms.Length; i++)
        {
            string token = payload.platforms[i];
            if (string.IsNullOrEmpty(token)) continue;
            switch (token.Trim().ToLowerInvariant())
            {
                case "ios":
                    platforms |= EditorAvailabilityPlatform.Ios;
                    break;
                case "android":
                    platforms |= EditorAvailabilityPlatform.AndroidStandard;
                    break;
                default:
                    Debug.LogWarning(Yodo1U3dMas.TAG + "editorBlockRules: unknown platform \"" + token + "\" for network \"" + name + "\"");
                    break;
            }
        }
        if (platforms == EditorAvailabilityPlatform.None) return false;
        if ((platforms & ~ValidRemotePlatformMask) != 0) return false;
        string versionGate = payload.blockFromUnityVersion != null ? payload.blockFromUnityVersion.Trim() : string.Empty;
        string reason = payload.blockReason != null ? payload.blockReason.Trim() : string.Empty;
        if (string.IsNullOrEmpty(reason))
        {
            Debug.LogWarning(Yodo1U3dMas.TAG + "editorBlockRules: \"blockReason\" is empty for network \"" + name
                                              + "\". Add a clear user-facing explanation in the rule payload.");
        }

        rule = new EditorNetworkBlockRule(name, platforms, versionGate, reason);
        return true;
    }

    /// <summary>
    /// When the network is blocked for <paramref name="sdkGroup"/> in this editor, sets <paramref name="reason"/> to the configured <c>blockReason</c> when present; otherwise <c>null</c> (Integration Manager uses a generic tooltip).
    /// </summary>
    public static bool TryGetBlockReason(string networkName, SdkGroupType sdkGroup, out string reason)
    {
        reason = null;
        if (string.IsNullOrEmpty(networkName)) return false;
        if (s_effectiveRules == null)
        {
            RebuildMergedBlockRules(Yodo1AdNetworkManager.GetInstance().GetAdNetworkConfig());
        }

        foreach (EditorNetworkBlockRule rule in s_effectiveRules)
        {
            if (!RuleAppliesToSdkGroup(rule, sdkGroup)) continue;
            if (!IsRuleActiveForCurrentEditor(rule)) continue;
            if (!NameMatches(rule.NetworkName, networkName)) continue;
            string trimmed = rule.BlockReason != null ? rule.BlockReason.Trim() : string.Empty;
            reason = string.IsNullOrEmpty(trimmed) ? null : trimmed;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removal-dialog only: delegates to <see cref="TryGetBlockReason"/> so dialog suffix copy and IM/tooltips stay one implementation.
    /// </summary>
    private static bool TryGetPayloadRemovalReason(
        string networkName,
        SdkGroupType sdkGroup,
        out string configuredReasonOrNull)
    {
        return TryGetBlockReason(networkName, sdkGroup, out configuredReasonOrNull);
    }

    /// <summary>
    /// Same platform rules as <see cref="IsBlockedInUi"/>; use for UI copy next to N/A.
    /// </summary>
    public static bool TryGetBlockReasonForUi(bool isIosTab, Yodo1AdNetwork adNetwork, out string reason)
    {
        reason = null;
        if (adNetwork == null || string.IsNullOrEmpty(adNetwork.name)) return false;
        if (isIosTab)
        {
            return TryGetBlockReason(adNetwork.name, SdkGroupType.IosStandard, out reason);
        }

        if (!NetworkTargetsAndroidTab(adNetwork)) return false;
        return TryGetBlockReason(adNetwork.name, SdkGroupType.AndroidStandard, out reason);
    }

    /// <summary>
    /// Rebuilds the effective editor block list from <see cref="Yodo1AdNetworkConfig.editorBlockRules"/> only.
    /// Pass null or an empty/missing array after fetch failure to clear rules (nothing blocked).
    /// </summary>
    public static void RebuildMergedBlockRules(Yodo1AdNetworkConfig config)
    {
        if (config?.editorBlockRules == null || config.editorBlockRules.Length == 0)
        {
            s_effectiveRules = new EditorNetworkBlockRule[0];
            return;
        }
        var list = new List<EditorNetworkBlockRule>(config.editorBlockRules.Length);
        for (int i = 0; i < config.editorBlockRules.Length; i++)
        {
            if (TryParseRemoteBlockRule(config.editorBlockRules[i], out EditorNetworkBlockRule remoteRule))
            {
                list.Add(remoteRule);
            }
        }
        s_effectiveRules = list.ToArray();
    }

    /// <summary>
    /// Returns whether the named network must not be used for the given MAS SDK group in this editor session.
    /// </summary>
    /// <param name="networkName">Mediation network identifier (same casing convention as MAS configuration).</param>
    /// <param name="sdkGroup">Android Standard or iOS Standard.</param>
    public static bool IsNetworkBlockedInEditor(string networkName, SdkGroupType sdkGroup)
    {
        if (string.IsNullOrEmpty(networkName)) return false;
        if (s_effectiveRules == null)
        {
            RebuildMergedBlockRules(Yodo1AdNetworkManager.GetInstance().GetAdNetworkConfig());
        }
        foreach (EditorNetworkBlockRule rule in s_effectiveRules)
        {
            if (!RuleAppliesToSdkGroup(rule, sdkGroup)) continue;
            if (!IsRuleActiveForCurrentEditor(rule)) continue;
            if (NameMatches(rule.NetworkName, networkName)) return true;
        }
        return false;
    }

    /// <summary>
    /// Whether Integration Manager should treat the network as not installable on the current tab (Android vs iOS),
    /// respecting <see cref="Yodo1AdNetwork.supported"/>.
    /// </summary>
    public static bool IsBlockedInUi(bool isIosTab, Yodo1AdNetwork adNetwork)
    {
        if (adNetwork == null || string.IsNullOrEmpty(adNetwork.name)) return false;
        if (isIosTab)
        {
            return IsNetworkBlockedInEditor(adNetwork.name, SdkGroupType.IosStandard);
        }
        if (!NetworkTargetsAndroidTab(adNetwork)) return false;
        return IsNetworkBlockedInEditor(adNetwork.name, SdkGroupType.AndroidStandard);
    }

    /// <summary>
    /// True if remote <see cref="Yodo1AdNetwork.supported"/> includes Android Standard or legacy Family id (merged in editor).
    /// </summary>
    private static bool NetworkTargetsAndroidTab(Yodo1AdNetwork adNetwork)
    {
        int[] supported = adNetwork?.supported;
        if (supported == null || supported.Length == 0) return false;
        for (int i = 0; i < supported.Length; i++)
        {
            int s = supported[i];
            if (s == (int)SdkGroupType.AndroidStandard || s == Yodo1AdNetworkConfig.LegacyAndroidFamilySupportedId)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Marks blocked networks as hidden (<see cref="Yodo1AdNetwork.status"/> = 1) on fetched mediation metadata.
    /// </summary>
    public static void ApplyHiddenStatus(Yodo1AdNetworkConfig config)
    {
        if (config?.ios != null)
        {
            foreach (Yodo1AdNetwork n in config.ios)
            {
                if (n != null && IsNetworkBlockedInEditor(n.name, SdkGroupType.IosStandard))
                {
                    n.status = 1;
                }
            }
        }
        if (config?.android == null) return;
        foreach (Yodo1AdNetwork n in config.android)
        {
            if (n == null) continue;
            if (NetworkTargetsAndroidTab(n)
                && IsNetworkBlockedInEditor(n.name, SdkGroupType.AndroidStandard))
            {
                n.status = 1;
            }
        }
    }

    /// <summary>
    /// Removes entries that must not remain selected for this editor environment. The list is modified in place.
    /// </summary>
    /// <returns>Network names that were removed (may be empty).</returns>
    public static List<string> RemoveBlockedNetworks(List<string> networkNames, SdkGroupType sdkGroup)
    {
        var removed = new List<string>();
        if (networkNames == null || networkNames.Count == 0) return removed;
        networkNames.RemoveAll(n =>
        {
            if (string.IsNullOrEmpty(n)) return false;
            if (!IsNetworkBlockedInEditor(n, sdkGroup)) return false;
            removed.Add(n);
            return true;
        });
        return removed;
    }

    private static string GetPlatformLabel(SdkGroupType sdkGroup)
    {
        switch (sdkGroup)
        {
            case SdkGroupType.IosStandard:
                return "iOS";
            case SdkGroupType.AndroidStandard:
                return "Android";
            default:
                return sdkGroup.ToString();
        }
    }

    /// <summary>
    /// Schedules a single deferred editor dialog summarizing networks removed from saved selection (coalesced).
    /// </summary>
    public static void NotifyNetworksRemoved(IReadOnlyList<string> removedNames, SdkGroupType sdkGroup)
    {
        if (removedNames == null || removedNames.Count == 0) return;
        string platformLabel = GetPlatformLabel(sdkGroup);
        foreach (string name in removedNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            string key = name.ToUpperInvariant() + "|" + platformLabel;
            bool exists = false;
            for (int i = 0; i < s_pendingRemovals.Count; i++)
            {
                if (s_pendingRemovals[i].Key == key)
                {
                    exists = true;
                    break;
                }
            }
            if (exists) continue;
            s_pendingRemovals.Add(new PendingRemoval
            {
                Key = key,
                NetworkName = name,
                PlatformLabel = platformLabel,
                SdkGroup = sdkGroup
            });
        }
        ScheduleRemovalDialog();
    }

    private static void ScheduleRemovalDialog()
    {
        if (s_removalDialogQueued || s_pendingRemovals.Count == 0) return;
        s_removalDialogQueued = true;
        EditorApplication.delayCall += ShowRemovalDialog;
    }

    /// <summary>
    /// Human-readable network label for editor UI (Integration Manager rows, removal dialogs): uses
    /// <see cref="Yodo1AdNetwork.displayName"/> when set, otherwise title-cased <see cref="Yodo1AdNetwork.name"/>.
    /// </summary>
    public static string GetNetworkListDisplayName(Yodo1AdNetwork adNetwork)
    {
        if (adNetwork == null) return string.Empty;
        if (!string.IsNullOrEmpty(adNetwork.displayName))
        {
            return adNetwork.displayName;
        }
        string name = adNetwork.name;
        if (string.IsNullOrEmpty(name)) return string.Empty;
        return char.ToUpper(name[0]) + (name.Length > 1 ? name.Substring(1).ToLower() : string.Empty);
    }

    private static string FormatNetworkLabel(string internalName)
    {
        if (string.IsNullOrEmpty(internalName)) return internalName;
        return char.ToUpper(internalName[0])
               + (internalName.Length > 1 ? internalName.Substring(1).ToLower() : string.Empty);
    }

    private static Yodo1AdNetwork FindAdNetworkInConfig(Yodo1AdNetworkConfig config, string internalName, SdkGroupType sdkGroup)
    {
        if (config == null || string.IsNullOrEmpty(internalName)) return null;
        if (sdkGroup == SdkGroupType.IosStandard)
        {
            Yodo1AdNetwork n = FindAdNetworkInList(config.iosStandard, internalName);
            if (n != null) return n;
            return FindAdNetworkInArray(config.ios, internalName);
        }
        if (sdkGroup == SdkGroupType.AndroidStandard)
        {
            Yodo1AdNetwork n = FindAdNetworkInList(config.androidStandard, internalName);
            if (n != null) return n;
            return FindAdNetworkInArray(config.android, internalName);
        }
        return null;
    }

    private static Yodo1AdNetwork FindAdNetworkInList(List<Yodo1AdNetwork> list, string internalName)
    {
        if (list == null) return null;
        for (int i = 0; i < list.Count; i++)
        {
            Yodo1AdNetwork n = list[i];
            if (n != null && !string.IsNullOrEmpty(n.name) && n.name.Equals(internalName, StringComparison.OrdinalIgnoreCase))
            {
                return n;
            }
        }
        return null;
    }

    private static Yodo1AdNetwork FindAdNetworkInArray(Yodo1AdNetwork[] arr, string internalName)
    {
        if (arr == null) return null;
        for (int i = 0; i < arr.Length; i++)
        {
            Yodo1AdNetwork n = arr[i];
            if (n != null && !string.IsNullOrEmpty(n.name) && n.name.Equals(internalName, StringComparison.OrdinalIgnoreCase))
            {
                return n;
            }
        }
        return null;
    }

    private static string ResolveRemovalLabel(string internalName, SdkGroupType sdkGroup)
    {
        Yodo1AdNetworkConfig config = Yodo1AdNetworkManager.GetInstance().GetAdNetworkConfig();
        Yodo1AdNetwork net = FindAdNetworkInConfig(config, internalName, sdkGroup);
        if (net != null)
        {
            string label = GetNetworkListDisplayName(net);
            if (!string.IsNullOrEmpty(label)) return label;
        }
        return FormatNetworkLabel(internalName);
    }

    /// <summary>
    /// macOS <see cref="EditorUtility.DisplayDialog"/> truncates long single lines; break text into shorter lines.
    /// </summary>
    private static string WrapForNativeDialog(string text, int maxCharsPerLine)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxCharsPerLine)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length + 16);
        int index = 0;
        while (index < text.Length)
        {
            int remaining = text.Length - index;
            int take = remaining <= maxCharsPerLine ? remaining : maxCharsPerLine;
            if (take < remaining)
            {
                int windowEnd = index + take;
                int lastSpace = text.LastIndexOf(' ', windowEnd - 1, take);
                if (lastSpace > index)
                {
                    take = lastSpace - index;
                }
            }

            if (take < 1)
            {
                take = Math.Min(maxCharsPerLine, remaining);
            }

            sb.Append(text, index, take);
            index += take;
            while (index < text.Length && text[index] == ' ')
            {
                index++;
            }

            if (index < text.Length)
            {
                sb.Append('\n');
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds the same user-visible text as the real “networks removed from selection” dialog (preview / QA).
    /// </summary>
    /// <param name="removedNetworks">Internal network ids and SDK group per removed row.</param>
    /// <param name="rebuildMergedBlockRulesFromCachedConfig">When true, refreshes effective rules from <see cref="Yodo1AdNetworkManager.GetAdNetworkConfig"/> before resolving block reasons.</param>
    public static string BuildRemovalDialogBody(
        IReadOnlyList<(string networkName, SdkGroupType sdkGroup)> removedNetworks,
        bool rebuildMergedBlockRulesFromCachedConfig)
    {
        if (removedNetworks == null || removedNetworks.Count == 0)
        {
            return string.Empty;
        }

        if (rebuildMergedBlockRulesFromCachedConfig)
        {
            RebuildMergedBlockRules(Yodo1AdNetworkManager.GetInstance().GetAdNetworkConfig());
        }

        // Block reason follows the bullet on the same logical line (" — "); wrap the full entry for dialog width limits.
        const int entryLineWidth = 56;
        var blocks = new List<string>(removedNetworks.Count);
        for (int i = 0; i < removedNetworks.Count; i++)
        {
            (string networkName, SdkGroupType sdkGroup) = removedNetworks[i];
            string label = ResolveRemovalLabel(networkName, sdkGroup);
            string platformLabel = GetPlatformLabel(sdkGroup);
            string bullet = "• " + label + " (" + platformLabel + ")";
            TryGetPayloadRemovalReason(networkName, sdkGroup, out string configuredReason);
            string entry = string.IsNullOrEmpty(configuredReason) ? bullet : bullet + " — " + configuredReason;
            blocks.Add(WrapForNativeDialog(entry, entryLineWidth));
        }

        string intro = "One or more mediation networks are not available in your current Unity Editor setup (version "
                       + Application.unityVersion
                       + "). They have been removed from your saved MAS network selection:\n\n";
        string footer = "\n\nUse Yodo1 > MAS > Integration Manager to review mediation networks and dependencies.";
        return intro + string.Join("\n\n", blocks) + footer;
    }

    private static void ShowRemovalDialog()
    {
        EditorApplication.delayCall -= ShowRemovalDialog;
        s_removalDialogQueued = false;
        if (s_pendingRemovals.Count == 0) return;

        RebuildMergedBlockRules(Yodo1AdNetworkManager.GetInstance().GetAdNetworkConfig());

        var removed = new List<(string networkName, SdkGroupType sdkGroup)>(s_pendingRemovals.Count);
        for (int i = 0; i < s_pendingRemovals.Count; i++)
        {
            PendingRemoval p = s_pendingRemovals[i];
            removed.Add((p.NetworkName, p.SdkGroup));
        }

        s_pendingRemovals.Clear();

        const string title = "Yodo1 MAS";
        string body = BuildRemovalDialogBody(removed, rebuildMergedBlockRulesFromCachedConfig: false);

        if (Application.isBatchMode)
        {
            Debug.LogWarning(Yodo1U3dMas.TAG + title + ": " + body);
            return;
        }

        EditorUtility.DisplayDialog(title, body, "OK");
    }
}

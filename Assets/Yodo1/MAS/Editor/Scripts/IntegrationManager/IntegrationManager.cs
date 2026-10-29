using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Yodo1.MAS;

[InitializeOnLoad]
public class IntegrationManager : EditorWindow
{
    // ──────────────────────────────────────────────────────────────────────
    //  Layout Constants
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>Window width = 720px. Satisfies all layout constraints:
    /// ① Network Version centred at window midpoint (360px);
    /// ② SDK Actions == Net Actions width (160px) → column left edge aligned;
    /// ③ Network name column (160px) ≥ "Meta Audience Network" (~155px);
    /// ④ Actions column just wide enough to hold button + icon (no excess whitespace).</summary>
    private const float editorWindowWidth     = 720f;
    private const float editorWindowMinHeight = 500f;
    private const float windowPadding        = 12f;
    private const float colSpacing           =  8f;
    private const float rowHeight            = 24f;

    // ── SDK version table ─────────────────────────────────────────────────
    // Usable = 720 - 2×12 - 3×8 = 672px
    // Type(90) + Version(211) + Latest(211) + Actions(160) = 672
    private const float sdkTypeColWidth    =  90f;
    private const float sdkVersionColWidth = 211f;
    private const float sdkActionsColWidth = 160f;
    private const float sdkUpgradeBtnWidth =  80f;

    private static readonly float[] sdkColWidths =
    {
        sdkTypeColWidth,    // Type
        sdkVersionColWidth, // Version
        sdkVersionColWidth, // Latest Version
        sdkActionsColWidth  // Actions
    };

    // ── Network list table ────────────────────────────────────────────────
    // Usable = 720 - 2×12 - 2×8 = 680px
    // Network(160) + Version(360) + Actions(160) = 680
    // Actions(160) == SDK Actions(160) → column left edge aligned at x=548
    // Version centre = 12 + 160 + 8 + 180 = 360px
    private const float netNameColWidth    = 160f;
    private const float netVersionColWidth = 360f;
    private const float netActionsColWidth = 160f;
    private const float netActionBtnWidth  =  70f;
    private const float netIconWidth       =  20f;

    private static readonly GUILayoutOption rowHeightOption = GUILayout.Height(rowHeight);
    private static readonly GUILayoutOption netNameW       = GUILayout.Width(netNameColWidth);
    private static readonly GUILayoutOption netVersionW    = GUILayout.Width(netVersionColWidth);
    private static readonly GUILayoutOption netActionsW    = GUILayout.Width(netActionsColWidth);

    private static readonly Color separatorColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    private static readonly Color evenRowColor   = new Color(0f,   0f,   0f,   0.05f);

    // ──────────────────────────────────────────────────────────────────────
    //  Data Fields
    // ──────────────────────────────────────────────────────────────────────

    private Yodo1AdNetworkConfig adNetworkConfig;
    private Yodo1AdNetwork[] android;
    private Yodo1AdNetwork[] ios;
    private Yodo1AdNetworkConfigCacheData androidCachedData;
    private Yodo1AdNetworkConfigCacheData iosCachedData;
    private float sdkSize;

    // ──────────────────────────────────────────────────────────────────────
    //  GUI State
    // ──────────────────────────────────────────────────────────────────────

    private Vector2 scrollPosition;
    private GUIStyle tableHeaderStyle;
    private GUIStyle tableHeaderCenterStyle;
    private GUIStyle tableCellStyle;
    private GUIStyle tableCellCenterStyle;
    private GUIStyle statusBarStyle;
    private Texture installIcon;
    private int platformTabSelected;
    private int prevPlatformTabSelected;

    // ──────────────────────────────────────────────────────────────────────
    //  Download State
    // ──────────────────────────────────────────────────────────────────────

    private static string packageName = string.Empty;
    private static bool importPackageCompleted;
    private UnityWebRequest webRequest;

    // ══════════════════════════════════════════════════════════════════════
    //  Platform Helper
    // ══════════════════════════════════════════════════════════════════════

    #region Platform Helper

    /// <summary>Returns the network array for the currently selected platform tab.</summary>
    private Yodo1AdNetwork[] CurrentNetworks => platformTabSelected == 0 ? android : ios;

    /// <summary>Gets or sets the cached network data for the current platform tab.</summary>
    private Yodo1AdNetworkConfigCacheData CurrentCachedData
    {
        get => platformTabSelected == 0 ? androidCachedData : iosCachedData;
        set
        {
            if (platformTabSelected == 0)
                androidCachedData = value;
            else
                iosCachedData = value;
        }
    }

    /// <summary>Returns the SDK group type for the currently selected platform.</summary>
    private SdkGroupType CurrentSdkGroupType =>
        platformTabSelected == 0 ? SdkGroupType.AndroidStandard : SdkGroupType.IosStandard;

    /// <summary>Refreshes the cached network data from Yodo1AdNetworkManager for the current platform.</summary>
    private void RefreshCurrentCachedData()
    {
        CurrentCachedData = platformTabSelected == 0
            ? Yodo1AdNetworkManager.GetInstance().GetCachedAndroidNetworks()
            : Yodo1AdNetworkManager.GetInstance().GetCachedIosNetworks();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  Lifecycle
    // ══════════════════════════════════════════════════════════════════════

    #region Lifecycle

    static IntegrationManager()
    {
        AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
    }

    /// <summary>
    /// Handles post-import work when the MAS (Rivendell) package is installed.
    /// Ensures the Android plugins folder exists before dependency updates so EDM can copy Gradle templates.
    /// </summary>
    private static void OnImportPackageCompleted(string importedPackageName)
    {
        if (importedPackageName.Contains("Rivendell"))
        {
#if UNITY_ANDROID
            if (!Yodo1AdUtils.IsGooglePlayVersion()) return;
#endif
            UpdateAdNetworkAndDependencies();
            importPackageCompleted = true;
        }
    }

    /// <summary>
    /// Opens the Integration Manager window with a fixed width of 720px.
    /// </summary>
    [MenuItem("Yodo1/MAS/Integration Manager", false, 100)]
    static void Init()
    {
        var window = (IntegrationManager)GetWindow(typeof(IntegrationManager), true, "Yodo1 Integration Manager");
        window.minSize = new Vector2(editorWindowWidth, editorWindowMinHeight);
        window.maxSize = new Vector2(editorWindowWidth, 4096f);
        window.Show();
    }

    /// <summary>
    /// Validates the menu item; on Android, only shows when the Google Play version is active.
    /// </summary>
    [MenuItem("Yodo1/MAS/Integration Manager", true, 100)]
    static bool ValidateInit()
    {
#if UNITY_ANDROID
        return Yodo1AdUtils.IsGooglePlayVersion();
#else
        return true;
#endif
    }

    /// <summary>Initializes GUI styles and triggers the initial data load via delayCall.</summary>
    private void Awake()
    {
        tableHeaderStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize    = 12,
            fontStyle   = FontStyle.Bold,
            fixedHeight = rowHeight,
            alignment   = TextAnchor.MiddleLeft
        };
        tableHeaderCenterStyle = new GUIStyle(tableHeaderStyle)
        {
            alignment = TextAnchor.MiddleCenter
        };
        tableCellStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize    = 12,
            fixedHeight = rowHeight,
            alignment   = TextAnchor.MiddleLeft
        };
        tableCellCenterStyle = new GUIStyle(tableCellStyle)
        {
            alignment = TextAnchor.MiddleCenter
        };
        statusBarStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize    = 11,
            fixedHeight = 20,
            normal      = { textColor = EditorStyles.label.normal.textColor * 0.8f }
        };
        installIcon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Yodo1/MAS/Editor/Resources/asset1.png");
        EditorApplication.delayCall += LoadAndRepaint;
    }

    /// <summary>Polls for post-import refreshes when a package import completes.</summary>
    private void OnInspectorUpdate()
    {
        if (importPackageCompleted)
        {
            importPackageCompleted = false;
            LoadAndRepaint();
        }
    }

    /// <summary>Reloads plugin data and triggers a UI repaint.</summary>
    private void LoadAndRepaint()
    {
        LoadPluginData();
        Repaint();
    }

    void OnGUI()
    {
        DrawPluginDetails();
        GUIUtility.ExitGUI();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  Initialization
    // ══════════════════════════════════════════════════════════════════════

    #region Initialization

    /// <summary>
    /// Initializes ad network config, merges cached selections into dependency files, and refreshes assets.
    /// Callers include the Rivendell import callback and <see cref="Yodo1AdAssetsImporter"/>.
    /// </summary>
    public static void UpdateAdNetworkAndDependencies()
    {
#if UNITY_ANDROID
        if (!Yodo1AdUtils.IsGooglePlayVersion()) return;
#endif
        EnsureAndroidPluginsFolderForEdm();
        Yodo1AdNetworkManager.GetInstance().InitAdNetworkConfig();
        Yodo1AdNetworkManager.GetInstance().SyncDependenciesWithCache();
    }

    /// <summary>
    /// Creates <c>Assets/Plugins/Android</c> on disk if missing. Required because EDM copies
    /// <c>mainTemplate.gradle</c> there during Play Services / Android resolution; a missing parent
    /// directory causes <see cref="DirectoryNotFoundException"/>.
    /// </summary>
    private static void EnsureAndroidPluginsFolderForEdm()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Plugins", "Android"));
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  Data Loading
    // ══════════════════════════════════════════════════════════════════════

    #region Data Loading

    /// <summary>Loads ad network configuration, sorts networks by display name, and caches data for both platforms.</summary>
    private void LoadPluginData()
    {
        Yodo1AdNetworkManager.GetInstance().InitAdNetworkConfig();
        adNetworkConfig = Yodo1AdNetworkManager.GetInstance().GetAdNetworkConfig();
        if (adNetworkConfig == null) return;

        if (adNetworkConfig.ios != null && adNetworkConfig.ios.Length > 0)
        {
            adNetworkConfig.ios = adNetworkConfig.ios
                .OrderBy(n => n.displayName.FirstOrDefault())
                .ToArray();
        }

        if (adNetworkConfig.android != null && adNetworkConfig.android.Length > 0)
        {
            adNetworkConfig.android = adNetworkConfig.android
                .OrderBy(n => n.displayName.FirstOrDefault())
                .ToArray();
        }

        android = adNetworkConfig.android;
        ios = adNetworkConfig.ios;

        androidCachedData = Yodo1AdNetworkManager.GetInstance().GetCachedAndroidNetworks();
        iosCachedData = Yodo1AdNetworkManager.GetInstance().GetCachedIosNetworks();
        CalculateSDKSize();
    }

    /// <summary>Returns the currently installed MAS SDK version string.</summary>
    private string CurrentAdNetworkVersion()
    {
        return Yodo1AdNetworkManager.GetInstance().GetCurMakSdkVersion();
    }

    /// <summary>Returns the latest available MAS SDK version string from the server config.</summary>
    private string LatestAdNetworkVersion()
    {
        return adNetworkConfig != null ? adNetworkConfig.latestSdkversion : string.Empty;
    }

    /// <summary>Calculates the total size of all currently installed ad networks on the current platform.</summary>
    private void CalculateSDKSize()
    {
        sdkSize = 0f;
        var networks = CurrentNetworks;
        if (networks != null)
        {
            foreach (var network in networks)
            {
                if (IsNetworkInstalled(network))
                {
                    sdkSize += network.size;
                }
            }
        }
        sdkSize = Mathf.Round(sdkSize * 100f) / 100f;
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  Network Operations
    // ══════════════════════════════════════════════════════════════════════

    #region Network Operations

    /// <summary>
    /// True when a non-empty list is stored in settings. When false, the UI uses implicit behaviour:
    /// every non-hidden network (<see cref="Yodo1AdNetwork.status"/> != 1) is treated as installed.
    /// </summary>
    private static bool HasExplicitSavedNetworkList(Yodo1AdNetworkConfigCacheData cachedData)
    {
        return cachedData?.networks != null && cachedData.networks.Count > 0;
    }

    /// <summary>
    /// Writes platform group and MAS SDK version fields required when persisting an explicit selection for the first time.
    /// </summary>
    private void WriteExplicitSelectionMetadata(Yodo1AdNetworkConfigCacheData cachedData)
    {
        cachedData.sdkGroupType = CurrentSdkGroupType;
        cachedData.sdkVersion = adNetworkConfig.sdkVersion;
        cachedData.latestSdkVersion = adNetworkConfig.latestSdkversion;
    }

    /// <summary>
    /// Builds the list of network names that are "on" under the implicit default: all visible (non-hidden)
    /// networks on the current tab, optionally omitting <paramref name="excludeName"/>.
    /// </summary>
    private List<string> BuildDefaultInstalledNetworkNames(string excludeName)
    {
        var networks = CurrentNetworks;
        var list = new List<string>();
        if (networks == null) return list;

        foreach (var network in networks)
        {
            if (network.status != 1 && !string.Equals(network.name, excludeName, StringComparison.Ordinal))
            {
                list.Add(network.name);
            }
        }
        return list;
    }

    /// <summary>
    /// First transition from implicit default to a persisted list: default-all visible networks
    /// minus <paramref name="removedName"/>.
    /// </summary>
    private void PersistExplicitSelectionOnRemove(Yodo1AdNetworkConfigCacheData cachedData, string removedName)
    {
        cachedData.networks = BuildDefaultInstalledNetworkNames(removedName);
        WriteExplicitSelectionMetadata(cachedData);
    }

    /// <summary>
    /// First transition from implicit default to a persisted list: default-all visible networks,
    /// ensuring <paramref name="addedName"/> is included (e.g. installing a hidden network).
    /// </summary>
    private void PersistExplicitSelectionOnAdd(Yodo1AdNetworkConfigCacheData cachedData, string addedName)
    {
        List<string> list = BuildDefaultInstalledNetworkNames(null);
        if (!string.IsNullOrEmpty(addedName) && !list.Contains(addedName))
        {
            list.Add(addedName);
        }
        cachedData.networks = list;
        WriteExplicitSelectionMetadata(cachedData);
    }

    /// <summary>Returns whether the given ad network is currently considered installed on the active platform.</summary>
    private bool IsNetworkInstalled(Yodo1AdNetwork adNetwork)
    {
        var cachedData = CurrentCachedData;
        if (cachedData == null) return false;

        if (HasExplicitSavedNetworkList(cachedData))
        {
            return cachedData.networks.Contains(adNetwork.name);
        }

        return adNetwork.status != 1;
    }

    /// <summary>Installs the specified ad network for the current platform and refreshes the UI.</summary>
    private void InstallAdNetwork(Yodo1AdNetwork adNetwork)
    {
        var cachedData = CurrentCachedData;

        if (HasExplicitSavedNetworkList(cachedData))
        {
            if (!cachedData.networks.Contains(adNetwork.name))
            {
                cachedData.networks.Add(adNetwork.name);
            }
        }
        else
        {
            PersistExplicitSelectionOnAdd(cachedData, adNetwork.name);
        }

        Yodo1AdNetworkManager.GetInstance().UpdateAdNetworksInfo(cachedData);
        RefreshCurrentCachedData();
        CalculateSDKSize();
        Repaint();
    }

    /// <summary>Removes the specified ad network for the current platform and refreshes the UI.</summary>
    private void RemoveAdNetwork(Yodo1AdNetwork adNetwork)
    {
        var cachedData = CurrentCachedData;

        if (HasExplicitSavedNetworkList(cachedData))
        {
            cachedData.networks.Remove(adNetwork.name);
        }
        else
        {
            PersistExplicitSelectionOnRemove(cachedData, adNetwork.name);
        }

        Yodo1AdNetworkManager.GetInstance().UpdateAdNetworksInfo(cachedData);
        RefreshCurrentCachedData();
        CalculateSDKSize();
        Repaint();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  Upgrade / Download
    // ══════════════════════════════════════════════════════════════════════

    #region Upgrade / Download

    /// <summary>Returns the download URL for the latest MAS SDK version from the server config.</summary>
    private string GetUpgradeDownloadUrl()
    {
        return adNetworkConfig != null ? adNetworkConfig.sdkDownloadUrl : string.Empty;
    }

    /// <summary>Handles the Upgrade button click: resolves the package name and starts the download.</summary>
    private void UpgradeButtonClicked()
    {
        var url = GetUpgradeDownloadUrl();
        var packageComponents = url.Split(new[] { ".unitypackage" }, StringSplitOptions.None);
        packageName = packageComponents[0].Substring(packageComponents[0].LastIndexOf("/") + 1);
        if (packageName.Contains("-"))
        {
            var components = packageName.Split(new[] { "-beta" }, StringSplitOptions.None);
            packageName = components[0];
        }
        EditorCoroutineRunner.StartEditorCoroutine(DownloadPlugin(url, packageName));
    }

    /// <summary>Downloads the MAS SDK unity package from the given URL and imports it on completion.</summary>
    private IEnumerator DownloadPlugin(string downloadUrl, string version)
    {
        var path = Path.Combine(Application.temporaryCachePath, version + ".unitypackage");
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
            Debug.LogWarning(Yodo1U3dMas.TAG + "Download failed: " + webRequest.error);
        }
        else
        {
            AssetDatabase.ImportPackage(path, true);
        }

        webRequest.Dispose();
        webRequest = null;
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  SDK Version Table
    // ══════════════════════════════════════════════════════════════════════

    #region SDK Version Table

    /// <summary>Draws the complete Integration Manager layout from top to bottom.</summary>
    private void DrawPluginDetails()
    {
        GUILayout.Space(10);

        DrawSdkVersionSection();
        DrawPlatformToolbar();
        DrawMediationNetworksSection();
        DrawSeparator();
        DrawStatusBar();
    }

    /// <summary>Draws the SDK version header row and the single data row.</summary>
    private void DrawSdkVersionSection()
    {
        DrawSdkVersionHeader();
        DrawSeparator();
        DrawSdkVersionRow("Standard", CurrentAdNetworkVersion(), LatestAdNetworkVersion());
        DrawSeparator();
        GUILayout.Space(8);
    }

    /// <summary>
    /// Manually positions every SDK-version column using Rect so GUILayout
    /// padding/spacing never shifts columns unexpectedly.
    /// </summary>
    private void DrawSdkVersionHeader()
    {
        Rect row = GUILayoutUtility.GetRect(0f, rowHeight, GUILayout.ExpandWidth(true));
        DrawSdkRow(row,
            () => GUI.Label(SdkCol(row, 0), "Type",           tableHeaderStyle),
            () => GUI.Label(SdkCol(row, 1), "Version",        tableHeaderCenterStyle),
            () => GUI.Label(SdkCol(row, 2), "Latest Version", tableHeaderCenterStyle),
            () => GUI.Label(SdkCol(row, 3), "Actions",        tableHeaderCenterStyle));
    }

    /// <summary>Draws a single SDK version data row with platform name, current version, latest version, and upgrade button.</summary>
    private void DrawSdkVersionRow(string platform, string currentVersion, string latestVersion)
    {
        Rect row = GUILayoutUtility.GetRect(0f, rowHeight, GUILayout.ExpandWidth(true));
        DrawSdkRow(row,
            () => GUI.Label(SdkCol(row, 0), platform,       tableCellStyle),
            () => GUI.Label(SdkCol(row, 1), currentVersion, tableCellCenterStyle),
            () =>
            {
                if (!Yodo1AdNetworkUtil.IsPrerelease(currentVersion))
                    GUI.Label(SdkCol(row, 2), latestVersion, tableCellCenterStyle);
            },
            () =>
            {
                if (!Yodo1AdNetworkUtil.IsPrerelease(currentVersion))
                {
                    bool needsUpgrade =
                        Yodo1AdNetworkUtil.CompareVersions(currentVersion, latestVersion) == -1;
                    Rect col  = SdkCol(row, 3);
                    Rect btnR = new Rect(
                        col.x + (col.width - sdkUpgradeBtnWidth) / 2f,
                        col.y,
                        sdkUpgradeBtnWidth,
                        col.height);
                    GUI.enabled = needsUpgrade;
                    if (GUI.Button(btnR, "Upgrade")) UpgradeButtonClicked();
                    GUI.enabled = true;
                }
            });
    }

    /// <summary>Returns the Rect for SDK column <paramref name="index"/> (0-3).</summary>
    private static Rect SdkCol(Rect row, int index)
    {
        float x = windowPadding;
        for (int i = 0; i < index; i++)
            x += sdkColWidths[i] + colSpacing;
        return new Rect(row.x + x, row.y, sdkColWidths[index], row.height);
    }

    /// <summary>Executes four column-draw actions; exists only to keep callers tidy.</summary>
    private static void DrawSdkRow(Rect row, Action col0, Action col1, Action col2, Action col3)
    {
        col0(); col1(); col2(); col3();
    }

    /// <summary>Draws a thin horizontal separator line across the full window width.</summary>
    private void DrawSeparator()
    {
        Rect r = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(r, separatorColor);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  Platform Toolbar
    // ══════════════════════════════════════════════════════════════════════

    #region Platform Toolbar

    /// <summary>Draws the Android/iOS platform selector toolbar and handles tab changes.</summary>
    private void DrawPlatformToolbar()
    {
        platformTabSelected = GUILayout.Toolbar(
            platformTabSelected, new[] { "Android", "iOS" });
        if (platformTabSelected != prevPlatformTabSelected)
        {
            CalculateSDKSize();
            prevPlatformTabSelected = platformTabSelected;
        }
        GUILayout.Space(8);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════════
    //  Mediation Networks
    // ══════════════════════════════════════════════════════════════════════

    #region Mediation Networks

    /// <summary>Draws the mediation networks section title, table header, and scrollable rows.</summary>
    private void DrawMediationNetworksSection()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(windowPadding);
            GUILayout.Label("Mediation Network Details", tableHeaderStyle);
        }
        GUILayout.Space(4);
        DrawNetworkHeader();
        DrawSeparator();
        DrawNetworkRows();
    }

    /// <summary>Draws the network table header row (fixed, outside ScrollView).</summary>
    private void DrawNetworkHeader()
    {
        using (new EditorGUILayout.HorizontalScope(rowHeightOption))
        {
            GUILayout.Space(windowPadding);
            EditorGUILayout.LabelField("Network", tableHeaderStyle,       netNameW,    rowHeightOption);
            GUILayout.Space(colSpacing);
            EditorGUILayout.LabelField("Version", tableHeaderCenterStyle, netVersionW, rowHeightOption);
            GUILayout.Space(colSpacing);
            EditorGUILayout.LabelField("Actions", tableHeaderCenterStyle, netActionsW, rowHeightOption);
            GUILayout.Space(windowPadding);
        }
    }

    /// <summary>Draws all network data rows inside a ScrollView.</summary>
    private void DrawNetworkRows()
    {
        scrollPosition = GUILayout.BeginScrollView(
            scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar,
            GUILayout.ExpandHeight(true));
        var networks = CurrentNetworks;
        if (networks != null)
        {
            for (int i = 0; i < networks.Length; i++)
                DrawNetworkRow(networks[i], i);
        }
        GUILayout.EndScrollView();
    }

    /// <summary>Draws a single network data row with zebra striping.</summary>
    private void DrawNetworkRow(Yodo1AdNetwork adNetwork, int rowIndex)
    {
        if (rowIndex % 2 == 1)
        {
            Rect bg = GUILayoutUtility.GetRect(0f, rowHeight, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(bg, evenRowColor);
            GUILayout.Space(-rowHeight);
        }

        using (new EditorGUILayout.HorizontalScope(rowHeightOption))
        {
            GUILayout.Space(windowPadding);
            EditorGUILayout.LabelField(GetDisplayName(adNetwork), tableCellStyle,       netNameW,    rowHeightOption);
            GUILayout.Space(colSpacing);
            EditorGUILayout.LabelField(adNetwork.version.Trim(),  tableCellCenterStyle, netVersionW, rowHeightOption);
            GUILayout.Space(colSpacing);
            DrawNetworkActionCell(adNetwork);
            GUILayout.Space(windowPadding);
        }
    }

    /// <summary>Draws the action cell (Install / Remove / N/A) for a network row.</summary>
    private void DrawNetworkActionCell(Yodo1AdNetwork adNetwork)
    {
        float btnOffset = (netActionsColWidth - netActionBtnWidth) / 2f;

        using (new EditorGUILayout.HorizontalScope(netActionsW, rowHeightOption))
        {
            GUILayout.Space(btnOffset);

            if (IsProtectedNetwork(adNetwork))
                DrawProtectedButton();
            else if (TryDrawUnavailableButton(adNetwork)) { }
            else if (IsNetworkInstalled(adNetwork))
                DrawRemoveButton(adNetwork);
            else
                DrawInstallButton(adNetwork);
        }
    }

    /// <summary>Returns true when the network is protected and cannot be removed.</summary>
    private static bool IsProtectedNetwork(Yodo1AdNetwork adNetwork)
    {
        return adNetwork.name.IndexOf("APPLOVIN", StringComparison.OrdinalIgnoreCase) >= 0
            || adNetwork.name.IndexOf("ADMOB",    StringComparison.OrdinalIgnoreCase) >= 0
            || adNetwork.name.IndexOf("AMAZON",   StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>Draws a disabled Remove button for protected networks.</summary>
    private static void DrawProtectedButton()
    {
        GUI.enabled = false;
        GUILayout.Button("Remove", GUILayout.Width(netActionBtnWidth), rowHeightOption);
        GUI.enabled = true;
    }

    /// <summary>
    /// If the network is unavailable in the current editor, draws an N/A button with a tooltip
    /// and returns true. Otherwise returns false so the caller falls through to the next state.
    /// </summary>
    private bool TryDrawUnavailableButton(Yodo1AdNetwork adNetwork)
    {
        if (!Yodo1AdNetworkEditorAvailability.IsBlockedInUi(
                platformTabSelected != 0, adNetwork))
            return false;

        Yodo1AdNetworkEditorAvailability.TryGetBlockReasonForUi(
            platformTabSelected != 0, adNetwork, out string reason);
        string tip = string.IsNullOrEmpty(reason)
            ? "Not available in Integration Manager for this editor."
            : reason;
        GUILayout.Button(new GUIContent("N/A", tip),
            GUILayout.Width(netActionBtnWidth), rowHeightOption);
        return true;
    }

    /// <summary>Draws a Remove button with a confirmation dialog.</summary>
    private void DrawRemoveButton(Yodo1AdNetwork adNetwork)
    {
        if (GUILayout.Button("Remove", GUILayout.Width(netActionBtnWidth), rowHeightOption))
        {
            string dn   = GetDisplayName(adNetwork);
            bool   keep = EditorUtility.DisplayDialog(
                "Remove " + dn,
                "Are you sure you want to remove " + dn + "? This will impact REVENUE.",
                "Do Not Remove", "Remove");
            if (!keep) RemoveAdNetwork(adNetwork);
        }
    }

    /// <summary>Draws an Install button followed by the install icon.</summary>
    private void DrawInstallButton(Yodo1AdNetwork adNetwork)
    {
        if (GUILayout.Button("Install", GUILayout.Width(netActionBtnWidth), rowHeightOption))
            InstallAdNetwork(adNetwork);
        GUILayout.Space(4f);
        if (installIcon != null)
            GUILayout.Label(new GUIContent(installIcon),
                GUILayout.Width(netIconWidth), rowHeightOption);
    }

    /// <summary>Returns the display name for the given ad network.</summary>
    private string GetDisplayName(Yodo1AdNetwork adNetwork)
        => Yodo1AdNetworkEditorAvailability.GetNetworkListDisplayName(adNetwork);

    /// <summary>
    /// Draws the bottom status bar showing the installed network count (left)
    /// and the total SDK size for Android (right).
    /// </summary>
    private void DrawStatusBar()
    {
        using (new EditorGUILayout.HorizontalScope(GUILayout.Height(28)))
        {
            GUILayout.Space(windowPadding);

            int installed = 0;
            var networks = CurrentNetworks;
            int total = networks != null ? networks.Length : 0;
            if (networks != null)
                foreach (var n in networks)
                    if (IsNetworkInstalled(n)) installed++;

            EditorGUILayout.LabelField(
                "Mediation Networks: " + installed + " / " + total,
                statusBarStyle, GUILayout.ExpandWidth(true));

            if (platformTabSelected == 0)
            {
                var rightStyle = new GUIStyle(statusBarStyle)
                    { alignment = TextAnchor.MiddleRight };
                EditorGUILayout.LabelField(
                    "Current Size: " + sdkSize + " MB",
                    rightStyle, GUILayout.ExpandWidth(true));
            }

            GUILayout.Space(windowPadding);
        }
    }

    #endregion
}

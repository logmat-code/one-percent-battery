using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;
using Yodo1.MAS;

public class Yodo1AdNetworkUtil
{
    public static string GetCurSdkVersion(string versionPath)
    {
        if (string.IsNullOrEmpty(versionPath) || File.Exists(versionPath) == false)
        {
            Debug.LogError(Yodo1U3dMas.TAG + ": the versionPath is null or version.xml file is not exist!");
            return null;
        }

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.IgnoreComments = true;
        XmlReader reader = XmlReader.Create(versionPath, settings);

        XmlDocument xmlReadDoc = new XmlDocument();
        xmlReadDoc.Load(versionPath);
        XmlNode xnRead = xmlReadDoc.SelectSingleNode("versions");
        XmlElement unityNode = (XmlElement)xnRead.SelectSingleNode("unity");
        string version = unityNode.GetAttribute("version").ToString();

        reader.Close();

        return version;
    }

    /// <summary>
    /// Compares two semantic version strings (supports -beta suffix).
    /// Returns -1 if versionA &lt; versionB, 1 if versionA &gt; versionB, 0 if equal.
    /// </summary>
    public static int CompareVersions(string versionA, string versionB)
    {
        if (string.IsNullOrEmpty(versionA) || string.IsNullOrEmpty(versionB)) return 0;
        if (versionA.Equals(versionB)) return 0;

        int piece;
        var isVersionABeta = versionA.Contains("-beta");
        var versionABetaNumber = 0;
        if (isVersionABeta)
        {
            var components = versionA.Split(new[] { "-beta" }, StringSplitOptions.None);
            versionA = components[0];
            if (components[1].Contains("."))
            {
                components[1] = components[1].Replace(".", string.Empty);
            }
            versionABetaNumber = int.TryParse(components[1], out piece) ? piece : 0;
        }

        var isVersionBBeta = versionB.Contains("-beta");
        var versionBBetaNumber = 0;
        if (isVersionBBeta)
        {
            var components = versionB.Split(new[] { "-beta" }, StringSplitOptions.None);
            versionB = components[0];
            if (components[1].Contains("."))
            {
                components[1] = components[1].Replace(".", string.Empty);
            }
            versionBBetaNumber = int.TryParse(components[1], out piece) ? piece : 0;
        }

        if (versionA.Equals(versionB))
        {
            if (isVersionABeta && isVersionBBeta)
            {
                return versionABetaNumber.CompareTo(versionBBetaNumber);
            }
            if (isVersionABeta) return -1;
            if (isVersionBBeta) return 1;
        }

        var versionAComponents = versionA.Split('.').Select(v => int.TryParse(v, out piece) ? piece : 0).ToArray();
        var versionBComponents = versionB.Split('.').Select(v => int.TryParse(v, out piece) ? piece : 0).ToArray();
        var length = Mathf.Max(versionAComponents.Length, versionBComponents.Length);
        for (var i = 0; i < length; i++)
        {
            var a = i < versionAComponents.Length ? versionAComponents[i] : 0;
            var b = i < versionBComponents.Length ? versionBComponents[i] : 0;
            if (a != b) return a < b ? -1 : 1;
        }

        return 0;
    }

    /// <summary>
    /// Leading major.minor.patch from <see cref="Application.unityVersion"/> (e.g. 6000.3.2f1 -> 6000.3.2).
    /// For custom editor tooling only; not for runtime builds.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string GetUnityEditorVersionComparable()
    {
        string v = Application.unityVersion;
        System.Text.RegularExpressions.Match m = Regex.Match(v ?? string.Empty, @"^(\d+\.\d+\.\d+)");
        return m.Success ? m.Groups[1].Value : "0.0.0";
    }

    /// <summary>
    /// Returns true if the version string indicates a pre-release (alpha or beta).
    /// </summary>
    public static bool IsPrerelease(string version)
    {
        return !string.IsNullOrEmpty(version) && (version.Contains("alpha") || version.Contains("beta"));
    }

    /// <summary>
    /// True when the running editor&apos;s comparable version is &gt;= <paramref name="inclusiveLowerBound"/> (inclusive).
    /// For editor tooling only; the bound is an arbitrary comparison floor, not an SDK &quot;minimum Unity&quot; claim.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static bool IsUnityEditorVersionAtLeast(string inclusiveLowerBound)
    {
        return CompareVersions(GetUnityEditorVersionComparable(), inclusiveLowerBound) >= 0;
    }
}

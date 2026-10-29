#if UNITY_2018_4_OR_NEWER && (UNITY_ANDROID || UNITY_IOS)

using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Yodo1.MAS;

public class Yodo1IdSync : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Yodo1AdSettingsSync.SyncBeforeBuild();
    }
}

#endif

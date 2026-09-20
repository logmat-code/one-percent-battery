#pragma warning disable CS0626
#if UNITY_IOS || UNITY_EDITOR
using UnityEngine;
using System.Runtime.InteropServices;
#endif

public static class iCloudKV
{
#if UNITY_IOS || UNITY_EDITOR
    [DllImport("__Internal")]
#endif
    public static extern void iCloudKV_Synchronize();

#if UNITY_IOS || UNITY_EDITOR
    [DllImport("__Internal")]
#endif
    public static extern void iCloudKV_SetInt(string key, int value);

#if UNITY_IOS || UNITY_EDITOR
    [DllImport("__Internal")]
#endif
    public static extern void iCloudKV_SetFloat(string key, float value);

#if UNITY_IOS || UNITY_EDITOR
    [DllImport("__Internal")]
#endif
    public static extern int iCloudKV_GetInt(string key);

#if UNITY_IOS || UNITY_EDITOR
    [DllImport("__Internal")]
#endif
    public static extern float iCloudKV_GetFloat(string key);

#if UNITY_IOS || UNITY_EDITOR
    [DllImport("__Internal")]
#endif
    public static extern void iCloudKV_Reset();
}
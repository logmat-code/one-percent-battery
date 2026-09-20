namespace UGSSpace {
public class TimeScaleEffect : UGS_TimeScaleEffect
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ClockIcon"; }
    }
#endif
}
}

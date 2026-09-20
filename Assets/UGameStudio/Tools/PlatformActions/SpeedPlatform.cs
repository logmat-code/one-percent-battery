namespace UGSSpace {
public class SpeedPlatform : UGS_SpeedPlatform
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "SpeedPlatform_Icon"; }
    }
#endif
}
}

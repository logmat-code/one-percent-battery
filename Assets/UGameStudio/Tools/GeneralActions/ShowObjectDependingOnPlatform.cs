namespace UGSSpace {
public class ShowObjectDependingOnPlatform : UGS_ShowObjectDependingOnPlatform
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ShowObjectDependingOnPlatform_Icon"; }
    }
#endif
}
}

namespace UGSSpace {
public class BouncingPlatform : UGS_BouncingPlatform
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "BouncingPlatform_Icon"; }
    }
#endif
}
}

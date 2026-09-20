namespace UGSSpace {
public class SlipperyPlatform : UGS_SlipperyPlatform
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "SlipperyPlatform_Icon"; }
    }
#endif
}
}

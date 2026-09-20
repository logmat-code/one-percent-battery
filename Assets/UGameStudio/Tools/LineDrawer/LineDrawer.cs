namespace UGSSpace {
public class LineDrawer : UGS_LineDrawer
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "LineDrawerIcon"; }
    }
#endif
}
}

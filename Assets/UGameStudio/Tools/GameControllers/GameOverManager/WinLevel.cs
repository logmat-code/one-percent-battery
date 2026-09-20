namespace UGSSpace {
public class WinLevel : UGS_WinLevel
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "WinLevel_Icon"; }
    }
#endif
}
}

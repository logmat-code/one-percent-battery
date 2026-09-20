namespace UGSSpace {
public class ShowPlayerHealth : UGS_ShowPlayerHealth
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ShowPlayerHealth_Icon"; }
    }
#endif
}
}

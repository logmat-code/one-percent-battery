namespace UGSSpace {
public class ShowPlayerIcon : UGS_ShowPlayerIcon
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "rightDirectionHeadWhite_Icon"; }
    }
#endif
}
}

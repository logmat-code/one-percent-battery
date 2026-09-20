namespace UGSSpace {
public class ShowScore : UGS_ShowScore
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ShowScore_Icon"; }
    }
#endif
}
}

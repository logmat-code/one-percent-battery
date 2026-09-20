namespace UGSSpace {
public class ShowRemainingTimeToGetLife : UGS_ShowRemainingTimeToGetLife
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ShowRemainingTimeToGetLife_Icon"; }
    }
#endif
}
}

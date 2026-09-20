namespace UGSSpace {
public class ProgressiveTimeScore : UGS_ProgressiveTimeScore
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ProgressiveTimeScore_Icon"; }
    }
#endif
}
}

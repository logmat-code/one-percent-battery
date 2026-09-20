namespace UGSSpace {
public class ScoreIncreaser : UGS_ScoreIncreaser
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ScoreIncreaser_Icon"; }
    }
#endif
}
}

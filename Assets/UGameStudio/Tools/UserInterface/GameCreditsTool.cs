namespace UGSSpace {
public class GameCreditsTool : UGS_GameCreditsTool
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "GameCreditsTool_Icon"; }
    }
#endif
}
}

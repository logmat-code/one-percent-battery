namespace UGSSpace {
public class GameCountdown : UGS_GameCountdown
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "GameCountdown_Icon"; }
    }
#endif
}
}

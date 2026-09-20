namespace UGSSpace {
public class IncreaseGameCountdown : UGS_IncreaseGameCountdown
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "IncreaseGameCountdown_Icon"; }
    }
#endif
}
}

namespace UGSSpace {
public class ButtonToContinueAndReviveThePlayer : UGS_ButtonToContinueAndReviveThePlayer
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ButtonToContinueAndReviveThePlayer_Icon"; }
    }
#endif
}
}

namespace UGSSpace {
public class PauseButton : UGS_PauseButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "PauseButton_Icon"; }
    }
#endif
}
}

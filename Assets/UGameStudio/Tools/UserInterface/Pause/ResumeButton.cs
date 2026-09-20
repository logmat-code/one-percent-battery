namespace UGSSpace {
public class ResumeButton : UGS_ResumeButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ResumeButton_Icon"; }
    }
#endif
}
}

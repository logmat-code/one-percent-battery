namespace UGSSpace {
public class OpenURLButton : UGS_OpenURLButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "OpenURLButton_Icon"; }
    }
#endif
}
}

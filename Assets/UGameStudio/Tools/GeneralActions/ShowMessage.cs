namespace UGSSpace {
public class ShowMessage : UGS_ShowMessage
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ShowMessage_Icon"; }
    }
#endif
}
}

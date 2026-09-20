namespace UGSSpace {
public class DialogueManager : UGS_DialogueManager
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

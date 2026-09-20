namespace UGSSpace {
public class ButtonToDeleteSavePoint : UGS_ButtonToDeleteSavePoint
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ButtonToDeleteSavePoint_Icon"; }
    }
#endif
}
}

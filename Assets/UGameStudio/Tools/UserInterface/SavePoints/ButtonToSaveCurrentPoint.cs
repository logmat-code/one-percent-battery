namespace UGSSpace {
public class ButtonToSaveCurrentPoint : UGS_ButtonToSaveCurrentPoint
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ButtonToSaveCurrentPoint_Icon"; }
    }
#endif
}
}

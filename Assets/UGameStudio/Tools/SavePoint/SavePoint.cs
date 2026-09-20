namespace UGSSpace {
public class SavePoint : UGS_SavePoint
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "SavePoint_Icon"; }
    }
#endif
}
}

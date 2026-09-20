namespace UGSSpace {
public class Mount : UGS_Mount
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "Mount"; }
    }
#endif
}
}

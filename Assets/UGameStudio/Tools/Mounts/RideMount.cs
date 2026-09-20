namespace UGSSpace {
public class RideMount : UGS_RideMount
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "RideMount_Icon"; }
    }
#endif
}
}

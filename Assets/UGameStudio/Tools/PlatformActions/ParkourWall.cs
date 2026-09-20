namespace UGSSpace {
public class ParkourWall : UGS_ParkourWall
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ParkourWall_Icon"; }
    }
#endif
}
}

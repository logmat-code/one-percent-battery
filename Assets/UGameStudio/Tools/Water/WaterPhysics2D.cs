namespace UGSSpace {
public class WaterPhysics2D : UGS_WaterPhysics2D
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "WaterPhysics2D_Icon"; }
    }
#endif
}
}

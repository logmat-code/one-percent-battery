namespace UGSSpace {
public class WaterPhysics3D : UGS_WaterPhysics3D
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

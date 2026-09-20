namespace UGSSpace {
public class CameraMovement3D : UGS_CameraMovement3D
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "CameraMovement3D_Icon"; }
    }
#endif
}
}

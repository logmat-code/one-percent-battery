namespace UGSSpace {
public class VirtualJoystick2 : UGS_VirtualJoystick2
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "VirtualJoystickV2_Icon"; }
    }
#endif
}
}

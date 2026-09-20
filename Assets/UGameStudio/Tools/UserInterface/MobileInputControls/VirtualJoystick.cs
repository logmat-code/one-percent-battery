namespace UGSSpace {
public class VirtualJoystick : UGS_VirtualJoystick
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "VirtualJoystick_Icon"; }
    }
#endif
}
}

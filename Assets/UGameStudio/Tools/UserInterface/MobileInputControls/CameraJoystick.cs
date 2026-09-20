namespace UGSSpace
{
    public class CameraJoystick : UGS_CameraJoystick
#if UNITY_EDITOR
        , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "CameraJoystick_Icon"; }
        }
#endif
    }
}
namespace UGSSpace {
public class MovingPlatform : UGS_MovingPlatform
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "MovingPlatform_Icon"; }
    }
#endif
}
}

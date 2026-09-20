namespace UGSSpace {
public class ShowNumberOfProjectiles : UGS_ShowNumberOfProjectiles
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ShowNumberOfProjectiles_Icon"; }
    }
#endif
}
}

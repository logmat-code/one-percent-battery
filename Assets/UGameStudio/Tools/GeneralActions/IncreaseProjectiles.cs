namespace UGSSpace {
public class IncreaseProjectiles : UGS_IncreaseProjectiles
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "IncreaseProjectiles_Icon"; }
    }
#endif
}
}

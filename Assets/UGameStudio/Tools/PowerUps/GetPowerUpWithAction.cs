namespace UGSSpace {
public class GetPowerUpWithAction : UGS_GetPowerUpWithAction
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "GetPowerUpWithAction_Icon"; }
    }
#endif
}
}

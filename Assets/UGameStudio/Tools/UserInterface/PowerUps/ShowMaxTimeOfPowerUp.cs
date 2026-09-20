namespace UGSSpace {
public class ShowMaxTimeOfPowerUp : UGS_ShowMaxTimeOfPowerUp
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ShowMaxTimeOfPowerUp_Icon"; }
    }
#endif
}
}

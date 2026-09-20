namespace UGSSpace {
public class ShowNumberOfUsesOfPowerUp : UGS_ShowNumberOfUsesOfPowerUp
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ShowNumberOfUsesOfPowerUp_Icon"; }
    }
#endif
}
}

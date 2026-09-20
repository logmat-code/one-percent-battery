namespace UGSSpace {
public class PlayerPowerUpsManager : UGS_PlayerPowerUpsManager
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "PlayerPowerUpsManager_Icon"; }
    }
#endif
}
}

namespace UGSSpace {
public class PowerUpUpgradingButton : UGS_PowerUpUpgradingButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "PowerUpUpgradingButton_Icon"; }
    }
#endif
}
}

namespace UGSSpace {
public class PowerUpPurchaseButton : UGS_PowerUpPurchaseButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "PowerUpPurchaseButton_Icon"; }
    }
#endif
}
}

namespace UGSSpace {
public class WeaponPurchaseButton : UGS_WeaponPurchaseButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "WeaponPurchaseButton_Icon"; }
    }
#endif
}
}

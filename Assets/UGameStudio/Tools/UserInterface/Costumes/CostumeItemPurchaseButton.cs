namespace UGSSpace {
public class CostumeItemPurchaseButton : UGS_CostumeItemPurchaseButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "CostumeItemPurchaseButton_Icon"; }
    }
#endif
}
}

namespace UGSSpace {
public class CharacterPurchaseButton : UGS_CharacterPurchaseButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "CharacterPurchaseButton_Icon"; }
    }
#endif
}
}

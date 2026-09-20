namespace UGSSpace {
public class CostumeItemSelectButton : UGS_CostumeItemSelectButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "CostumeItemSelectButton_Icon"; }
    }
#endif
}
}

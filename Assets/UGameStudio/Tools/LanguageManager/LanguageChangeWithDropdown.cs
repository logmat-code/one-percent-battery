namespace UGSSpace {
public class LanguageChangeWithDropdown : UGS_LanguageChangeWithDropdown
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "LanguageChangeButton_Icon"; }
    }
#endif
}
}

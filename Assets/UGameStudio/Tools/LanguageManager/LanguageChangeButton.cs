namespace UGSSpace {
public class LanguageChangeButton : UGS_LanguageChangeButton
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

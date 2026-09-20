namespace UGSSpace {
public class LanguageManager : UGS_LanguageManager
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "LanguageManagerIcon"; }
    }
#endif
}
}

namespace UGSSpace {
public class SelectorButton : UGS_SelectorButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "SelectorButton_Icon"; }
    }
#endif
}
}

namespace UGSSpace {
public class LoadLevelButton : UGS_LoadLevelButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "LoadLevelButton_Icon"; }
    }
#endif
}
}

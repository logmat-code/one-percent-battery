namespace UGSSpace {
public class LoadLevelWithAction : UGS_LoadLevelWithAction
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

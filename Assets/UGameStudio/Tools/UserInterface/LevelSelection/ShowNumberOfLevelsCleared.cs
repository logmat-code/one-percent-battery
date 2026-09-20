namespace UGSSpace {
public class ShowNumberOfLevelsCleared : UGS_ShowNumberOfLevelsCleared
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ShowNumberOfLevelsCleared_Icon"; }
    }
#endif
}
}

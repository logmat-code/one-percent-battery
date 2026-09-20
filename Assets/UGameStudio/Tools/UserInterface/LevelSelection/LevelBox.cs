namespace UGSSpace {
public class LevelBox : UGS_LevelBox
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "LevelBox_Icon"; }
    }
#endif
}
}

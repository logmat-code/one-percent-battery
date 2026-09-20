namespace UGSSpace {
public class BonusLevelBox : UGS_BonusLevelBox
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

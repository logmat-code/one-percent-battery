namespace UGSSpace {
public class MoveObjectToCurrentLevelBox : UGS_MoveObjectToCurrentLevelBox
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "MoveObjectToCurrentLevelBox_Icon"; }
    }
#endif
}
}

namespace UGSSpace {
public class GamePauseManager : UGS_GamePauseManager
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "GamePauseManager_Icon"; }
    }
#endif
}
}

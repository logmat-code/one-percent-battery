namespace UGSSpace {
public class GameSettings : UGS_GameSettings
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "GameSettings_Icon"; }
    }
#endif
}
}

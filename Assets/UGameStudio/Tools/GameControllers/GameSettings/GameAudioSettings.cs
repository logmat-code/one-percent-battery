namespace UGSSpace {
public class GameAudioSettings : UGS_GameAudioSettings
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "GameAudioSettings_Icon"; }
    }
#endif
}
}

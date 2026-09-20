namespace UGSSpace {
public class DismountPlayer : UGS_DismountPlayer
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "DismountPlayer_Icon"; }
    }
#endif
}
}

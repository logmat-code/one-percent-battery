namespace UGSSpace {
public class Player : UGS_Player
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "Player_Icon"; }
    }
#endif
}
}

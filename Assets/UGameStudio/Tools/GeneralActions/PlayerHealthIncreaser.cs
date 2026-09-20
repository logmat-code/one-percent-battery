namespace UGSSpace {
public class PlayerHealthIncreaser : UGS_PlayerHealthIncreaser
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "PlayerHealthIncreaser_Icon"; }
    }
#endif
}
}

namespace UGSSpace {
public class PlayerHealthReducer : UGS_PlayerHealthReducer
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "PlayerHealthReducer_Icon"; }
    }
#endif
}
}

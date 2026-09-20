namespace UGSSpace {
public class PlayerVelocityInverter : UGS_PlayerVelocityInverter
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "PlayerVelocityInverter_Icon"; }
    }
#endif
}
}

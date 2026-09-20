namespace UGSSpace {
public class GetRandomPowerUp : UGS_GetRandomPowerUp
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "RandomPowerUp_Icon"; }
    }
#endif
}
}

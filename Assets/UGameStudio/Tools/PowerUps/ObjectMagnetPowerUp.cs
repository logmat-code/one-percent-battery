namespace UGSSpace {
public class ObjectMagnetPowerUp : UGS_ObjectMagnetPowerUp
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ObjectMagnetPowerUp_Icon"; }
    }
#endif
}
}

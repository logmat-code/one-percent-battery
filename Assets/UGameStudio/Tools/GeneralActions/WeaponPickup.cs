namespace UGSSpace {
public class WeaponPickup : UGS_WeaponPickup
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "WeaponPickup_Icon"; }
    }
#endif
}
}

namespace UGSSpace {
public class ShowWeaponIcon : UGS_ShowWeaponIcon
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "WeaponsManager_Icon"; }
    }
#endif
}
}

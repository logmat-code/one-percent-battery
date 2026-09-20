namespace UGSSpace {
public class WeaponSelectButton : UGS_WeaponSelectButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "WeaponSelectButton_Icon"; }
    }
#endif
}
}

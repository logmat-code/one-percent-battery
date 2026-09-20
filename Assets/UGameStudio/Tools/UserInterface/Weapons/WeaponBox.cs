namespace UGSSpace {
public class WeaponBox : UGS_WeaponBox
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "WeaponBox_Icon"; }
    }
#endif
}
}

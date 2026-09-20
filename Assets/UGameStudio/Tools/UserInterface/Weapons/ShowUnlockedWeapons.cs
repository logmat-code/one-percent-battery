namespace UGSSpace {
public class ShowUnlockedWeapons : UGS_ShowUnlockedWeapons
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ShowUnlockedWeapons_Icon"; }
    }
#endif
}
}

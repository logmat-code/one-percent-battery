namespace UGSSpace {
public class CostumeItemBox : UGS_CostumeItemBox
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "CostumeItemBox_Icon"; }
    }
#endif
}
}

namespace UGSSpace {
public class CostumeItemInstantiator : UGS_CostumeItemInstantiator
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "CostumeItemInstantiator_Icon"; }
    }
#endif
}
}

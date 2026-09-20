namespace UGSSpace {
public class ConstantSpeed : UGS_ConstantSpeed
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ConstantSpeed_Icon"; }
    }
#endif
}
}

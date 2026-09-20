namespace UGSSpace {
public class Teleporter : UGS_Teleporter
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "Teleporter_Icon"; }
    }
#endif
}
}

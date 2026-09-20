namespace UGSSpace {
public class Ladders : UGS_Ladders
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "Ladders_Icon"; }
    }
#endif
}
}

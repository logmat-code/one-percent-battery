namespace UGSSpace {
public class TravelLine : UGS_TravelLine
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "LineOfTravel_Icon"; }
    }
#endif
}
}

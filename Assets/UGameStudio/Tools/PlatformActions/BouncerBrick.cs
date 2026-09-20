namespace UGSSpace {
public class BouncerBrick : UGS_BouncerBrick
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "BouncerBrick_Icon"; }
    }
#endif
}
}

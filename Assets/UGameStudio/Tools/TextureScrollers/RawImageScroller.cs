namespace UGSSpace {
public class RawImageScroller : UGS_RawImageScroller
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ParallaxScrolling_Icon"; }
    }
#endif
}
}

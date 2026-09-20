namespace UGSSpace {
public class TextureScroller : UGS_TextureScroller
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

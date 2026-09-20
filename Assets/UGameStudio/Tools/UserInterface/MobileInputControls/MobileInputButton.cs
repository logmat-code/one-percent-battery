namespace UGSSpace {
public class MobileInputButton : UGS_MobileInputButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "MobileInputButton_Icon"; }
    }
#endif
}
}

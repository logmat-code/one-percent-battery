namespace UGSSpace {
public class ProjectilesPurchaseButton : UGS_ProjectilesPurchaseButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ProjectilesPurchaseButton_Icon"; }
    }
#endif
}
}

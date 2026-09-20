namespace UGSSpace {
public class PowerUpActivatorButton : UGS_PowerUpActivatorButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "PowerUpActivatorButton_Icon"; }
    }
#endif
}
}

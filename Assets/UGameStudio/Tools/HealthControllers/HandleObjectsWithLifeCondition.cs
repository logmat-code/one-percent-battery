namespace UGSSpace {
public class HandleObjectsWithLifeCondition : UGS_HandleObjectsWithLifeCondition
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "HandleObjectsWithLifeCondition_Icon"; }
    }
#endif
}
}

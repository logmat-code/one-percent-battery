namespace UGSSpace {
public class EnemyHealthReducer : UGS_EnemyHealthReducer
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "EnemyHealthReducer_Icon"; }
    }
#endif
}
}

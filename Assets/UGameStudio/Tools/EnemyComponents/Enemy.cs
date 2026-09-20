namespace UGSSpace
{
    public class Enemy : UGS_Enemy
#if UNITY_EDITOR
        , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "Enemy_Icon"; }
        }
#endif
    }
}

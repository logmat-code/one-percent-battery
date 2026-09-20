namespace UGSSpace
{
    public class SceneVariables : UGS_SceneVariables
#if UNITY_EDITOR
        , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "bool_var_icon"; }
        }
#endif
    }
}

namespace UGSSpace
{
    public class ObjectVariables : UGS_ObjectVariables
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

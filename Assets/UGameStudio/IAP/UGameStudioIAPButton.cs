namespace UGSSpace
{
    public class UGameStudioIAPButton : UGS_UGameStudioIAPButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "productsIcon"; }
        }
#endif
    }
}

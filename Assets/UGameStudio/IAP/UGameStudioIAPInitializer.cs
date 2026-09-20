namespace UGSSpace
{
    public class UGameStudioIAPInitializer : UGS_UGameStudioIAPInitializer
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

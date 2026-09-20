namespace UGSSpace
{
    public class ModalPanel : UGS_ModalPanel
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "ModalPanelIcon"; }
        }
#endif
    }
}

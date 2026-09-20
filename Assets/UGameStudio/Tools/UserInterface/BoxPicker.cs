namespace UGSSpace
{
    public class BoxPicker : UGS_BoxPicker
#if UNITY_EDITOR
        , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "BoxPickerIcon"; }
        }
#endif
    }
}
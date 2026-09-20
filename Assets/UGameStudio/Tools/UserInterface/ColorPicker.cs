namespace UGSSpace
{
    public class ColorPicker : UGS_ColorPicker
#if UNITY_EDITOR
        , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "ColorPickerIcon"; }
        }
#endif
    }
}
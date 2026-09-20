namespace UGSSpace
{
    public class ShareInSocialMedia_Button : UGS_ShareInSocialMedia_Button
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "Share_icon"; }
        }
#endif
    }
}
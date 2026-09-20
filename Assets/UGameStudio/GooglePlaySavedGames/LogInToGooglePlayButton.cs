namespace UGSSpace
{
    public class LogInToGooglePlayButton : UGS_LogInToGooglePlayButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "loginIcon"; }
        }
#endif
    }
}
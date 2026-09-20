namespace UGSSpace
{
    public class LoginToGameCenterButton : UGS_LoginToGameCenterButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "login_icon"; }
        }
#endif
    }
}
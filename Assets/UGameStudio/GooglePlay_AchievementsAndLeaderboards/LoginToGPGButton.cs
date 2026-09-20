namespace UGSSpace
{
    public class LoginToGPGButton : UGS_LoginToGPGButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {

#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "GPGLogin_icon"; }
        }
#endif
    }
}
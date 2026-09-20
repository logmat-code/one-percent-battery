namespace UGSSpace
{
    public class ShowGPGLeaderboardsButton : UGS_ShowGPGLeaderboardsButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {

#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "GPGLeaderboards_icon"; }
        }
#endif
    }
}
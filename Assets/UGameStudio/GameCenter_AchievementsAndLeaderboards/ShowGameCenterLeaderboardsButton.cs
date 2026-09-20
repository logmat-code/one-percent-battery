namespace UGSSpace
{
    public class ShowGameCenterLeaderboardsButton : UGS_ShowGameCenterLeaderboardsButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "game_center_leaderboards_icon"; }
        }
#endif
    }
}
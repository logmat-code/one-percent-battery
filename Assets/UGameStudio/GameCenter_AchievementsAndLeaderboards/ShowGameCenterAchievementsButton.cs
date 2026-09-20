namespace UGSSpace
{
    public class ShowGameCenterAchievementsButton : UGS_ShowGameCenterAchievementsButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "game_center_achievements_icon"; }
        }
#endif
    }
}
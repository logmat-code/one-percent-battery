namespace UGSSpace
{
    public class ResetAchievementsButton : UGS_ResetAchievementsButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "reset_achievements_icon"; }
        }
#endif

        public override void ResetAchievements()
        {
        }
    }
}
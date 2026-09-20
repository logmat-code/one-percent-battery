namespace UGSSpace
{
    public class ShowGPGAchievementsButton : UGS_ShowGPGAchievementsButton
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {

#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "GPGAchievements_icon"; }
        }
#endif
    }
}
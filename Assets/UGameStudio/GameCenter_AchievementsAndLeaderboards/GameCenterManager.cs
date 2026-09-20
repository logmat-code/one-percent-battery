namespace UGSSpace
{
    public class GameCenterManager : UGS_GameCenterManager
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "game_center_ico"; }
        }
#endif
    }
}
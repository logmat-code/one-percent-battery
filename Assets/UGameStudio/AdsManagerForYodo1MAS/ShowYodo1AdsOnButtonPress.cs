namespace UGSSpace
{
    public class ShowYodo1AdsOnButtonPress : UGS_ShowYodo1AdsOnButtonPress
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {

#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "ads_icon"; }
        }
#endif
    }
}

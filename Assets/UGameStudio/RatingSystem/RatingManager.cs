namespace UGSSpace
{
    public class RatingManager : UGS_RatingManager
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {

#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "Rating_icon"; }
        }
#endif
    }
}
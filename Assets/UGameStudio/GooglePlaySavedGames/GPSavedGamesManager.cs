namespace UGSSpace
{
    using UGameStudioGDK;
    using UnityEngine;
    using System.Text;

    public class GPSavedGamesManager : UGS_GPSavedGamesManager
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "googleCloudIco"; }
        }
#endif
    }
}
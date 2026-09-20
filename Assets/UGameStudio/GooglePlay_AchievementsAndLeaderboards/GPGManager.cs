namespace UGSSpace
{
    using UnityEngine;

    public class GPGManager : UGS_GPGManager
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {

#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "GPGManager_icon"; }
        }
#endif
    }
}
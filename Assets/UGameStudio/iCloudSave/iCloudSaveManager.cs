namespace UGSSpace
{
    public class iCloudSaveManager : UGS_iCloudSaveManager
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {

#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "iCloudIco"; }
        }

#endif
    }
}
namespace UGSSpace
{
    public class ConstantRotation : UGS_ConstantRotation
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "ConstantRotation_Icon"; }
        }
#endif

#if UNITY_EDITOR
        void Reset()
        {
            if (objectToRotate == null)
                objectToRotate = this.transform;
        }
#endif
    }
}

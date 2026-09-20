namespace UGSSpace {
public class ObjectSpawner : UGS_ObjectSpawner
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{

#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "ObjectInstantiator_Icon"; }
    }
#endif
}
}

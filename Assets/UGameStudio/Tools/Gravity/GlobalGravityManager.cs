namespace UGSSpace {
using UnityEngine;
public class GlobalGravityManager : UGS_GlobalGravityManager
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "GlobalGravityManager_Icon"; }
    }
#endif

#if UNITY_EDITOR
    void Reset()
    {
        defaultGravityX = Physics2D.gravity.x;
        newGravityX = -Physics2D.gravity.x;

        defaultGravityY = Physics2D.gravity.y;
        newGravityY = -Physics2D.gravity.y;
    }
#endif
}
}

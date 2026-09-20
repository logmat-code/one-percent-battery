namespace UGSSpace {
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class JetpackPowerUp : UGS_JetpackPowerUp
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
{
#if UNITY_EDITOR
    public string EditorIconPath
    {
        get { return "JetpackPowerUp_Icon"; }
    }
#endif

#if UNITY_EDITOR
    void Reset()
    {
        groundMask = 1 << LayerMask.NameToLayer("Ground");
    }

    void OnDrawGizmosSelected()
    {
        if (groundDetection == GroundDetection.DefinedHere && groundChecker != null)
        {
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(groundChecker.position, groundChecker.forward, radiusOfGroundChecker);
        }
    }
#endif
}
}

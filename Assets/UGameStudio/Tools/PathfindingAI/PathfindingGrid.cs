namespace UGSSpace {
using UnityEngine;
public class PathfindingGrid : UGS_PathfindingGrid
{
#if UNITY_EDITOR
    void OnEnable()
    {
        if (GetComponent<Pathfinding>() == null)
        {
            pf = gameObject.AddComponent<Pathfinding>();
            pf.hideFlags = HideFlags.HideInInspector;
        }
        else
        {
            pf = GetComponent<Pathfinding>();
        }

        if (gameObject.GetComponent<PathRequestManager>() == null)
        {
            prm = gameObject.AddComponent<PathRequestManager>();
            prm.hideFlags = HideFlags.HideInInspector;
        }
        else
        {
            prm = GetComponent<PathRequestManager>();
        }
    }
#endif
}
}

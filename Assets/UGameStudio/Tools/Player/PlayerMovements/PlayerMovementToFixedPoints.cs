namespace UGSSpace {
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
public class PlayerMovementToFixedPoints : UGS_PlayerMovementToFixedPoints
{
#if UNITY_EDITOR
    public override void Start()
    {
        if (usePlayerStates && FindAnyObjectByType<UGS_Player>() == null)
        {
            Debug.LogError("<color=yellow>UGame Studio: </color>You do not have any object with the Player component in the scene, and this is necessary to use the Player States.");
            EditorApplication.ExecuteMenuItem("Edit/Play");
        }

        base.Start();
    }

    void OnDrawGizmos()
    {
        Handles.color = wayColor;
        if (points.Count > 2 && loopMovement && points[points.Count - 1] != null && points[points.Count - 1].wayPointPosition != null)
        {
            if (usingCurves)
            {
                Handles.DrawBezier(points[0].wayPointPosition.position,
                    points[points.Count - 1].wayPointPosition.position,
                    points[points.Count - 1].bezierPoint1.position,
                    points[points.Count - 1].bezierPoint2.position,
                    wayColor,
                    null,
                    waySize);
            }
            else
            {
                Handles.color = wayColor;
                Handles.DrawLine(points[0].wayPointPosition.position, points[points.Count - 1].wayPointPosition.position);
            }
        }

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] != null && points[i].wayPointPosition != null)
            {

                if (i != points.Count - 1 && points[i + 1] != null && points[i + 1].wayPointPosition != null)
                {
                    if (usingCurves)
                    {
                        Handles.DrawBezier(points[i].wayPointPosition.position,
                            points[i + 1].wayPointPosition.position,
                            points[i].bezierPoint1.position,
                            points[i].bezierPoint2.position,
                            wayColor,
                           null,
                            waySize);
                    }
                    else
                    {
                        Handles.color = wayColor;
                        Handles.DrawLine(points[i].wayPointPosition.position, points[i + 1].wayPointPosition.position);
                    }
                }

                Handles.Label(points[i].wayPointPosition.position, i.ToString(), EditorStyles.boldLabel);
                Gizmos.color = wayPointsColor;
                Gizmos.DrawSphere(points[i].wayPointPosition.position, wayPointsSize);

            }
        }
    }
#endif
}
}

namespace UGSSpace
{
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using UnityEngine;

    public class PathFollower : UGS_PathFollower
#if UNITY_EDITOR
        , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "PathFollower_Icon"; }
        }
#endif

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (points != null)
            {
                Handles.color = editorSettings.wayColor;
                if (points.Count > 2 && loopMovement && points[points.Count - 1] != null && points[0].wayPointPosition != null && points[points.Count - 1].wayPointPosition != null)
                {
                    if (usingCurves)
                    {
                        Handles.DrawBezier(points[0].wayPointPosition.position,
                            points[points.Count - 1].wayPointPosition.position,
                            points[points.Count - 1].bezierPoint1.position,
                            points[points.Count - 1].bezierPoint2.position,
                            editorSettings.wayColor,
                            null,
                            editorSettings.waySize);
                    }
                    else
                    {
                        Handles.color = editorSettings.wayColor;
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
                                    editorSettings.wayColor,
                                   null,
                                    editorSettings.waySize);
                            }
                            else
                            {
                                Handles.color = editorSettings.wayColor;
                                Handles.DrawLine(points[i].wayPointPosition.position, points[i + 1].wayPointPosition.position);
                            }
                        }

                        Handles.Label(points[i].wayPointPosition.position, i.ToString(), EditorStyles.boldLabel);
                        Gizmos.color = editorSettings.wayPointsColor;
                        Gizmos.DrawSphere(points[i].wayPointPosition.position, editorSettings.wayPointsSize);

                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (Selection.objects.Length == 1)
            {
                //If you select the parent of the path follower, OnDrawGismozSelected is called,
                //so, if we do not write this line, you will be not able to select the parent because will be selected automaticaly the object with this component.
                //With this line we verify that the selected object is the PathFollower and not other object (Like the parent).
                if (Selection.activeTransform.GetComponent<PathFollower>() != null && Selection.activeGameObject.GetComponentsInChildren(typeof(PathFollower)).Length == 1)
                {
                    Selection.activeObject = this.gameObject;
                }
            }
        }

#endif
    }
}

namespace UGSSpace
{
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif
    public class GenerateObjectsByDistance : UGS_GenerateObjectsByDistance
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "LevelGenerator_Icon"; }
        }
#endif


#if UNITY_EDITOR

        void OnDrawGizmos()
        {
            if (limitToMove != null && limitToDestroyObjects != null)
            {
                Gizmos.color = Color.yellow / 1.2F;

                Gizmos.DrawSphere(transform.position, 0.3f);
                Gizmos.DrawSphere(limitToMove.position, 0.3f);
                Gizmos.DrawSphere(limitToDestroyObjects.position, 0.3f);

                Handles.color = Color.white / 1.2f;
                Handles.DrawDottedLine(limitToMove.position, limitToDestroyObjects.position, 1f);
                Handles.DrawDottedLine(transform.position, limitToMove.position, 1f);
            }

            if (limitToMove != null)
            {
                if (directionToGenerate == DirectionToGenerate.Right || directionToGenerate == DirectionToGenerate.Left)
                {
                    Handles.color = Color.green;
                    Handles.DrawDottedLine(new Vector3(limitToMove.position.x, limitToMove.position.y + 50, limitToMove.position.z), new Vector3(limitToMove.position.x, limitToMove.position.y - 50, limitToMove.position.z), 1f);
                    Handles.color = Color.red;
                    Handles.DrawDottedLine(new Vector3(limitToDestroyObjects.position.x, limitToDestroyObjects.position.y + 50, limitToDestroyObjects.position.z), new Vector3(limitToDestroyObjects.position.x, limitToDestroyObjects.position.y - 50, limitToDestroyObjects.position.z), 1f);
                }
                else
                {
                    Handles.color = Color.green;
                    Handles.DrawDottedLine(new Vector3(limitToMove.position.x + 50, limitToMove.position.y, limitToMove.position.z), new Vector3(limitToMove.position.x - 50, limitToMove.position.y, limitToMove.position.z), 1f);
                    Handles.color = Color.red;
                    Handles.DrawDottedLine(new Vector3(limitToDestroyObjects.position.x + 50, limitToDestroyObjects.position.y, limitToDestroyObjects.position.z), new Vector3(limitToDestroyObjects.position.x - 50, limitToDestroyObjects.position.y, limitToDestroyObjects.position.z), 1f);
                }

                Gizmos.color = Color.yellow;
                if (directionToGenerate == DirectionToGenerate.Right)
                {
                    Gizmos.DrawLine(new Vector3(limitToMove.position.x + 0.7f, limitToMove.position.y, limitToMove.position.z), new Vector3(limitToMove.position.x + 0.2f, limitToMove.position.y + 0.2f, limitToMove.position.z));
                    Gizmos.DrawLine(new Vector3(limitToMove.position.x + 0.7f, limitToMove.position.y, limitToMove.position.z), new Vector3(limitToMove.position.x + 0.2f, limitToMove.position.y - 0.2f, limitToMove.position.z));
                }
                else if (directionToGenerate == DirectionToGenerate.Left)
                {
                    Gizmos.DrawLine(new Vector3(limitToMove.position.x - 0.7f, limitToMove.position.y, limitToMove.position.z), new Vector3(limitToMove.position.x - 0.2f, limitToMove.position.y + 0.2f, limitToMove.position.z));
                    Gizmos.DrawLine(new Vector3(limitToMove.position.x - 0.7f, limitToMove.position.y, limitToMove.position.z), new Vector3(limitToMove.position.x - 0.2f, limitToMove.position.y - 0.2f, limitToMove.position.z));
                }
                else if (directionToGenerate == DirectionToGenerate.Up)
                {
                    Gizmos.DrawLine(new Vector3(limitToMove.position.x, limitToMove.position.y + 0.7f, limitToMove.position.z), new Vector3(limitToMove.position.x + 0.2f, limitToMove.position.y + 0.2f, limitToMove.position.z));
                    Gizmos.DrawLine(new Vector3(limitToMove.position.x, limitToMove.position.y + 0.7f, limitToMove.position.z), new Vector3(limitToMove.position.x - 0.2f, limitToMove.position.y + 0.2f, limitToMove.position.z));
                }
                else if (directionToGenerate == DirectionToGenerate.Down)
                {
                    Gizmos.DrawLine(new Vector3(limitToMove.position.x, limitToMove.position.y - 0.7f, limitToMove.position.z), new Vector3(limitToMove.position.x + 0.2f, limitToMove.position.y - 0.2f, limitToMove.position.z));
                    Gizmos.DrawLine(new Vector3(limitToMove.position.x, limitToMove.position.y - 0.7f, limitToMove.position.z), new Vector3(limitToMove.position.x - 0.2f, limitToMove.position.y - 0.2f, limitToMove.position.z));
                }
            }
        }
#endif

    }
}

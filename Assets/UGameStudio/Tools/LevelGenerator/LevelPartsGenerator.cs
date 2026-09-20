namespace UGSSpace
{
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using UnityEngine;

    public class LevelPartsGenerator : UGS_LevelPartsGenerator
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
            DrawGuides(defaultGuides);
        }

        public override void DrawGuides(LevelPart_Guides guides)
        {
            //Draw the cubes       
            Vector3 cubeSize = new Vector3(0.4f, 0.4f, 0.4f);
            Gizmos.color = Color.blue / 1.5f;
            Gizmos.DrawCube(guides.leftDownLimit.position, cubeSize);
            Gizmos.DrawCube(guides.leftUpLimit.position, cubeSize);
            Gizmos.DrawCube(guides.rightDownLimit.position, cubeSize);
            Gizmos.DrawCube(guides.rightUpLimit.position, cubeSize);

            //Draw the guide to continue (=)
            Gizmos.color = Color.white / 1.2f;
            if (directionToGenerate == DirectionToGenerate.Right || directionToGenerate == DirectionToGenerate.Left)
            {
                Gizmos.DrawCube(new Vector3(guides.guideToContinue.position.x, guides.guideToContinue.position.y + 0.1f, guides.guideToContinue.position.z), new Vector3(0.7f, 0.1f, 0.1f));
                Gizmos.DrawCube(new Vector3(guides.guideToContinue.position.x, guides.guideToContinue.position.y - 0.1f, guides.guideToContinue.position.z), new Vector3(0.7f, 0.1f, 0.1f));
            }
            else if (directionToGenerate == DirectionToGenerate.Up || directionToGenerate == DirectionToGenerate.Down)
            {
                Gizmos.DrawCube(new Vector3(guides.guideToContinue.position.x + 0.1f, guides.guideToContinue.position.y, guides.guideToContinue.position.z), new Vector3(0.1f, 0.7f, 0.1f));
                Gizmos.DrawCube(new Vector3(guides.guideToContinue.position.x - 0.1f, guides.guideToContinue.position.y, guides.guideToContinue.position.z), new Vector3(0.1f, 0.7f, 0.1f));
            }

            //Draw the dotted lines

            Handles.color = Color.white / 1.2f;

            Handles.DrawDottedLine(guides.leftUpLimit.position, guides.leftDownLimit.position, 1f);
            Handles.DrawDottedLine(guides.rightUpLimit.position, guides.rightDownLimit.position, 1f);

            Handles.DrawDottedLine(guides.leftUpLimit.position, guides.rightUpLimit.position, 1f);
            Handles.DrawDottedLine(guides.leftDownLimit.position, guides.rightDownLimit.position, 1f);

            if (directionToGenerate == DirectionToGenerate.Right)
            {
                Handles.DrawLine(guides.leftUpLimit.position,
                    new Vector3(guides.leftUpLimit.position.x + 5, guides.leftUpLimit.position.y, guides.leftUpLimit.position.z));

                Handles.DrawLine(guides.leftDownLimit.position,
                    new Vector3(guides.leftDownLimit.position.x + 5, guides.leftDownLimit.position.y, guides.leftDownLimit.position.z));

                Handles.DrawLine(guides.leftUpLimit.position, guides.leftDownLimit.position);
            }
            else if (directionToGenerate == DirectionToGenerate.Left)
            {
                Handles.DrawLine(guides.rightUpLimit.position,
                    new Vector3(guides.rightUpLimit.position.x - 5, guides.rightUpLimit.position.y, guides.rightUpLimit.position.z));

                Handles.DrawLine(guides.rightDownLimit.position,
                    new Vector3(guides.rightDownLimit.position.x - 5, guides.rightDownLimit.position.y, guides.rightDownLimit.position.z));

                Handles.DrawLine(guides.rightUpLimit.position, guides.rightDownLimit.position);

            }
            else if (directionToGenerate == DirectionToGenerate.Up)
            {
                Handles.DrawLine(guides.leftDownLimit.position,
                  new Vector3(guides.leftDownLimit.position.x, guides.leftDownLimit.position.y + 5, guides.leftDownLimit.position.z));

                Handles.DrawLine(guides.rightDownLimit.position,
                  new Vector3(guides.rightDownLimit.position.x, guides.rightDownLimit.position.y + 5, guides.rightDownLimit.position.z));

                Handles.DrawLine(guides.leftDownLimit.position, guides.rightDownLimit.position);
            }
            else if (directionToGenerate == DirectionToGenerate.Down)
            {
                Handles.DrawLine(guides.leftUpLimit.position,
                new Vector3(guides.leftUpLimit.position.x, guides.leftUpLimit.position.y - 5, guides.leftUpLimit.position.z));

                Handles.DrawLine(guides.rightUpLimit.position,
                  new Vector3(guides.rightUpLimit.position.x, guides.rightUpLimit.position.y - 5, guides.rightUpLimit.position.z));

                Handles.DrawLine(guides.leftUpLimit.position, guides.rightUpLimit.position);

            }

        }
#endif
    }
}

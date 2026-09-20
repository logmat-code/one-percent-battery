namespace UGSSpace
{
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using UnityEngine;

    public class LevelPart : UGS_LevelPart
    {
#if UNITY_EDITOR
        public override void DrawGuides(LevelPart_Guides guides)
        {
            Vector3 posTitle = new Vector3((guides.rightUpLimit.position.x - Vector3.Distance(guides.leftUpLimit.position, guides.rightUpLimit.position) / 2), guides.leftUpLimit.position.y + 1.5f, guides.leftUpLimit.position.z);
            if (levelGenerator.isEditing && !Application.isPlaying)
                Handles.Label(posTitle, "Edit Mode", EditorStyles.miniLabel);

            //Draw the cubes       

                Vector3 cubeSize = new Vector3(0.2f, 0.2f, 0.2f);
                Gizmos.color = Color.white / 1.3f;
                Gizmos.DrawCube(guides.leftDownLimit.position, cubeSize);
                Gizmos.DrawCube(guides.leftUpLimit.position, cubeSize);
                Gizmos.DrawCube(guides.rightDownLimit.position, cubeSize);
                Gizmos.DrawCube(guides.rightUpLimit.position, cubeSize);
            

            //Draw the guide to continue (=)
            Gizmos.color = Color.white / 1.2f;
            if (levelGenerator.directionToGenerate == UGS_LevelPartsGenerator.DirectionToGenerate.Right || levelGenerator.directionToGenerate == UGS_LevelPartsGenerator.DirectionToGenerate.Left)
            {
               
                    float distance_AxisY_guideToContinue;

                    if (levelGenerator.directionToGenerate == UGS_LevelPartsGenerator.DirectionToGenerate.Right)
                    {
                        guides.guideToContinue.position = guides.rightDownLimit.position;
                        distance_AxisY_guideToContinue = Vector3.Distance(levelGenerator.defaultGuides.guideToContinue.position, levelGenerator.defaultGuides.rightDownLimit.position);
                    }
                    else
                    {
                        guides.guideToContinue.position = guides.leftDownLimit.position;
                        distance_AxisY_guideToContinue = Vector3.Distance(levelGenerator.defaultGuides.guideToContinue.position, levelGenerator.defaultGuides.leftDownLimit.position);
                    }

                    Vector3 posGuideToContinue = new Vector3(guides.guideToContinue.position.x, guides.guideToContinue.position.y + distance_AxisY_guideToContinue, guides.guideToContinue.position.z);

                    Gizmos.DrawCube(new Vector3(guides.guideToContinue.position.x, posGuideToContinue.y + 0.1f, guides.guideToContinue.position.z), new Vector3(0.7f, 0.1f, 0.1f));
                    Gizmos.DrawCube(new Vector3(guides.guideToContinue.position.x, posGuideToContinue.y - 0.1f, guides.guideToContinue.position.z), new Vector3(0.7f, 0.1f, 0.1f));
                
            }
            else if (levelGenerator.directionToGenerate == UGS_LevelPartsGenerator.DirectionToGenerate.Up || levelGenerator.directionToGenerate == UGS_LevelPartsGenerator.DirectionToGenerate.Down)
            {
               
                    float distance_AxisX_guideToContinue;

                    if (levelGenerator.directionToGenerate == UGS_LevelPartsGenerator.DirectionToGenerate.Up)
                    {
                        guides.guideToContinue.position = guides.leftUpLimit.position;
                        distance_AxisX_guideToContinue = Vector3.Distance(levelGenerator.defaultGuides.guideToContinue.position, levelGenerator.defaultGuides.leftUpLimit.position);
                    }
                    else
                    {
                        guides.guideToContinue.position = guides.leftDownLimit.position;
                        distance_AxisX_guideToContinue = Vector3.Distance(levelGenerator.defaultGuides.guideToContinue.position, levelGenerator.defaultGuides.leftDownLimit.position);
                    }

                    Vector3 posGuideToContinue = new Vector3(guides.guideToContinue.position.x + distance_AxisX_guideToContinue, guides.guideToContinue.position.y, guides.guideToContinue.position.z);

                    Gizmos.DrawCube(new Vector3(posGuideToContinue.x + 0.1f, guides.guideToContinue.position.y, guides.guideToContinue.position.z), new Vector3(0.1f, 0.7f, 0.1f));
                    Gizmos.DrawCube(new Vector3(posGuideToContinue.x - 0.1f, guides.guideToContinue.position.y, guides.guideToContinue.position.z), new Vector3(0.1f, 0.7f, 0.1f));
                
            }

            //Draw the dotted lines

           
                //Assign the respective distances of the limits respect to the default guides.
            
                if (levelGenerator.directionToGenerate == UGS_LevelPartsGenerator.DirectionToGenerate.Right || levelGenerator.directionToGenerate == UGS_LevelPartsGenerator.DirectionToGenerate.Left)
                {
                    Vector3 posLimitLeftDown = new Vector3(guides.leftDownLimit.position.x, levelGenerator.defaultGuides.leftDownLimit.position.y, guides.leftDownLimit.position.z);
                    Vector3 posLimitRightDown = new Vector3(guides.rightDownLimit.position.x, levelGenerator.defaultGuides.rightDownLimit.position.y, guides.rightDownLimit.position.z);

                    Vector3 posLimitLeftUp = new Vector3(guides.leftUpLimit.position.x, levelGenerator.defaultGuides.leftUpLimit.position.y, guides.leftUpLimit.position.z);
                    Vector3 posLimitRightUp = new Vector3(guides.rightUpLimit.position.x, levelGenerator.defaultGuides.rightUpLimit.position.y, guides.rightUpLimit.position.z);

                    guides.leftDownLimit.position = posLimitLeftDown;
                    guides.rightDownLimit.position = posLimitRightDown;

                    guides.leftUpLimit.position = posLimitLeftUp;
                    guides.rightUpLimit.position = posLimitRightUp;
                }
                else
                {
                    Vector3 posLimitLeftDown = new Vector3(levelGenerator.defaultGuides.leftDownLimit.position.x, guides.leftDownLimit.position.y, guides.leftDownLimit.position.z);
                    Vector3 posLimitRightDown = new Vector3(levelGenerator.defaultGuides.rightDownLimit.position.x, guides.rightDownLimit.position.y, guides.rightDownLimit.position.z);

                    Vector3 posLimitLeftUp = new Vector3(levelGenerator.defaultGuides.leftUpLimit.position.x, guides.leftUpLimit.position.y, guides.leftUpLimit.position.z);
                    Vector3 posLimitRightUp = new Vector3(levelGenerator.defaultGuides.rightUpLimit.position.x, guides.rightUpLimit.position.y, guides.rightUpLimit.position.z);

                    guides.leftDownLimit.position = posLimitLeftDown;
                    guides.rightDownLimit.position = posLimitRightDown;

                    guides.leftUpLimit.position = posLimitLeftUp;
                    guides.rightUpLimit.position = posLimitRightUp;
                }
            

            Handles.color = Color.cyan / 1.3f;

            Handles.DrawDottedLine(guides.leftUpLimit.position, guides.leftDownLimit.position, 1f);
            Handles.DrawDottedLine(guides.rightUpLimit.position, guides.rightDownLimit.position, 1f);

            Handles.DrawDottedLine(guides.leftUpLimit.position, guides.rightUpLimit.position, 1f);
            Handles.DrawDottedLine(guides.leftDownLimit.position, guides.rightDownLimit.position, 1f);

            if (levelGenerator.directionToGenerate == UGS_LevelPartsGenerator.DirectionToGenerate.Right)
            {
                Handles.DrawLine(guides.leftUpLimit.position,
                    new Vector3(guides.leftUpLimit.position.x + 5, guides.leftUpLimit.position.y, guides.leftUpLimit.position.z));

                Handles.DrawLine(guides.leftDownLimit.position,
                    new Vector3(guides.leftDownLimit.position.x + 5, guides.leftDownLimit.position.y, guides.leftDownLimit.position.z));

                Handles.DrawLine(guides.leftUpLimit.position, guides.leftDownLimit.position);
            }
            else if (levelGenerator.directionToGenerate == UGS_LevelPartsGenerator.DirectionToGenerate.Left)
            {
                Handles.DrawLine(guides.rightUpLimit.position,
                    new Vector3(guides.rightUpLimit.position.x - 5, guides.rightUpLimit.position.y, guides.rightUpLimit.position.z));

                Handles.DrawLine(guides.rightDownLimit.position,
                    new Vector3(guides.rightDownLimit.position.x - 5, guides.rightDownLimit.position.y, guides.rightDownLimit.position.z));

                Handles.DrawLine(guides.rightUpLimit.position, guides.rightDownLimit.position);

            }
            else if (levelGenerator.directionToGenerate == UGS_LevelPartsGenerator.DirectionToGenerate.Up)
            {
                Handles.DrawLine(guides.leftDownLimit.position,
                  new Vector3(guides.leftDownLimit.position.x, guides.leftDownLimit.position.y + 5, guides.leftDownLimit.position.z));

                Handles.DrawLine(guides.rightDownLimit.position,
                  new Vector3(guides.rightDownLimit.position.x, guides.rightDownLimit.position.y + 5, guides.rightDownLimit.position.z));

                Handles.DrawLine(guides.leftDownLimit.position, guides.rightDownLimit.position);
            }
            else if (levelGenerator.directionToGenerate == UGS_LevelPartsGenerator.DirectionToGenerate.Down)
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

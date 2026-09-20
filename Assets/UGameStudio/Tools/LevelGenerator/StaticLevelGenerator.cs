namespace UGSSpace {
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class StaticLevelGenerator : UGS_StaticLevelGenerator
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

    void DrawGuides(StaticLevelPart_Guides guides)
    {
        if (guides != null)
        {
            Vector3 posTitle = new Vector3((guides.rightUpLimit.position.x - Vector3.Distance(guides.leftUpLimit.position, guides.rightUpLimit.position) / 2), guides.leftUpLimit.position.y + 1.5f, guides.leftUpLimit.position.z);
            if (isEditing && !Application.isPlaying)
                Handles.Label(posTitle, "Edit Mode", EditorStyles.miniLabel);

            //Draw the cubes       

            Vector3 cubeSize = new Vector3(0.4f, 0.4f, 0.4f);
            Gizmos.color = Color.blue / 1.5f;
            Gizmos.DrawCube(guides.leftDownLimit.position, cubeSize);
            Gizmos.DrawCube(guides.leftUpLimit.position, cubeSize);
            Gizmos.DrawCube(guides.rightDownLimit.position, cubeSize);
            Gizmos.DrawCube(guides.rightUpLimit.position, cubeSize);

            Gizmos.color = Color.white / 1.2f;

            //Draw the dotted lines

            if (isEditing && !Application.isPlaying)
                Handles.color = Color.cyan / 1.3f;
            else
                Handles.color = Color.white / 1.2f;

            Handles.DrawDottedLine(guides.leftUpLimit.position, guides.leftDownLimit.position, 1f);
            Handles.DrawDottedLine(guides.rightUpLimit.position, guides.rightDownLimit.position, 1f);

            Handles.DrawDottedLine(guides.leftUpLimit.position, guides.rightUpLimit.position, 1f);
            Handles.DrawDottedLine(guides.leftDownLimit.position, guides.rightDownLimit.position, 1f);
        }
    }
#endif
}
}

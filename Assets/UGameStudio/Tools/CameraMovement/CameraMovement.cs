namespace UGSSpace
{
    using UnityEngine;
#if UNITY_EDITOR
    using UnityEditor;
#endif

    public class CameraMovement : UGS_CameraMovement
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "CameraMovement_Icon"; }
        }
#endif

#if UNITY_EDITOR

        void Reset()
        {
            thisCamera = GetComponent<Camera>();
        }

        void OnDrawGizmos()
        {
            if (thisCamera == null)
                thisCamera = GetComponent<Camera>();

            if (showGismozEver)
            {
                DrawTheGizmos();
            }
        }

        void OnDrawGizmosSelected()
        {
            if (thisCamera == null)
                thisCamera = GetComponent<Camera>();

            if (!showGismozEver)
            {
                DrawTheGizmos();
            }
        }

        void DrawTheGizmos()
        {
            //Camera edges
            camUpperLeft = thisCamera.ScreenToWorldPoint(new Vector3(0, thisCamera.pixelHeight, thisCamera.nearClipPlane));
            camUpperRight = thisCamera.ScreenToWorldPoint(new Vector3(thisCamera.pixelWidth, thisCamera.pixelHeight, thisCamera.nearClipPlane));
            camBelowRight = thisCamera.ScreenToWorldPoint(new Vector3(thisCamera.pixelWidth, 0, thisCamera.nearClipPlane));
            camBelowLeft = thisCamera.ScreenToWorldPoint(new Vector3(0, 0, thisCamera.nearClipPlane));

            if (enableInstantMovementLimits)
            {
                Handles.color = new Color(0.7f, 0.0f, 0, 1f);

                //========================================================
                //Horizontal Lines for limits of movement
                Vector3 endPositionToGoUp = new Vector3(camUpperRight.x, positionToGoUp.position.y, positionToGoUp.position.z);
                Vector3 endPositionToGoDown = new Vector3(camUpperRight.x, positionToGoDown.position.y, positionToGoDown.position.z);

                Handles.DrawDottedLine(new Vector3(camUpperLeft.x, positionToGoUp.position.y, positionToGoUp.position.z), endPositionToGoUp, 2.5f);
                Handles.DrawDottedLine(new Vector3(camUpperLeft.x, positionToGoDown.position.y, positionToGoDown.position.z), endPositionToGoDown, 2.5f);

                //Vertical Lines for limits of movement
                Vector3 endPositionToGoLeft = new Vector3(positionToGoLeft.position.x, camBelowLeft.y, positionToGoLeft.position.z);
                Vector3 endPositionToGoRight = new Vector3(positionToGoRight.position.x, camBelowLeft.y, positionToGoRight.position.z);

                Handles.DrawDottedLine(new Vector3(positionToGoLeft.position.x, camUpperLeft.y, positionToGoLeft.position.z), endPositionToGoLeft, 2.5f);
                Handles.DrawDottedLine(new Vector3(positionToGoRight.position.x, camUpperLeft.y, positionToGoRight.position.z), endPositionToGoRight, 2.5f);
                //========================================================
            }

            //For smooth movement
            if (enableSmoothMovement)
            {
                Handles.color = new Color(0.1f, 0.3f, 1, 1f);
                Handles.DrawLine(positionToGoUpSmoothly.position, positionToGoRightSmoothly.position);
                Handles.DrawLine(positionToGoRightSmoothly.position, positionToGoDownSmoothly.position);
                Handles.DrawLine(positionToGoDownSmoothly.position, positionToGoLeftSmoothly.position);
                Handles.DrawLine(positionToGoLeftSmoothly.position, positionToGoUpSmoothly.position);
                //========================================================
            }

            //CAMERA LIMITS
            Handles.color = new Vector4(0, 0.8f, 0, 1f);
            //LIMIT UP LINE
            if (!disableCameraUpperLimit)
                Handles.DrawLine(new Vector3(limitCamPositionLeft.position.x, limitCamPositionUp.position.y, limitCamPositionUp.position.z),
                    new Vector3(limitCamPositionRight.position.x, limitCamPositionUp.position.y, limitCamPositionUp.position.z));

            //LIMIT DOWN LINE
            if (!disableCameraLowerLimit)
                Handles.DrawLine(new Vector3(limitCamPositionRight.position.x, limitCamPositionDown.position.y, limitCamPositionDown.position.z),
                new Vector3(limitCamPositionLeft.position.x, limitCamPositionDown.position.y, limitCamPositionDown.position.z));

            //LIMIT LEFT LINE
            if (!disableCameraLeftLimit)
                Handles.DrawLine(new Vector3(limitCamPositionLeft.position.x, limitCamPositionDown.position.y, limitCamPositionLeft.position.z),
                new Vector3(limitCamPositionLeft.position.x, limitCamPositionUp.position.y, limitCamPositionLeft.position.z));

            //LIMIT RIGHT LINE
            if (!disableCameraRightLimit)
                Handles.DrawLine(new Vector3(limitCamPositionRight.position.x, limitCamPositionDown.position.y, limitCamPositionRight.position.z),
                new Vector3(limitCamPositionRight.position.x, limitCamPositionUp.position.y, limitCamPositionRight.position.z));

            //If disable some camera limit, show Extension of the line and the labels with the infinite symbol
            //=================================================================================================
            if (disableCameraLeftLimit && !disableCameraLowerLimit)
            {
                Handles.color = new Vector4(1, 0.8f, 0, 1f);
                Handles.Label(new Vector3(limitCamPositionLeft.position.x - 11, limitCamPositionLeft.position.y, limitCamPositionLeft.position.z), "-∞", EditorStyles.whiteBoldLabel);

                //Extended dotted line
                Handles.DrawDottedLine(new Vector3(limitCamPositionLeft.position.x, limitCamPositionLeft.position.y, limitCamPositionLeft.position.z),
                new Vector3(limitCamPositionLeft.position.x - 10, limitCamPositionLeft.position.y, limitCamPositionLeft.position.z), 0.2f);

                //Lines of arrows
                Handles.DrawLine(new Vector3(limitCamPositionLeft.position.x - 9.5f, limitCamPositionLeft.position.y - 0.2f, limitCamPositionLeft.position.z),
                new Vector3(limitCamPositionLeft.position.x - 10, limitCamPositionLeft.position.y, limitCamPositionLeft.position.z));

                Handles.DrawLine(new Vector3(limitCamPositionLeft.position.x - 9.5f, limitCamPositionLeft.position.y + 0.2f, limitCamPositionLeft.position.z),
                new Vector3(limitCamPositionLeft.position.x - 10, limitCamPositionLeft.position.y, limitCamPositionLeft.position.z));
            }

            if (disableCameraLeftLimit && !disableCameraUpperLimit)
            {
                Handles.color = new Vector4(1, 0.8f, 0, 1f);
                Handles.Label(new Vector3(limitCamPositionUp.position.x - 11, limitCamPositionUp.position.y, limitCamPositionUp.position.z), "-∞", EditorStyles.whiteBoldLabel);

                //Extended dotted line
                Handles.DrawDottedLine(new Vector3(limitCamPositionUp.position.x, limitCamPositionUp.position.y, limitCamPositionUp.position.z),
                new Vector3(limitCamPositionUp.position.x - 10, limitCamPositionUp.position.y, limitCamPositionUp.position.z), 0.2f);

                //Lines of arrows
                Handles.DrawLine(new Vector3(limitCamPositionUp.position.x - 9.5f, limitCamPositionUp.position.y - 0.2f, limitCamPositionUp.position.z),
                new Vector3(limitCamPositionUp.position.x - 10, limitCamPositionUp.position.y, limitCamPositionUp.position.z));

                Handles.DrawLine(new Vector3(limitCamPositionUp.position.x - 9.5f, limitCamPositionUp.position.y + 0.2f, limitCamPositionUp.position.z),
                new Vector3(limitCamPositionUp.position.x - 10, limitCamPositionUp.position.y, limitCamPositionUp.position.z));
            }

            if (disableCameraRightLimit && !disableCameraUpperLimit)
            {
                Handles.color = new Vector4(1, 0.8f, 0, 1f);
                Handles.Label(new Vector3(limitCamPositionRight.position.x + 10, limitCamPositionRight.position.y, limitCamPositionRight.position.z), "+∞", EditorStyles.whiteBoldLabel);

                //Extended dotted line
                Handles.DrawDottedLine(new Vector3(limitCamPositionRight.position.x, limitCamPositionRight.position.y, limitCamPositionRight.position.z),
                new Vector3(limitCamPositionRight.position.x + 10, limitCamPositionRight.position.y, limitCamPositionRight.position.z), 0.2f);

                //Lines of arrows
                Handles.DrawLine(new Vector3(limitCamPositionRight.position.x + 9.5f, limitCamPositionRight.position.y - 0.2f, limitCamPositionRight.position.z),
                new Vector3(limitCamPositionRight.position.x + 10, limitCamPositionRight.position.y, limitCamPositionRight.position.z));

                Handles.DrawLine(new Vector3(limitCamPositionRight.position.x + 9.5f, limitCamPositionRight.position.y + 0.2f, limitCamPositionRight.position.z),
                new Vector3(limitCamPositionRight.position.x + 10, limitCamPositionRight.position.y, limitCamPositionRight.position.z));
            }


            if (disableCameraRightLimit && !disableCameraLowerLimit)
            {
                Handles.color = new Vector4(1, 0.8f, 0, 1f);
                Handles.Label(new Vector3(limitCamPositionDown.position.x + 10, limitCamPositionDown.position.y, limitCamPositionDown.position.z), "+∞", EditorStyles.whiteBoldLabel);

                //Extended dotted line
                Handles.DrawDottedLine(new Vector3(limitCamPositionDown.position.x, limitCamPositionDown.position.y, limitCamPositionDown.position.z),
                new Vector3(limitCamPositionDown.position.x + 10, limitCamPositionDown.position.y, limitCamPositionDown.position.z), 0.2f);

                //Lines of arrows
                Handles.DrawLine(new Vector3(limitCamPositionDown.position.x + 9.5f, limitCamPositionDown.position.y - 0.2f, limitCamPositionDown.position.z),
                new Vector3(limitCamPositionDown.position.x + 10, limitCamPositionDown.position.y, limitCamPositionDown.position.z));

                Handles.DrawLine(new Vector3(limitCamPositionDown.position.x + 9.5f, limitCamPositionDown.position.y + 0.2f, limitCamPositionDown.position.z),
                new Vector3(limitCamPositionDown.position.x + 10, limitCamPositionDown.position.y, limitCamPositionDown.position.z));
            }

            if (disableCameraUpperLimit && !disableCameraLeftLimit)
            {
                Handles.color = new Vector4(1, 0.8f, 0, 1f);
                Handles.Label(new Vector3(limitCamPositionUp.position.x, limitCamPositionUp.position.y + 11, limitCamPositionUp.position.z), "+∞", EditorStyles.whiteBoldLabel);

                //Extended dotted line
                Handles.DrawDottedLine(new Vector3(limitCamPositionUp.position.x, limitCamPositionUp.position.y, limitCamPositionUp.position.z),
                new Vector3(limitCamPositionUp.position.x, limitCamPositionUp.position.y + 10, limitCamPositionUp.position.z), 0.2f);

                //Lines of arrows
                Handles.DrawLine(new Vector3(limitCamPositionUp.position.x - 0.2f, limitCamPositionUp.position.y + 9.5f, limitCamPositionUp.position.z),
                new Vector3(limitCamPositionUp.position.x, limitCamPositionUp.position.y + 10, limitCamPositionUp.position.z));

                Handles.DrawLine(new Vector3(limitCamPositionUp.position.x + 0.2f, limitCamPositionUp.position.y + 9.5f, limitCamPositionUp.position.z),
                new Vector3(limitCamPositionUp.position.x, limitCamPositionUp.position.y + 10, limitCamPositionUp.position.z));
            }

            if (disableCameraUpperLimit && !disableCameraRightLimit)
            {
                Handles.color = new Vector4(1, 0.8f, 0, 1f);
                Handles.Label(new Vector3(limitCamPositionRight.position.x, limitCamPositionRight.position.y + 11, limitCamPositionRight.position.z), "+∞", EditorStyles.whiteBoldLabel);

                //Extended dotted line
                Handles.DrawDottedLine(new Vector3(limitCamPositionRight.position.x, limitCamPositionRight.position.y, limitCamPositionRight.position.z),
                new Vector3(limitCamPositionRight.position.x, limitCamPositionRight.position.y + 10, limitCamPositionRight.position.z), 0.2f);

                //Lines of arrows
                Handles.DrawLine(new Vector3(limitCamPositionRight.position.x - 0.2f, limitCamPositionRight.position.y + 9.5f, limitCamPositionRight.position.z),
                new Vector3(limitCamPositionRight.position.x, limitCamPositionRight.position.y + 10, limitCamPositionRight.position.z));

                Handles.DrawLine(new Vector3(limitCamPositionRight.position.x + 0.2f, limitCamPositionRight.position.y + 9.5f, limitCamPositionRight.position.z),
                new Vector3(limitCamPositionRight.position.x, limitCamPositionRight.position.y + 10, limitCamPositionRight.position.z));
            }

            if (disableCameraLowerLimit && !disableCameraRightLimit)
            {
                Handles.color = new Vector4(1, 0.8f, 0, 1f);
                Handles.Label(new Vector3(limitCamPositionDown.position.x, limitCamPositionDown.position.y - 11, limitCamPositionDown.position.z), "-∞", EditorStyles.whiteBoldLabel);

                //Extended dotted line
                Handles.DrawDottedLine(new Vector3(limitCamPositionDown.position.x, limitCamPositionDown.position.y, limitCamPositionDown.position.z),
                new Vector3(limitCamPositionDown.position.x, limitCamPositionDown.position.y - 10, limitCamPositionDown.position.z), 0.2f);

                //Lines of arrows
                Handles.DrawLine(new Vector3(limitCamPositionDown.position.x - 0.2f, limitCamPositionDown.position.y - 9.5f, limitCamPositionDown.position.z),
                new Vector3(limitCamPositionDown.position.x, limitCamPositionDown.position.y - 10, limitCamPositionDown.position.z));

                Handles.DrawLine(new Vector3(limitCamPositionDown.position.x + 0.2f, limitCamPositionDown.position.y - 9.5f, limitCamPositionDown.position.z),
                new Vector3(limitCamPositionDown.position.x, limitCamPositionDown.position.y - 10, limitCamPositionDown.position.z));
            }

            if (disableCameraLowerLimit && !disableCameraLeftLimit)
            {
                Handles.color = new Vector4(1, 0.8f, 0, 1f);
                Handles.Label(new Vector3(limitCamPositionLeft.position.x, limitCamPositionLeft.position.y - 11, limitCamPositionLeft.position.z), "-∞", EditorStyles.whiteBoldLabel);

                //Extended dotted line
                Handles.DrawDottedLine(new Vector3(limitCamPositionLeft.position.x, limitCamPositionLeft.position.y, limitCamPositionLeft.position.z),
                new Vector3(limitCamPositionLeft.position.x, limitCamPositionLeft.position.y - 10, limitCamPositionLeft.position.z), 0.2f);

                //Lines of arrows
                Handles.DrawLine(new Vector3(limitCamPositionLeft.position.x - 0.2f, limitCamPositionLeft.position.y - 9.5f, limitCamPositionLeft.position.z),
                new Vector3(limitCamPositionLeft.position.x, limitCamPositionLeft.position.y - 10, limitCamPositionLeft.position.z));

                Handles.DrawLine(new Vector3(limitCamPositionLeft.position.x + 0.2f, limitCamPositionLeft.position.y - 9.5f, limitCamPositionLeft.position.z),
                new Vector3(limitCamPositionLeft.position.x, limitCamPositionLeft.position.y - 10, limitCamPositionLeft.position.z));
            }

            //=================================================================================================
        }

#endif
    }
}

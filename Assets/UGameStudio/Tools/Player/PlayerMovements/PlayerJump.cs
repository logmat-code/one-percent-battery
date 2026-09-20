namespace UGSSpace
{
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using UnityEngine;

    public class PlayerJump : UGS_PlayerJump
    {
#if UNITY_EDITOR
        private void Reset()
        {
            groundMask = 1 << LayerMask.NameToLayer("Ground");
        }

        void OnDrawGizmosSelected()
        {
            if (isGroundDetectorRequired)
            {
                if (groundChecker != null)
                {
                    Handles.color = Color.cyan;
                    Handles.DrawWireDisc(groundChecker.position, groundChecker.forward, radiusOfGroundChecker);
                }
            }
        }
#endif
    }
}

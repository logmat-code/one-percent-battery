namespace UGSSpace
{
    namespace UGameActions
    {
        using System;
        using System.Collections;

        [Serializable]
        [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
        public class UGSAction_Testing_PauseUnityEditor : UGSActionNormal
        {
            protected override void Action()
            {
                PublicEditorTools.PauseEditor();
            }

            public override UGSAction Copy()
            {
                UGSAction_Testing_PauseUnityEditor ugsAction = (UGSAction_Testing_PauseUnityEditor)this.MemberwiseClone();
                return ugsAction;
            }

            public override void StartReactiveBehavior()
            {
            }

            public override void StopReactiveBehavior()
            {   
            }                
        }
    }
}

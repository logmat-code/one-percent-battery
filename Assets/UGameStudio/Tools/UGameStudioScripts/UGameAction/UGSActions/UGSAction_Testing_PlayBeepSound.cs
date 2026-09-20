namespace UGSSpace
{
    namespace UGameActions
    {
        using System;
        using System.Collections;

        [Serializable]
        [UnityEngine.Scripting.APIUpdating.MovedFrom(true, null, "Assembly-CSharp", null)]
        public class UGSAction_Testing_PlayBeepSound : UGSActionNormal
        {
            protected override void Action()
            {
                PublicEditorTools.PlayBeep();
            }

            public override UGSAction Copy()
            {
                UGSAction_Testing_PlayBeepSound ugsAction = (UGSAction_Testing_PlayBeepSound)this.MemberwiseClone();
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

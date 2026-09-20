namespace UGSSpace
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    [RequireComponent(typeof(Button))]
    public class UGS_ResetAchievementsButton : MonoBehaviour
    {
        public virtual void ResetAchievements() { }
    }
}
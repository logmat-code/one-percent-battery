namespace UGSSpace
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    [RequireComponent(typeof(Button))]
    public class UGS_ShowYodo1AdsOnButtonPress : MonoBehaviour
    {
        public enum IfAdIsNotAvailable { DoNothing, DisableThisObject, DisableAnotherObject, EnableAnotherObjectWithButtonPress }
        public int placementID;
        public Yodo1AdsManager.BannerAction bannerAction;

        //This method saves the final goods that will receive the player when complete the video (saves the goods passed as a parameter in showRewardedVideo() to give them when the video finishes).
        public List<Yodo1AdsManager.GoodForVideoRewards> goodsToGetOnCompleteVideo = new List<Yodo1AdsManager.GoodForVideoRewards>(1) { new Yodo1AdsManager.GoodForVideoRewards() };

        public bool showAdWithFrequencyOfAdsManager = true;
        public IfAdIsNotAvailable ifAdIsNotAvailable = IfAdIsNotAvailable.DoNothing;
        public GameObject objectToDisable;
    }
}

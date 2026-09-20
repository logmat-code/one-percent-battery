namespace UGSSpace
{
    using System.Collections.Generic;
    using UGameStudioGDK;
    using UnityEngine;
    using UnityEngine.UI;

    public class Yodo1AdsManager : MonoBehaviour
#if UNITY_EDITOR
    , IHierarchyIcon
#endif
    {
#if UNITY_EDITOR
        public string EditorIconPath
        {
            get { return "ads_icon"; }
        }
#endif
        [System.Serializable]
        public class GoodForVideoRewards
        {
            public enum Type { Score, MultiplyTheScoreObtainedInCurrentLevel, Weapon, ProjectilesForWeapon, CostumeItem, PlayerCharacter, Health, Lives, PowerUps }
            public Type type = Type.PlayerCharacter;
            public int productID;
            public int costumeCategory;
            public float floatQuantity;
            public int intQuantity;
            public UGSSpace.UGameStudioGDK.Enums.PowerUpsEnum powerUpType = UGSSpace.UGameStudioGDK.Enums.PowerUpsEnum.PowerToFly;
        }

        public enum BannerAction { Show, Hide }

        [System.Serializable]
        public class AdsWithGameActionSettings
        {
            public int placementID = 0;

            public BannerAction bannerAction = BannerAction.Show;

            //This list is passed as a parameter to ShowRewardedVideo() to save goods in other field (specificGoodsToGetOnCompleteVideo) in AdsManagerForUnityAds to be excecuted when the video finishes. (If you do not understand, continue reading the code :c)
            public List<GoodForVideoRewards> goodsToGetOnCompleteVideo = new List<GoodForVideoRewards>(1) { new GoodForVideoRewards() };

            public List<PlayerStateCondition> conditionsToShowAds = new List<PlayerStateCondition>(1) { new PlayerStateCondition(47) }; //47 is PlayerHasFinishedTheLevel

            public int frequencyToShowInterstitial = 1;
        }

        [System.Serializable]
        public class WindowForRewards
        {
            public GameObject canvas;
            public GameObject window;
            public Button closeButton;
        }

        public static int frequencyToShowInterstitial;
        public List<AdsWithGameActionSettings> adsWithGameAction = new List<AdsWithGameActionSettings>(1) { new AdsWithGameActionSettings() };

        public bool showWindowWhenRewardedVideoFinish = true;
        public WindowForRewards windowForRewards = new WindowForRewards();
        public Utilities.UIEffects.EffectFields effectFields = new Utilities.UIEffects.EffectFields();
    }
}
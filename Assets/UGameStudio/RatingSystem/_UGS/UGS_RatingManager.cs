namespace UGSSpace
{
    using System;
    using System.Collections;
    using UGameStudioGDK;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.UI;

    public class UGS_RatingManager : MonoBehaviour
    {
        [Serializable]
        public class WindowElements
        {
            public Button star1Btn;
            public Button star2Btn;
            public Button star3Btn;
            public Button star4Btn;
            public Button star5Btn;
            public Sprite starImageWhenEnabled;
            public Sprite starImageWhenDisabled;
            public Color starColorWhenEnabled = Color.white;
            public Color starColorWhenDisabled = new Color(0, 0, 0, 0.35f);
            public Button rateNowBtn;
            public Button rateLaterBtn;
            public Button neverShowAgainBtn;
        }

        public enum When { WhenThePlayerWinsALevel, WhenThePlayerLosesALevel, WhenPlayerWinsOrLosesALevel }
        public enum AndroidStores { GooglePlayStore, AmazonAppstore, Other }
        public GameObject ratingCanvas;
        public GameObject ratingWindow;

        public When whenToShow = When.WhenThePlayerWinsALevel;

        //Conditions to show
        public bool showOnlyWhenPlayerHasInternetConnection = true;
        public int playsRequiredToShow = 3;
        public When whenToCountPlays = When.WhenPlayerWinsOrLosesALevel;
        public float minimumTimePlayedToShow = 150; //Time in seconds
        public bool resetTimePlayedIfRateLater = true;

        public AndroidStores androidStore = AndroidStores.GooglePlayStore;
        public string androidURL = "com.MyCompanyName.MyGameName";
        public bool showIOSInGameReviewPopup = true;
        public string appStoreAppID = "000000000";
        public string urlForOtherStores = "https://www.ugamestudio.com";

        public int minimumRatingToOpenURL = 5;
        public bool requestFeedbackIfRatingIsLow = true;
        public GameObject windowToRequestFeedback;
        public Button confirmationBtn;
        public Button closeBtn;

        public string email = "your@email.com";
        public string email_title = "Comments about your game";
        public string email_message = "Tell us what you didn't like: ";
        public bool includeGivenRatingAtTheEnd = true;
        public string givenRatingMsg = "My rating was: ";

        public Utilities.UIEffects.EffectFields effectFields = new Utilities.UIEffects.EffectFields();

        public WindowElements windowElements = new WindowElements();
    }
}
namespace UGSSpace
{
    using System.Text;
    using UnityEngine;
    using System.Collections;
    using System;
    using UnityEngine.SceneManagement;
    using UGameStudioGDK;
    using UnityEngine.UI;

    public class UGS_GPSavedGamesManager : MonoBehaviour
    {
        public static UGS_GPSavedGamesManager instance;
        public enum HowToLogin { LoginAutomatically, LoginWithButton }
        public enum HowToLoad { LoadItAutomatically, AskThePlayerToLoadOrContinue }

        //-------------------------------------------------------------------

        public HowToLogin howToLogin = HowToLogin.LoginAutomatically;
        public HowToLoad howToLoad = HowToLoad.AskThePlayerToLoadOrContinue;
        public GameObject canvas;
        public GameObject window;
        public Button buttonToLoadTheGame;
        public Button buttonToIgnoreAndContinue;

        public enum WhenToSave { SaveWhenChangingScenesAndWhenClosingTheGame, SaveOnlyWhenClosingTheGame }
        public WhenToSave whenToSave = WhenToSave.SaveWhenChangingScenesAndWhenClosingTheGame;

        public Utilities.UIEffects.EffectFields effectFields = new Utilities.UIEffects.EffectFields();
    }
}
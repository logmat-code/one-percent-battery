namespace UGSSpace
{
    using System;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.UI;
    using UGameStudioGDK;

    public class UGS_iCloudSaveManager : MonoBehaviour
    {
        public enum HowToLoad { LoadItAutomatically, AskThePlayerToLoadOrContinue }
        public HowToLoad howToLoad = HowToLoad.AskThePlayerToLoadOrContinue;
        public GameObject canvas;
        public GameObject window;
        public Button buttonToLoadTheGame;
        public Button buttonToIgnoreAndContinue;

        public enum WhenToSave { SaveWhenTheSceneChangesAndWhenTheGameIsClosed, SaveOnlyWhenTheGameIsClosed }
        public WhenToSave whenToSave = WhenToSave.SaveWhenTheSceneChangesAndWhenTheGameIsClosed;

        public Utilities.UIEffects.EffectFields effectFields = new Utilities.UIEffects.EffectFields();
    }
}
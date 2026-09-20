namespace UGSSpace
{
    using System.Collections;
    using UGameStudioGDK;
    using UnityEngine;
    using UnityEngine.UI;
    using System.IO;

    [RequireComponent(typeof(Button))]
    public class UGS_ShareInSocialMedia_Button : MonoBehaviour
    {
        public string screenshotName = "screenshot";
        public bool showScoreObtainedInThisLevel = true;
        public int scoreIndex;
        public string textToShareInAndroid = "I got ";
        public string textToShareInAndroid2 = " of score in this game, try to beat me!, Download it at: http://my_app_link.com";
        public string textToShareInIOS = "I got ";
        public string textToShareInIOS2 = " of score in this game, try to beat me!, Download it at: http://my_app_link.com";
    }
}
namespace UGSSpace
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using System.Collections;

    [RequireComponent(typeof(Button))]
    public class UGS_UGameStudioIAPButton : MonoBehaviour
    {
        public enum BtnActionEnum { PurchaseProduct, RestorePurchases }
        public BtnActionEnum _btnAction = BtnActionEnum.PurchaseProduct;

        public bool disableButtonOnUnsupportedPlatforms = true; //If RestorePurchases

        public string textToShowOnRestore = "Transactions restored succesfully.";
        public string textToShowOnFailRestore = "The transactions have not been loaded, maybe you have nothing to restore. Otherwise, please try again.";

        public int productID_number;

        public bool showSpriteOnUIImage; //MOSTRAR IMAGEN ASIGNADA DEL PRODUCTO EN EL EDITOR
        public Image uiImageToShowSprite;

        public bool showTitleFromStore = true;
        public Text textToShowTheTitle;

        public bool showDescriptionFromStore = true;
        public Text textToShowTheDescription;

        public bool showPriceFromStore = true;
        public Text textToShowThePrice;

        public bool showResultText = true;
        public GameObject objectToEnableToShowResult;
        public Text textToShowResult;
    }
}

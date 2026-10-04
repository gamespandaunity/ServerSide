using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Cricket;


namespace Cricket
{

    public class Popups : Singleton<Popups>
    {

        public Button[] buttons;

    
        public Sprite[] earnedItemSprite;

        private string[] earnedItemText = new string[2] { "74", "172" };

        public Text topBarTitle;

        public Text content;

        public Text button1Text;

        public Text button2Text;

        public GameObject holder;

        public GameObject innerHolder;

        public GameObject innerContent;

        public GameObject BG;

        

        public static string insuffStatus = string.Empty;

       
        private void ResetAnim()
        {
            innerHolder.transform.DOLocalMoveY(650f, 0f);
            BG.transform.DOScaleY(0f, 0f).SetUpdate(isIndependentUpdate: true);
            innerContent.transform.DOLocalMoveX(-950f, 0f);
        }

       

        private void StartAnim()
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Insert(0f, innerHolder.transform.DOLocalMoveY(-10f, 0.4f));
            sequence.Insert(0f, BG.transform.DOScaleY(1f, 0.35f));
            sequence.Insert(0.25f, innerContent.transform.DOLocalMoveX(0f, 0.25f));
            sequence.Insert(0.4f, innerHolder.transform.DOLocalMoveY(0f, 0.1f));
            sequence.SetUpdate(isIndependentUpdate: true);
        }

        public void ShowMe()
        {
            CONTROLLER.tempPageName = CONTROLLER.pageName;
            CONTROLLER.pageName = "popup";
            innerHolder.transform.DOLocalMoveY(650f, 0f);
            StartAnim();
            holder.SetActive(value: true);
            buttons[0].onClick.RemoveAllListeners();
            buttons[1].onClick.RemoveAllListeners();
            if (CONTROLLER.PopupName == "incompletePopup1")
            {
                buttons[0].gameObject.SetActive(value: true);
                buttons[1].gameObject.SetActive(value: true);
                buttons[0].onClick.AddListener(delegate
                {
                    Singleton<IncompleteMatch>.instance.YesButton();
                });
                buttons[1].onClick.AddListener(delegate
                {
                    Singleton<IncompleteMatch>.instance.NoButton();
                });
                topBarTitle.text = LocalizationData.localizationInstance.getText(115);
                content.text = LocalizationData.localizationInstance.getText(133);
                button1Text.text = LocalizationData.localizationInstance.getText(164);
                button2Text.text = LocalizationData.localizationInstance.getText(165);
            }
            else if (CONTROLLER.PopupName == "incompletePopup2")
            {
                buttons[0].gameObject.SetActive(value: true);
                buttons[1].gameObject.SetActive(value: true);
                buttons[0].onClick.AddListener(delegate
                {
                    Singleton<IncompleteMatchTWO>.instance.YesButton();
                });
                buttons[1].onClick.AddListener(delegate
                {
                    Singleton<IncompleteMatchTWO>.instance.NoButton();
                });
                topBarTitle.text = LocalizationData.localizationInstance.getText(402);
                content.text = LocalizationData.localizationInstance.getText(134) + " " + LocalizationData.localizationInstance.getText(135);
                button1Text.text = LocalizationData.localizationInstance.getText(164);
                button2Text.text = LocalizationData.localizationInstance.getText(166);
            }
            else if (CONTROLLER.PopupName == "toss")
            {
                buttons[0].gameObject.SetActive(value: true);
                buttons[1].gameObject.SetActive(value: true);
                buttons[0].onClick.AddListener(delegate
                {
                    Singleton<TossPageTWO>.instance.RVSelected(1);
                });
                buttons[1].onClick.AddListener(delegate
                {
                    Singleton<TossPageTWO>.instance.RVSelected(0);
                });
                topBarTitle.text = LocalizationData.localizationInstance.getText(117);
                content.text = LocalizationData.localizationInstance.getText(136);
                button1Text.text = LocalizationData.localizationInstance.getText(164);
                button2Text.text = LocalizationData.localizationInstance.getText(166);
            }
           
            else if (CONTROLLER.PopupName == "squadWarning")
            {
                buttons[1].gameObject.SetActive(value: true);
                buttons[0].onClick.AddListener(delegate
                {
                    Singleton<WarningTossTWO>.instance.YesButton();
                });
                buttons[1].onClick.AddListener(delegate
                {
                    Singleton<WarningTossTWO>.instance.NoButton();
                });
                topBarTitle.text = LocalizationData.localizationInstance.getText(119);
                content.text = LocalizationData.localizationInstance.getText(138) + " " + LocalizationData.localizationInstance.getText(139);
                button1Text.text = LocalizationData.localizationInstance.getText(164);
                button2Text.text = LocalizationData.localizationInstance.getText(166);
            }
            else if (CONTROLLER.PopupName == "editPopup")
            {
                buttons[0].gameObject.SetActive(value: true);
                buttons[1].gameObject.SetActive(value: true);
                buttons[0].onClick.AddListener(delegate
                {
                    Singleton<WarningEditPlayersTWO>.instance.YesButton();
                });
                buttons[1].onClick.AddListener(delegate
                {
                    Singleton<WarningEditPlayersTWO>.instance.NoButton();
                });
                topBarTitle.text = LocalizationData.localizationInstance.getText(120);
                content.text = LocalizationData.localizationInstance.getText(140);
                button1Text.text = LocalizationData.localizationInstance.getText(164);
                button2Text.text = LocalizationData.localizationInstance.getText(166);
            }
            else if (CONTROLLER.PopupName == "errorPopup")
            {
                buttons[0].gameObject.SetActive(value: false);
                buttons[1].onClick.AddListener(delegate
                {
                    HideMe();
                });
                topBarTitle.text = string.Empty;
                content.text = Singleton<ErrorPopupTWO>.instance.ErrorTxt.text;
                button2Text.text = LocalizationData.localizationInstance.getText(167);
            }
           
            
            //Singleton<PowerUps>.instance.ResetDetails();
            if (ManageScene.activeSceneName() == "MainMenu")
            {
                Singleton<GameModeTWO>.instance.ResetDetails();
            }
        }

        //public void Setholderactive()
        //{
        //    holder.SetActive(value: true);
        //}

        public void HideMe()
        {
            CONTROLLER.pageName = CONTROLLER.tempPageName;
            if (ManageScene.activeSceneName() == "MainMenu" && Singleton<GameModeTWO>.instance.Holder.activeInHierarchy)
            {
                //Singleton<AdIntegrate>.instance.HideAd();
            }
            ResetAnim();
            holder.SetActive(value: false);
        }
    }

}
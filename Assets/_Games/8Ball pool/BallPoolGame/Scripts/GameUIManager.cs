using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;
    public static int betAmount;
    private void Start()
    {
        if (instance == null)
        {
            instance = this;
            //DontDestroyOnLoad(gameObject);
        }

        SaveSoundValue();

        Time.timeScale = 1;
       // betInputField.onValueChange.AddListener(SaveBetAmount);
    }

    //public void SaveBetAmount(string sv)
    //{
    //    string amount = sv;
    //    betAmount = Convert.ToInt32(amount);
    //    //print(betAmount + "saving bet amount");
    //}

    public bool sound;

    public GameObject soundTick;
    public GameObject soundunTick;

    public static bool isAi;
    public void SaveSoundValue()
    {
        if (PlayerPrefs.GetInt("Sound") == 1)
        {
            soundunTick.SetActive(false);
            SoundManager.instance.clickSound.mute = false;
            soundTick.SetActive(true);

            sound = true;

        }
        else
        {
            soundTick.SetActive(false);
            SoundManager.instance.clickSound.mute = true;
            soundunTick.SetActive(true);

            sound = false;
        }
    }


    public void SoundBtn()
    {
        if (sound)
        {
            // off
            soundTick.SetActive(false);
            SoundManager.instance.clickSound.mute = true;
            soundunTick.SetActive(true);

            sound = false;

            PlayerPrefs.SetInt("Sound", 0);

        }
        else
        {
            SoundManager.instance.ClickSoundPlay();
            // on
            soundunTick.SetActive(false);
            SoundManager.instance.clickSound.mute = false;
            soundTick.SetActive(true);

            sound = true;

            PlayerPrefs.SetInt("Sound", 1);
        }
    }


   

    public GameObject modeSelection;
    public GameObject settingPanelHome;
    public GameObject exitGamePanel;



    //RAR
    public GameObject aiBetPanel;
    public InputField aiBetTxt;
    public Button aiBetNextBtn;


    public void SettingBtn()
    {
        settingPanelHome.SetActive(true);
        SoundManager.instance.ClickSoundPlay();

        isSettingPanel = true;
    }

    public void SettingCloseBtn()
    {
        settingPanelHome.SetActive(false);
        SoundManager.instance.ClickSoundPlay();
    }

    public void ExitBtn()
    {
        exitGamePanel.SetActive(true);
        SoundManager.instance.ClickSoundPlay();

        isExitPanel = true;
    }

    public void ExitYesBtn()
    {
        SceneLoaderUtility.LoadScene("MainmenuScene");
        SoundManager.instance.ClickSoundPlay();
    }

    public void ExitNoBtn()
    {
        exitGamePanel.SetActive(false);
        SoundManager.instance.ClickSoundPlay();
    }

    public void PlayWithPCBtn()
    {
        aiBetPanel.SetActive(true);
        SoundManager.instance.ClickSoundPlay();
    }


    public void BetPanelBackBtn()
    {
        aiBetPanel.SetActive(false);
        SoundManager.instance.ClickSoundPlay();
    }


    public void AIBetNextBtn()
    {
        // ApiAndRoomManager._instance.isreplaywithAI = false;

        SceneLoaderUtility.LoadScene("GamePlayScene");

        //  ApiAndRoomManager.gameid = "2";
        // ApiAndRoomManager._instance.silvercoins = aiBetTxt.text;
        isAi = true;
        betAmount = int.Parse(aiBetTxt.text);
        //print(betAmount + "saving bet amount");

      //  ApiAndRoomManager._instance.putBetforAIGame();

        SoundManager.instance.ClickSoundPlay();

    }



    public void PlayWithFriend()
    {
        SceneLoaderUtility.LoadScene("home");
        SoundManager.instance.ClickSoundPlay();
        isAi = false;
    }
    public void BackBtnFromModeSelection()
    {
        modeSelection.SetActive(false);
      
        SoundManager.instance.ClickSoundPlay();
    }



    
    public bool isMainSelectionPanel;
    public bool isSettingPanel;
    public bool isExitPanel;


    private void Update()
    {

        if (aiBetTxt.text != "" && int.Parse(aiBetTxt.text) >= 20)
        {
            aiBetNextBtn.interactable = true;
        }
        else
        {
            aiBetNextBtn.interactable = false;
        }





        /* if (Input.GetKeyDown(KeyCode.Escape)) //RAR
         {
             if (isMainSelectionPanel == true)
             {
                 modeSelection.SetActive(false);



                 isMainSelectionPanel = false;
             }
             else if (isSettingPanel == true)
             {
                 settingPanelHome.SetActive(false);



                 isSettingPanel = false;
             }
             else if (isExitPanel == true)
             {
                 exitGamePanel.SetActive(false);



                 isExitPanel = false;
             }*/
        /*else if (CarromLoadingManager.isHomePanel == true)
        {
            exitGamePanel.SetActive(true);
            homePanel.SetActive(false);


            CarromLoadingManager.isHomePanel = true;
            }
        }*/
    }
    public void SetGameMode(int modeId)
    {
        staticVariables.isgoldcoins = false;
        ApiAndRoomManager.currentGameId = modeId;
        isAi = false;

        SceneLoaderUtility.LoadScene("PlaywithFriendSilverCoinsScene");

        //print("test multi silver coins");
    }

    public void SetgoldGameMode(int modeId)
    {

        staticVariables.isgoldcoins = true;
        isAi = false;
        ApiAndRoomManager.currentGameId = modeId;

        SceneLoaderUtility.LoadScene("PlaywithFriendSilverCoinsScene");


        //print("test gold coins");
    }

}

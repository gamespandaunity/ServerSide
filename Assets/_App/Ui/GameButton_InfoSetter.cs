using NetworkManagement;
 
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using TeenPattiGame;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameButton_InfoSetter : MonoBehaviour
{
    public Image gameLogo;
    public TextMeshProUGUI gameName;
    public RoomsCounterText GameChallengestxt;
    public int game_id;
    public string fileUrl, category;
    game_detail game_Detail;
    //public GameObject silverButton, GoldButton;
    public GameObject DownloadPanel;
    public GameObject pauseButton, downloadButton,silverButtonParent,goldButtonParent;
    public Image LoadingSprite;

    AssetReference currentSceneName;
    private bool isDownloading;
    public void init(game_detail game_Detail, GamesSetterMenu gamesSetterMenu)
    {
        this.game_Detail = game_Detail;
        game_id = game_Detail._id;
        fileUrl = game_Detail.file_url;
        gameName.text = game_Detail.title;
        category = game_Detail.category;
        GamesSetterMenu.All_games_Ref.Add(this);
        if (GamesSetterMenu.All_games_Ref.Count >= GamesSetterMenu.totalgamescount)
        {
            gamesSetterMenu.RefreshMenuButons();
        }
        //game_id.Show();
        if (game_id == 5 || game_id == 9 || game_id == 10)
        {
            //  gameName.text.Show();
            GameChallengestxt.GoldChallengestxt.text = "Gold";
            GameChallengestxt.SilverChallengestxt.text = "Silver";
        }

        switch (game_id)
        {
            case 7:

                CheckForAddressables(AddressablesBundleSpawning.instance.twelveOfflineddressableSceneName);
                currentSceneName = AddressablesBundleSpawning.instance.twelveOfflineddressableSceneName;
                break;

            case 11:
                CheckForAddressables(AddressablesBundleSpawning.instance.horseAddressableSceneName);
                currentSceneName = AddressablesBundleSpawning.instance.horseAddressableSceneName;
                break;
            case 12:
                CheckForAddressables(AddressablesBundleSpawning.instance.carAddressableSceneName);
                currentSceneName = AddressablesBundleSpawning.instance.carAddressableSceneName;
                break;
            // case 13:
            //     CheckForAddressables(AddressablesBundleSpawning.instance.cricketMenuAddressableSceneName);
            //     currentSceneName = AddressablesBundleSpawning.instance.cricketGroundAddressableSceneName;
            //     break;
        }
    }
    public void OnEnable()
    {
        if (staticVariables.isGuest)
        {
            switch (game_id)
            {
                case 7:

                    CheckForAddressables(AddressablesBundleSpawning.instance.twelveOfflineddressableSceneName);
                    currentSceneName = AddressablesBundleSpawning.instance.twelveOfflineddressableSceneName;
                    break;

                case 11:
                    CheckForAddressables(AddressablesBundleSpawning.instance.horseAddressableSceneName);
                    currentSceneName = AddressablesBundleSpawning.instance.horseAddressableSceneName;
                    break;
                case 12:
                    CheckForAddressables(AddressablesBundleSpawning.instance.carAddressableSceneName);
                    currentSceneName = AddressablesBundleSpawning.instance.carAddressableSceneName;
                     break;
                // case 13:
                //     CheckForAddressables(AddressablesBundleSpawning.instance.cricketMenuAddressableSceneName);
                //     currentSceneName = AddressablesBundleSpawning.instance.cricketGroundAddressableSceneName;
                //     break;
            }
        }
        goldButtonParent.SetActive( staticVariables.gameFeatures.isGoldCoins);

    }

   
   

    public void CheckForAddressables(AssetReference sceneName)
    {
        AddressablesBundleSpawning.instance.CheckSceneStatus(sceneName, isDownloaded =>
        {
            if (isDownloaded)
            {
                ////Debug.Log("Scene is already downloaded.");
                // Proceed to load the scene directly if cached
                // LoadAddressableScene(sceneName);
                DownloadPanel.SetActive(false);
                if (isDownloading == false)
                    this.transform.SetAsFirstSibling();
            }
            else
            {
                //Debug.Log("Scene is not downloaded. Starting download...");
                // Start download or handle accordingly
                // StartCoroutine(DownloadAndLoadScene(sceneName));
                downloadButton.SetActive(true);
                pauseButton.gameObject.SetActive(false);
                DownloadPanel.SetActive(true);
                LoadingSprite.enabled = false;

                this.transform.SetAsLastSibling();
            }
        });
    }
    private AsyncOperationHandle<SceneInstance> handle;

    public void DownloadScene()
    {

        pauseButton.gameObject.SetActive(true);
        downloadButton.SetActive(false);
        isDownloading = true;
        //AddressablesBundleSpawning.instance.downloadProgressSlider = LoadingSprite;
        StartCoroutine(DownloadAndLoadScene(isDownloaded1 =>
        {
            if (isDownloaded1)
            {
                //Debug.Log("Scene is already downloaded.");
                // Proceed to load the scene directly if cached
                // LoadAddressableScene(sceneName);
                DownloadPanel.SetActive(false);
                //if (isDownloading == false)
                //    this.transform.SetAsFirstSibling();
            }
            else
            {
                //Debug.Log("Scene is not downloaded. Starting download...");
                // Start download or handle accordingly
                // StartCoroutine(DownloadAndLoadScene(sceneName));
                DownloadPanel.SetActive(true);
                pauseButton.gameObject.SetActive(false);
                downloadButton.SetActive(true);
                LoadingSprite.enabled = false;
                //this.transform.SetAsLastSibling();
            }
        }));

    }

    public void Pause()
    {

        DownloadPanel.SetActive(true);
        pauseButton.gameObject.SetActive(false);
        downloadButton.SetActive(true);
        //LoadingSprite.enabled = false;
        StopAllCoroutines();
        handle.Release();
        Addressables.Release(handle);

    }
    private IEnumerator DownloadAndLoadScene(Action<bool> onDownloadedCallback)
    {
        LoadingSprite.enabled = true;
        if (game_id == 13)
        {
            for (int i = 0; i < 2; i++)
            {
                if (i == 0)
                {
               // handle = Addressables.LoadAssetAsync<SceneInstance>(AddressablesBundleSpawning.instance.cricketMenuAddressableSceneName);

                }
                else
                {
               // handle = Addressables.LoadAssetAsync<SceneInstance>(AddressablesBundleSpawning.instance.cricketGroundAddressableSceneName);
                }
                while (!handle.IsDone)
                {
                    float percent = handle.GetDownloadStatus().Percent;
                    LoadingSprite.fillAmount = percent;
                    //downloadProgressText.text = $"Downloading: {(percent * 100).ToString("F2")}%";
                    yield return null;
                }

                if (handle.PercentComplete == 1)
                {
                    //downloadProgressText.text = "Download Complete!";
                    LoadingSprite.enabled = false;
                    onDownloadedCallback.Invoke(true);
                }
                else
                {
                    LoadingSprite.enabled = true;
                    ConstantsData_M.Log("Failed to download or load the scene.");
                    onDownloadedCallback.Invoke(false);
                }
            }
        }
        else
        {
            handle = Addressables.LoadAssetAsync<SceneInstance>(currentSceneName);
            while (!handle.IsDone)
            {
                float percent = handle.GetDownloadStatus().Percent;
                LoadingSprite.fillAmount = percent;
                //downloadProgressText.text = $"Downloading: {(percent * 100).ToString("F2")}%";
                yield return null;
            }

            if (handle.PercentComplete == 1)
            {
                //downloadProgressText.text = "Download Complete!";
                LoadingSprite.enabled = false;
                onDownloadedCallback.Invoke(true);
            }
            else
            {
                LoadingSprite.enabled = true;
                ConstantsData_M.Log("Failed to download or load the scene.");
                onDownloadedCallback.Invoke(false);
            }
        }
    }

    #region Button Function

    public void SilverGameBtn()
    {
        if (staticVariables.isGuest)
        {
            PopupMessageManager.instance.ShowConfirmPanel(body: "Please Sign In to continue", () =>
            {
                SceneManager.LoadSceneAsync(1);
                staticVariables.ResetGuestStatus();
            }, yesButtonText: "SIGN IN");
        }
        else
        {
            ApiAndRoomManager.currentGameId = game_id;
            ConstantsData_M.Log($"{game_id}");
            staticVariables.isgoldcoins = false;
            HomeMenuManager.instance.createChallengePanelParent.SetActive(false);
            if (Borderspanel.coinSelected != null)
            {
                Borderspanel.coinSelected(staticVariables.isgoldcoins);
            }
            AightBallPoolNetworkGameAdapter.is3DGraphics = false;
            staticVariables.directSupport = "";
            if (game_id == 5 || game_id == 9 || game_id == 10)
            {
                if (Borderspanel.ChangeTitle != null)
                {

                    Borderspanel.ChangeTitle("SELECT MODE");
                }
                GameModeManager.IsCoinTypeSelected = true;

                openPanelByName("PlayWithScreen");
            }
            else
            {
                openPanelByName("FriendRequest");
                if (Borderspanel.logoHandler != null)
                    Borderspanel.logoHandler(true);
                if (Borderspanel.ChangeTitle != null)
                {
                    Borderspanel.ChangeTitle("SILVER CHALLENGES");
                }
                SocketIOUnityAdapter.instance.RenderPublicRoomTables();
            }
        }
        SoundManagerMain.instance.ClickSoundPlay();
    }

    public void GoldGameBtn()
    {
        if (staticVariables.isGuest)
        {
            PopupMessageManager.instance.ShowConfirmPanel(body: "Please Sign In to continue", () =>
            {
                SceneManager.LoadSceneAsync(1);
                staticVariables.ResetGuestStatus();
            }, yesButtonText: "SIGN IN");
        }
        else
        {
            ApiAndRoomManager.currentGameId = game_id;
            staticVariables.isgoldcoins = true;
            HomeMenuManager.instance.createChallengePanelParent.SetActive(false);
            if (Borderspanel.coinSelected != null)
            {
                Borderspanel.coinSelected(staticVariables.isgoldcoins);
            }
            AightBallPoolNetworkGameAdapter.is3DGraphics = false;
            staticVariables.directSupport = "";
            if (game_id == 5 || game_id == 9 || game_id == 10)
            {
                if (Borderspanel.ChangeTitle != null)
                {

                    Borderspanel.ChangeTitle("SELECT MODE");
                }
                GameModeManager.IsCoinTypeSelected = true;
                openPanelByName("PlayWithScreen");
            }
            else
            {
                if (Borderspanel.logoHandler != null)
                    Borderspanel.logoHandler(true);
                //ScreenNavigotor_Custom.OpenScreen("FriendRequest");
                openPanelByName("FriendRequest");
                if (Borderspanel.ChangeTitle != null)
                {
                    Borderspanel.ChangeTitle("GOLD CHALLENGES");
                }
            }
            SocketIOUnityAdapter.instance.RenderPublicRoomTables();
        }
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public void GameBtn()
    {
        staticVariables.directSupport = "";
        ApiAndRoomManager.currentGameId = game_id;
        //Constants_M.Log("Game ID " + game_id);
        if (Borderspanel.ChangeTitle != null)
        {

            Borderspanel.ChangeTitle("SELECT MODE");
        }
        // ScreenNavigotor_Custom.OpenScreen("PlayWithScreen");
        openPanelByName("PlayWithScreen");
        // SceneLoaderUtility.LoadScene("Start");
        if (Borderspanel.logoHandler != null)
            Borderspanel.logoHandler(true);
        SoundManagerMain.instance.ClickSoundPlay();

    }
    #endregion
    #region small challenge game button
    public void SilverGoldSelect(bool isGold)// function on  buttons
    {
        ApiAndRoomManager.currentGameId = game_id;
        ConstantsData_M.Log("game id " + ApiAndRoomManager.currentGameId + " - " + game_id);
        staticVariables.isgoldcoins = isGold;
        AightBallPoolNetworkGameAdapter.is3DGraphics = false;
        if (Borderspanel.coinSelected != null)
        {
            Borderspanel.coinSelected(staticVariables.isgoldcoins);
            ConstantsData_M.Log(staticVariables.isgoldcoins + "=>-----------------------");
        }
        if (Borderspanel.ChangeTitle != null)
        {
            Borderspanel.ChangeTitle("Create Challenge");
        }
        SoundManagerMain.instance.ClickSoundPlay();
        Invoke("CallDelegate", .2f);
    }
    public void CallDelegate()
    {
        if (SmallChallenge.SilverGoldChallenge_Selected != null)
        {
            SmallChallenge.SilverGoldChallenge_Selected();
        }
    }

    public void OnSelectingGame()
    {

        ConstantsData_M.Log("game id " + " - " + game_id);
        ApiAndRoomManager.currentGameId = game_id;
        ConstantsData_M.Log("game id " + ApiAndRoomManager.currentGameId + " - " + game_id);
        AightBallPoolNetworkGameAdapter.is3DGraphics = false;
        SoundManagerMain.instance.ClickSoundPlay();
        if (SmallChallenge.GameSelect != null)
        {
            SmallChallenge.GameSelect();
        }
    }
    #endregion
    public static void openPanelByName(string str)
    {
        foreach (GameObject gb in ScreenNavigotor_Custom.Instance.ScreenGameObjects)
        {
            gb.SetActive(gb.name.Equals(str));
        }
    }
    public void Updatefeatures()
    {
        goldButtonParent.SetActive(staticVariables.gameFeatures.isGoldCoins);

    }
}

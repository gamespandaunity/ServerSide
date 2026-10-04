using Extensions.Unity.ImageLoader;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GamesSetterMenu : MonoBehaviour
{

    public Button CasinoButton, OtherButton;
    public static List<GameButton_InfoSetter> All_games_Ref = new List<GameButton_InfoSetter>();
    public GameObject gamesHolder;
    public bool fromDirectInvite = false;
    public GameObject HeaderObj;
    public static int totalgamescount = 0;

    private void Start()
    {
        if (staticVariables.isGuest)
        {
            
        }
        else
        {
            SpawnGamesButton();
        }
    }
    private void OnEnable()
    {
        if (staticVariables.isGuest)
        {
            if (HeaderObj != null)
            {
                HeaderObj.SetActive(false);
            }
            var objs = gamesHolder.GetComponentsInChildren<GameButton_InfoSetter>();
            All_games_Ref = objs.ToList<GameButton_InfoSetter>();
        }
        else
        {
            if (HeaderObj != null)
            {
                HeaderObj.SetActive(false);
            }
            GamesInfo();
        }

        ApiAndRoomManager._instance.ModifyUserBalance();
    }
    public void GamesInfo()// INITIATING GAMES FROM SERVER
    {
            SocketIOUnityAdapter.instance.RenderPublicRoomTables();

        ApiAndRoomManager._instance.FetchGameDetails(OnSuccess =>
        {
            if (PlayerPrefs.GetString("GamesData_M") != OnSuccess)
            {
                //Constants_M.Log("yahan agya");
                PlayerPrefs.SetString("GamesData_M", OnSuccess);
                SpawnGamesButton();
               
            }
        }
   );


    }

    public void SpawnGamesButton()
    {
        All_games_Ref.Clear();
        gamesHolder.transform.Clear();
        if (PlayerPrefs.HasKey("GamesData_M"))
        {
            //Constants_M.Log("Bado Badi Bado Badi");
            string OnSuccess = PlayerPrefs.GetString("GamesData_M");
            GameDetailParent detail = JsonUtility.FromJson<GameDetailParent>(OnSuccess);
            if (detail.status)
            {
                staticVariables.lotteryGamesList = new List<game_detail>();
                totalgamescount = detail.game_detail.Count;
                for (int i = 0; i < detail.game_detail.Count; i++)
                {
                    switch (detail.game_detail[i].category)
                    {
                        case "Games":
                            staticVariables.lotteryGamesList.Add(detail.game_detail[i]);
                            GameButtonInit(detail.game_detail[i]);
                            break;

                    }
                }
            }
        }
    }
    public void LotteryGamesSet(string gameType)
    {
        foreach (GameButton_InfoSetter gm in All_games_Ref) gm.gameObject.SetActive(true);
        //GameButton_InfoSetter[] category = All_games_Ref.FindAll(x => x.category == gameType).ToArray();
        //foreach (GameButton_InfoSetter gm in category) gm.gameObject.SetActive(true);
    }
    public void RefreshMenuButons()
    {
        OtherButton.onClick.Invoke();
    }
    public void GameButtonInit(game_detail detail)
    {
        GameObject obj;
        obj = fromDirectInvite ? Resources.Load<GameObject>("SmallChallengeGameButton") : Resources.Load<GameObject>("GameButton");
        if (fromDirectInvite)
        {
            if (detail.title != "roulette")
            {
                InstantiateButton(detail, obj);
            }
            OtherButton.onClick.Invoke();
        }
        else
        {

            InstantiateButton(detail, obj);
        }
    }

    private void InstantiateButton(game_detail detail, GameObject obj)
    {
        GameObject game = Instantiate(obj, gamesHolder.transform);
        game.GetComponent<GameButton_InfoSetter>().init(detail, this);
        GetGamesLogo(detail, game.GetComponent<GameButton_InfoSetter>().gameLogo);
    }

    public async void GetGamesLogo(game_detail fileUrl, Image gameLogo)
    {
        if (SpritesManager.Instance.spritesScriptable.downloadedGamePics[fileUrl.game_id] == null)
        {
            string url = SplitUrl(fileUrl.file_url);
            await ImageLoader.LoadSprite(ServerConnection.Main_URL() + url).Consume(gameLogo);
            //ServerConnection.DownloadSprite(url
            //     , DownloadedTexture =>
            //     {
            //         try
            //         {
            //             gameLogo.sprite = ConstantsData_M.ConvertTextureToSprite(DownloadedTexture);
            //             SpritesManager.Instance.spritesScriptable.downloadedGamePics[fileUrl.game_id] = ConstantsData_M.ConvertTextureToSprite(DownloadedTexture);
            //         }
            //         catch
            //         {

            //         }
            //     },
            //       Onfailed =>
            //       {
            //           //Debug.Log("Failed To Load Image Main menu" + url + " with " + Onfailed);
            //       }
            //      );
        }
        else
        {
            gameLogo.sprite = SpritesManager.Instance.spritesScriptable.downloadedGamePics[fileUrl.game_id];
        }
    }
    public string SplitUrl(string str)
    {
        if (str.Contains("public"))
        {

            string[] splittedform = str.Split("public");
            return splittedform[1];
        }
        else
        {
            return str;
        }
    }

    private void OnDisable()
    {
        if (staticVariables.isGuest)
        {
            if (HeaderObj != null)
            {

                HeaderObj.SetActive(true);
            }
        }
        else
        {
            if (HeaderObj != null)
            {

                HeaderObj.SetActive(true);
            }
          
        }
    }



}

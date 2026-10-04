using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using UnityEngine.UIElements.Experimental;
using static HistoryGameButton_data;
//using SimpleJSON;

public class GameHistoryManager : MonoBehaviour
{
    public static GameHistoryManager instance;
    public bool isSelectedAll = false;
    public GameObject gameHistoryInfo;
    public GameObject gameHistoryTemplate, teenPattiTemplate;
    public List<Gamehistory> history = new List<Gamehistory>();
    public List<Gamehistory> IncomingData = new List<Gamehistory>();
    public HistoryManager historyManager;
    public TMP_InputField RangeDateFrom, RangeDateTo;
    public Sprite GoldCoinIcon, SilverCoinIcon;
    string dateFormat = "MM/dd/yyyy";
    public int Gameid = 1, CurrentPage = 1;
    public GameObject buttonHolder;
    public RoundHistoryManager RoundsHistory;
    public Text StatusTORound;
    public string localjson;

   
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

    }
    private void Start()
    {
        RangeDateFrom.onValueChanged.AddListener(FetchHistory);
        RangeDateTo.onValueChanged.AddListener(FetchHistory);
    }
    public void OnEnable()
    {

        FetchHistory();
        isSelectedAll = false;
        historyManager.firstBtn.onClick.AddListener(LoadFirstPage);
        historyManager.lastBtn.onClick.AddListener(LoadLastPage);
        historyManager.nextBtn.onClick.AddListener(LoadNextPage);
        historyManager.previousBtn.onClick.AddListener(LoadPreviousPage);
        GamesInfo();


    }
    public void ChangeText()
    {
        ConstantsData_M.Log("CHANGE TEXT");
        StatusTORound.text = "Rounds";
    }
    public void ChangeTextToStatus()
    {
        ConstantsData_M.Log("CHANGE REVERT TEXT");
        StatusTORound.text = "Status";
    }
    public void GamesInfo()// INITIATING GAMES FROM SERVER
    {
        GameObject gb = Resources.Load<GameObject>("HistoryGameButton");
        if (staticVariables.lotteryGamesList == null)
        {
            ConstantsData_M.Log("STATIC LISTS NULL");
            ApiAndRoomManager._instance.FetchGameDetails(OnSuccess =>
            {
                GameDetailParent detail = JsonUtility.FromJson<GameDetailParent>(OnSuccess);
                if (detail.status)
                {
                    for (int i = 0; i < detail.game_detail.Count; i++)
                    {
                        GameObject obj = Instantiate(gb, buttonHolder.transform);
                        obj.GetComponent<HistoryGameButton_data>().Init(detail.game_detail[i]);
                    }
                }
            });
        }
        else
        {
            ConstantsData_M.Log("STATIC LISTS NOT NULL");
            for (int i = 0; i < staticVariables.lotteryGamesList.Count; i++)
            {
                GameObject obj = Instantiate(gb, buttonHolder.transform);
                obj.GetComponent<HistoryGameButton_data>().Init(staticVariables.lotteryGamesList[i]);
            }
           
        }
    }
    private void OnDisable()
    {
        for (int i = 0; i < buttonHolder.transform.childCount; i++)
        {
            Destroy(buttonHolder.transform.GetChild(i).gameObject);

        }

        historyManager.firstBtn.onClick.RemoveAllListeners();
        historyManager.lastBtn.onClick.RemoveAllListeners();
        historyManager.nextBtn.onClick.RemoveAllListeners();
        historyManager.previousBtn.onClick.RemoveAllListeners();

    }
    void FetchHistory(string VAL = "")
    {

        string PagniteQuery = $"?game_id={Gameid}&page={CurrentPage}&perPage=20&start_date={RangeDateFrom.text}&end_date={RangeDateTo.text}";
        if (gameObject.activeInHierarchy)
            StartCoroutine(ServerConnection.GetApiRequest(ServerConnection.GameHistoryUrl() + PagniteQuery, OnSuccess =>
            {
                if (localjson != OnSuccess)
                {
                    localjson = OnSuccess;
                    for (int i = 0; i < gameHistoryInfo.transform.childCount; i++)
                    {
                        Destroy(gameHistoryInfo.transform.GetChild(i).gameObject);
                    }
                    history.Clear();
                    try
                    {

                        GameHistoryObj gameHistoryObj = JsonUtility.FromJson<GameHistoryObj>(OnSuccess);
                        history = gameHistoryObj.data;
                        StartCoroutine(DisplayFilterFriends(gameHistoryObj.data));

                        historyManager.currentPage.text = gameHistoryObj.currentPage;
                        historyManager.lastPage.text = gameHistoryObj.totalPages;
                    }
                    catch (Exception e)
                    {

                    }
                }
            }));
    }
    public void GamesHistoryWIthID(int id)
    {
        Gameid = id;
        FetchHistory();
        SoundManagerMain.instance.ClickSoundPlay();

    }
    public void LoadFirstPage()
    {
        CurrentPage = 1;
        FetchHistory();
    }
    public void LoadNextPage()
    {
        if (CurrentPage < int.Parse(historyManager.lastPage.text))
        {
            CurrentPage++;
            FetchHistory();
        }
    }
    public void LoadPreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            FetchHistory();
        }
    }
    public void LoadLastPage()
    {
        CurrentPage = int.Parse(historyManager.lastPage.text);
        FetchHistory();
    }
    public IEnumerator DisplayFilterFriends(List<Gamehistory> data)
    {
        yield return new WaitUntil(() => gameHistoryInfo.transform.childCount == 0);
        for (int i = 0; i < data.Count; i++)
        {
            string category = GamesSetterMenu.All_games_Ref.Find(x => x.game_id == Gameid).category;
            //if (category.Contains(APIManager.casino)) { if (data[i].rounds_played == "0") continue; }
            GameObject gameObjectManager = Instantiate( gameHistoryTemplate, gameHistoryInfo.transform);
           
            {
                gameObjectManager.GetComponent<SingleGameHistory>().Init(data[i]);
                StatusTORound.text = "Status";
            }

        }
    }
    public void ReturnBtn()
    {
        SoundManagerMain.instance.ClickSoundPlay();
    }

    public void selectGameSelection(int gameNumber)
    {

        isSelectedAll = false;
        for (int i = 0; i < gameHistoryInfo.transform.childCount; i++)
        {
            Destroy(gameHistoryInfo.transform.GetChild(i).gameObject);
        }
        ApiAndRoomManager._instance.ChallengesHistory(OnSuccess =>
        {

            GameHistoryObj gameHistoryObj = JsonUtility.FromJson<GameHistoryObj>(OnSuccess);
            //  currentPage.text = gameHistoryObj.currentPage;
            // lastPage.text = gameHistoryObj.totalPages.ToString();

            ConstantsData_M.Log("game history" + gameHistoryObj.data.Count);
            for (int i = 0; i < gameHistoryObj.data.Count; i++)
            {
                DateTime dateTime = DateTime.Parse(gameHistoryObj.data
                    [i].updatedAt.ToString());
                string formattedDateTime = dateTime.ToString("dd-MMM-yy hh:mmtt");
                GameObject gameObjectManager = Instantiate(gameHistoryTemplate, gameHistoryInfo.transform);
                gameObjectManager.GetComponent<SingleGameHistory>().gameName.text = gameHistoryObj.data[i].title;
                gameObjectManager.GetComponent<SingleGameHistory>().serialNumber.text = gameHistoryObj.data[i].transaction_id;
                gameObjectManager.GetComponent<SingleGameHistory>().winlost.text = gameHistoryObj.data[i].status;
                gameObjectManager.GetComponent<SingleGameHistory>().coinsAmount.text = gameHistoryObj.data[i].user_coins.ToString();
            }

        });
        SoundManagerMain.instance.ClickSoundPlay();
    }
}

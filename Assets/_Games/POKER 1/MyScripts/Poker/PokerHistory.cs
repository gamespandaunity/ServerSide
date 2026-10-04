namespace POKER 
{
    using Mirror;
    //
    //using Photon.Pun;
    using POKER;
using System;
using System.Collections;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PokerHistory : MonoBehaviour
{

    public Image[] CommunityCardsSprite;
    public PlayerPokerRecord[] playerPokerRecord;
    public plyerPokerFields[] PokerHistoryFields;
    public GameObject[] playerPokerFieldsPrefab;
    public Color[] WinLoseColor;

    public TMP_Text CurrentRecordIndexTxt;

    #region Creating Instance
    private static PokerHistory _instance;
    public static PokerHistory Instance
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<PokerHistory>();
            return _instance;
        }
    }
    private void Awake()
    {
        if (!MatchHandler.IsPoker())
        {
            gameObject.SetActive(false);
            return;
        }
        if (_instance == null)
            _instance = this;
    }
    #endregion


    private void Start()
    {
        if (!MatchHandler.IsPoker())
        {
            gameObject.SetActive(false);
            return;
        }
        GetPokerHistroyFromNetwork();
    }
    public void ShowPokerHistory()
    {
        currentRecord = 1;
        CurrentRecordIndexTxt.text = playerPokerRecord.Length - currentRecord + "/" + (playerPokerRecord.Length - 1);
        ShowCurrentMatchHistory(currentRecord);
    }

    public void SortPokerRecord()
    {
        for (int i = playerPokerRecord.Length - 1; i > 0; i--)
        {
            for (int j = 0; j < CommunityCardsSprite.Length; j++)
            {
                playerPokerRecord[i].CommunityCardsIndex[j] = playerPokerRecord[i - 1].CommunityCardsIndex[j];
            }
            playerPokerRecord[i].NumberOfPlayersInMatch = playerPokerRecord[i - 1].NumberOfPlayersInMatch;
            for (int j = 0; j < playerPokerRecord[i].playerRecord.Length; j++)
            {
                playerPokerRecord[i].playerRecord[j].nameOfPlayer = playerPokerRecord[i - 1].playerRecord[j].nameOfPlayer;
                playerPokerRecord[i].playerRecord[j].RankOfPlayer = playerPokerRecord[i - 1].playerRecord[j].RankOfPlayer;
                playerPokerRecord[i].playerRecord[j].BetAmountWinLoose = playerPokerRecord[i - 1].playerRecord[j].BetAmountWinLoose;
                playerPokerRecord[i].playerRecord[j].holeCard1 = playerPokerRecord[i - 1].playerRecord[j].holeCard1;
                playerPokerRecord[i].playerRecord[j].holeCard2 = playerPokerRecord[i - 1].playerRecord[j].holeCard2;
                playerPokerRecord[i].playerRecord[j].isWinner = playerPokerRecord[i - 1].playerRecord[j].isWinner;
            }
        }
        SetHistoryToNetwork();
        GetPokerHistroyFromNetwork();
        ShowCurrentMatchHistory(currentRecord);
    }
    public void SetHistoryBetAmountForEachPlayer(int viewID, BigInteger amount)
    {
        for (int i = 0; i < playerPokerRecord[0].playerRecord.Length; i++)
        {
            if (playerPokerRecord[0].playerRecord[i].viewId == viewID)
            {
                playerPokerRecord[0].playerRecord[i].BetAmountWinLoose = amount.ToString();
            }
        }
    }
    void ShowCurrentMatchHistory(int matchNumber)
    {
        for (int i = 0; i < 5; i++)
        {
            int cardNumber = playerPokerRecord[matchNumber].CommunityCardsIndex[i];
            if (cardNumber != -1)
                CommunityCardsSprite[i].sprite = getCardSprite(cardNumber);
            else
                CommunityCardsSprite[i].sprite = GameManager.Instance.DummyCardPrefab.GetComponent<Image>().sprite;
        }
        if (playerPokerRecord[matchNumber].CommunityCardsIndex[0] == -1)
        {
            foreach (var item in playerPokerFieldsPrefab)
                item.SetActive(false);
            return;
        }
        for (int i = 0; i < 5; i++)
        {
            if (i < playerPokerRecord[matchNumber].NumberOfPlayersInMatch)
            {
                playerPokerFieldsPrefab[i].SetActive(true);
                PokerHistoryFields[i].nameOfPlayer.text = playerPokerRecord[matchNumber].playerRecord[i].nameOfPlayer;
                PokerHistoryFields[i].RankOfPlayer.text = playerPokerRecord[matchNumber].playerRecord[i].RankOfPlayer;
                if (playerPokerRecord[matchNumber].playerRecord[i].BetAmountWinLoose != "")
                    PokerHistoryFields[i].BetAmountWinLoose.text = LocalSettings.Rs(playerPokerRecord[matchNumber].playerRecord[i].BetAmountWinLoose);
                else
                    PokerHistoryFields[i].BetAmountWinLoose.text = "0";
                int cardNumber = playerPokerRecord[matchNumber].playerRecord[i].holeCard1;
                PokerHistoryFields[i].holeCard1.GetComponent<Image>().sprite = getCardSprite(cardNumber);
                cardNumber = playerPokerRecord[matchNumber].playerRecord[i].holeCard2;
                PokerHistoryFields[i].holeCard2.GetComponent<Image>().sprite = getCardSprite(cardNumber);


                if (playerPokerRecord[matchNumber].playerRecord[i].isWinner)
                {
                    PokerHistoryFields[i].BetAmountWinLoose.color = WinLoseColor[0];
                    if (playerPokerRecord[matchNumber].playerRecord[i].BetAmountWinLoose != "")
                        PokerHistoryFields[i].BetAmountWinLoose.text = "+" + PokerHistoryFields[i].BetAmountWinLoose.text;
                }
                else
                {
                    PokerHistoryFields[i].BetAmountWinLoose.color = WinLoseColor[1];
                    PokerHistoryFields[i].BetAmountWinLoose.text = "-" + PokerHistoryFields[i].BetAmountWinLoose.text;
                }
            }
            else
                playerPokerFieldsPrefab[i].SetActive(false);
        }
    }

    Sprite getCardSprite(int cardNumber)
    {
        return GameManager.Instance.AllCards.Card[cardNumber].gameObject.GetComponent<Image>().sprite;
    }

    int currentRecord = 0;
    public void ShowRecord(bool isNext)
    {
        if (isNext)
        {
            currentRecord++;
            if (currentRecord >= playerPokerRecord.Length)
                currentRecord = playerPokerRecord.Length - 1;
        }
        else
        {
            currentRecord--;
            if (currentRecord < 1)
                currentRecord = 1;
        }
        CurrentRecordIndexTxt.text = playerPokerRecord.Length - currentRecord + "/" + (playerPokerRecord.Length - 1);
        ShowCurrentMatchHistory(currentRecord);
    }
    void SetHistoryToNetwork()
    {
        if (!NetworkServer.active)
            return;
        for (int i = 0; i < playerPokerRecord.Length; i++)
        {
            string[] matchArray = new string[41];
            int index = 0;
            for (int j = 0; j < playerPokerRecord[i].CommunityCardsIndex.Length; j++)
            {
                matchArray[index] = playerPokerRecord[i].CommunityCardsIndex[j].ToString();
                index++;
            }
            matchArray[index] = playerPokerRecord[i].NumberOfPlayersInMatch.ToString();
            index++;
            for (int j = 0; j < playerPokerRecord[i].playerRecord.Length; j++)
            {
                matchArray[index] = playerPokerRecord[i].playerRecord[j].isWinner.ToString();
                index++;
                matchArray[index] = playerPokerRecord[i].playerRecord[j].viewId.ToString();
                index++;
                matchArray[index] = playerPokerRecord[i].playerRecord[j].nameOfPlayer;
                index++;
                matchArray[index] = playerPokerRecord[i].playerRecord[j].RankOfPlayer;
                index++;
                matchArray[index] = playerPokerRecord[i].playerRecord[j].BetAmountWinLoose;
                index++;
                matchArray[index] = playerPokerRecord[i].playerRecord[j].holeCard1.ToString();
                index++;
                matchArray[index] = playerPokerRecord[i].playerRecord[j].holeCard2.ToString();
                index++;
            }
                // Sync match history to all clients via SyncVar
                string csv = string.Join(",", matchArray);
                if (NetworkServer.active)
                    NetworkGameManager.Instance.CmdSetPokerMatchHistory(i, csv);
                else
                    NetworkGameManager.Instance.CmdSetPokerMatchHistory(i, csv);
            }
        }

    public void GetPokerHistroyFromNetwork()
    {
        StartCoroutine(GetPokerHistroyFromNetworkCo());
    }
    IEnumerator GetPokerHistroyFromNetworkCo()
    {
        yield return new WaitForSeconds(2);
        string csvCheck = NetworkGameManager.Instance.syncedPokerMatchHistory0;
            if (!string.IsNullOrEmpty(csvCheck))
            {
                for (int i = 0; i < playerPokerRecord.Length; i++)
            {
                string[] matchArray = new string[41];
                    // Read match history from SyncVar
                    string csv = (i == 0) ? NetworkGameManager.Instance.syncedPokerMatchHistory0 : NetworkGameManager.Instance.syncedPokerMatchHistory1;
                    if (!string.IsNullOrEmpty(csv))
                    {
                        string[] parts = csv.Split(',');
                        Array.Copy(parts, matchArray, Mathf.Min(parts.Length, matchArray.Length));
                    }
                    int index = 0;

                for (int j = 0; j < playerPokerRecord[i].CommunityCardsIndex.Length; j++)
                {
                    if (matchArray[index] == "-1")
                        playerPokerRecord[i].CommunityCardsIndex[j] = -1;
                    else
                        playerPokerRecord[i].CommunityCardsIndex[j] = GetInt(matchArray[index]);
                    index++;
                }
                playerPokerRecord[i].NumberOfPlayersInMatch = GetInt(matchArray[index]);
                index++;
                for (int j = 0; j < playerPokerRecord[i].playerRecord.Length; j++)
                {
                    if (matchArray[index] == "True")
                        playerPokerRecord[i].playerRecord[j].isWinner = true;
                    else
                        playerPokerRecord[i].playerRecord[j].isWinner = false;
                    index++;

                    playerPokerRecord[i].playerRecord[j].viewId = GetInt(matchArray[index]);
                    index++;

                    playerPokerRecord[i].playerRecord[j].nameOfPlayer = matchArray[index];
                    index++;

                    playerPokerRecord[i].playerRecord[j].RankOfPlayer = matchArray[index];
                    index++;

                    playerPokerRecord[i].playerRecord[j].BetAmountWinLoose = matchArray[index];
                    index++;

                    playerPokerRecord[i].playerRecord[j].holeCard1 = GetInt(matchArray[index]);
                    index++;

                    playerPokerRecord[i].playerRecord[j].holeCard2 = GetInt(matchArray[index]);
                    index++;
                }
            }
            ShowPokerHistory();
        }
    }
    int GetInt(string key)
    {
        return int.Parse(key);
    }
}


[Serializable]
public class PlayerPokerRecord
{

    public int[] CommunityCardsIndex;
    public int NumberOfPlayersInMatch;
    public PokerRecord5Players[] playerRecord;
}
[Serializable]
public class PokerRecord5Players
{
    public bool isWinner;
    public int viewId;
    public string nameOfPlayer;
    public string RankOfPlayer;
    public string BetAmountWinLoose;
    public int holeCard1;
    public int holeCard2;
}
[Serializable]
public class plyerPokerFields
{
    public TMP_Text nameOfPlayer;
    public TMP_Text RankOfPlayer;
    public TMP_Text BetAmountWinLoose;
    public RectTransform holeCard1;
    public RectTransform holeCard2;
}

}
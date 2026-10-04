using System;
using System.Linq;
using System.Transactions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class CasinoGameHistory : MonoBehaviour
{
    public Text gameName;
    public Text serialNumber;
    public Text dateTime;
    public Text coinsAmount;
    public Text winlost;
    public RoundHistoryParent RoundsHistory;
    public GameObject OpenRoundsHistoryButton;
    public void Init(Gamehistory data)
    {
        string[] dateFormats = {
                "yyyy-MM-ddTHH:mm:ss.fffZ", // ISO 8601 format with milliseconds
                "yyyy-MM-ddTHH:mm:ssZ",     // ISO 8601 format without milliseconds
                "yyyy-MM-ddTHH:mm:ss"       // Fallback for ISO 8601 without 'Z'
            };

        // Attempt to parse date with DateTimeOffset
        if (DateTimeOffset.TryParseExact(data.createdAt, dateFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTimeOffset parsedDateOffset))
        {
            // Convert to local time
            DateTime localDateTime = parsedDateOffset.LocalDateTime;
            data._createdAt = localDateTime; // Assign local DateTime to your field
        }
        else
        {
            ConstantsData_M.Log("Failed to parse date: " + data.createdAt);
            data._createdAt = DateTime.MinValue; // Set a default value if parsing fails
        }

        // Display the formatted date
        DateTime createdt = data._createdAt;
        gameName.text = data.title;
        serialNumber.text = data.transaction_id;
        dateTime.text = createdt.ToString("MMMM dd, yyyy HH:mm:ss");
        coinsAmount.transform.parent.GetComponent<Image>().sprite = data.coins_type.Contains("silver") ? GameHistoryManager.instance.SilverCoinIcon : GameHistoryManager.instance.GoldCoinIcon;
        OpenRoundsHistoryButton.SetActive(int.Parse(data.rounds_played) > 0);
        //Debug.Log($"<color=blue>{data.rounds_played}</color> ");
        GetHistoryRounds(data.transaction_id);
    }
    public void OpenRoundsHistory()
    {
        for (int i = 0; i < GameHistoryManager.instance.RoundsHistory.dataHolder.transform.childCount; i++)
        {
            Destroy(GameHistoryManager.instance.RoundsHistory.dataHolder.transform.GetChild(i).gameObject);
        }
        GameHistoryManager.instance.RoundsHistory.Init(RoundsHistory.data);
    }
    public void GetHistoryRounds(string transactionId)
    {
        //Debug.Log("Getting Rounds History of : " + transactionId);
      
    }
}

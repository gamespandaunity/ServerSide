using System;
using UnityEngine;
using UnityEngine.UI;
public class SingleGameHistory : MonoBehaviour
{
    public Text gameName;
    public Text serialNumber;
    public Text dateTime;
    public Text coinsAmount;
    public Text winlost;
    public Sprite win, lose;
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
        
        coinsAmount.text = data.gold != "0" ? data.gold : data.silver;
        coinsAmount.transform.parent.GetComponent<Image>().sprite = data.gold != "0" ? GameHistoryManager.instance.GoldCoinIcon : GameHistoryManager.instance.SilverCoinIcon;
        winlost.text = data.winner == staticVariables.UserProfiledata.user._id.ToString() ? "Won" : "loss";
        Color yellow;
        ColorUtility.TryParseHtmlString("#ffc107", out yellow);
        Color green;
        ColorUtility.TryParseHtmlString("#28a745", out green);
        Color red;
        ColorUtility.TryParseHtmlString("#dc3545", out red);

        if (winlost.text.Contains("Won"))
            winlost.transform.parent.GetComponent<Image>().sprite = win;
        else if (winlost.text.Contains("loss"))
            winlost.transform.parent.GetComponent<Image>().sprite = lose;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeenPattiRoundHistory_Template : MonoBehaviour
{
    public Text transaction_id;
    public Text winner;
    public Text coins;
    public Text status;
    public Text created_at;
    public void Init(RoundHistoryModel model)
    {
        transaction_id.text = model.transaction_id;
        winner.text = model.first_player_invest.ToString();
        coins.text = model.coins;
        status.text = model.winner == staticVariables.UserProfiledata.user._id.ToString() ? "Won" : "loss";
        Color yellow;
        ColorUtility.TryParseHtmlString("#ffc107", out yellow);
        Color green;
        ColorUtility.TryParseHtmlString("#28a745", out green);
        Color red;
        ColorUtility.TryParseHtmlString("#dc3545", out red);
        if (status.text.Contains("Won"))
            status.transform.parent.GetComponent<Image>().color = green;
        else if (status.text.Contains("loss"))
            status.transform.parent.GetComponent<Image>().color = red;
        DateTime dateTime = DateTime.Parse(model.created_at);
        string formattedDateTime = dateTime.ToString("dd-MMM-yy hh:mmtt");
        created_at.text = formattedDateTime;
    }

}

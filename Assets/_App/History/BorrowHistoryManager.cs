using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using UI.Dates;
//using SimpleJSON;

public class BorrowHistoryManager : MonoBehaviour
{
    public GameObject withdrawalInfo;
    public GameObject withdrawalTemplate;
    public List<BorrowHistoryModel> history = new List<BorrowHistoryModel>();
    public TMP_InputField RangeDateFrom, RangeDateTo;
    public HistoryManager historyManager;
    public int CurrentPage = 1;
    public string localjson;

    string dateFormat = "MM/dd/yyyy";
    
   
    IEnumerator DisplayFilterFriends(List<BorrowHistoryModel> Filterhistory)
    {
        yield return new WaitUntil(() => withdrawalInfo.transform.childCount == 0);

        for (int i = 0; i < Filterhistory.Count; i++)
        {
            GameObject template = Instantiate(withdrawalTemplate, withdrawalInfo.transform);
            SingleWithdrawalHistory templateHistory = template.GetComponent<SingleWithdrawalHistory>();
            if (templateHistory != null)
            {
                templateHistory.serialNumber.text = Filterhistory[i].transaction_id;
                templateHistory.coinsAmount.text = Filterhistory[i].silver_coin;
                templateHistory.CoinImage.sprite = templateHistory.silvercoin;
                templateHistory.withdrawalStatus.text = Filterhistory[i].status;
                Color yellow;
                ColorUtility.TryParseHtmlString("#ffc107", out yellow);
                Color green;
                ColorUtility.TryParseHtmlString("#28a745", out green);
                Color red;
                ColorUtility.TryParseHtmlString("#dc3545", out red);

                if (templateHistory.withdrawalStatus.text.Contains("pending"))
                {
                    templateHistory.withdrawalStatus.transform.parent.GetComponent<Image>().color = yellow;
                    templateHistory.deleteRequest.gameObject.SetActive(true);
                    templateHistory.Delete_URL = "/borrow/" + Filterhistory[i]._id;

                }
                else if (templateHistory.withdrawalStatus.text.Contains("canceled")|| templateHistory.withdrawalStatus.text.Contains("reject"))
                    templateHistory.withdrawalStatus.transform.parent.GetComponent<Image>().color = red;
                else
                    templateHistory.withdrawalStatus.transform.parent.GetComponent<Image>().color = green;
                string[] dateFormats = {
                "yyyy-MM-ddTHH:mm:ss.fffZ", // ISO 8601 format with milliseconds
                "yyyy-MM-ddTHH:mm:ssZ",     // ISO 8601 format without milliseconds
                "yyyy-MM-ddTHH:mm:ss"       // Fallback for ISO 8601 without 'Z'
            };

                // Attempt to parse date with DateTimeOffset
                if (DateTimeOffset.TryParseExact(Filterhistory[i].createdAt, dateFormats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTimeOffset parsedDateOffset))
                {
                    // Convert to local time
                    DateTime localDateTime = parsedDateOffset.LocalDateTime;
                    Filterhistory[i]._createdAt = localDateTime; // Assign local DateTime to your field
                }
                else
                {
                    ConstantsData_M.Log("Failed to parse date: " + Filterhistory[i].createdAt);
                    Filterhistory[i]._createdAt = DateTime.MinValue; // Set a default value if parsing fails
                }

                // Display the formatted date
                DateTime createdt = Filterhistory[i]._createdAt;
                templateHistory.withdrawalDate.text = createdt.ToString("MMMM dd, yyyy HH:mm:ss");
            }
        }
    }
    private void OnDisable()
    {
        historyManager.firstBtn.onClick.RemoveAllListeners();
        historyManager.lastBtn.onClick.RemoveAllListeners();
        historyManager.nextBtn.onClick.RemoveAllListeners();
        historyManager.previousBtn.onClick.RemoveAllListeners();
    }
    public void OnEnable()
    {
        historyManager.firstBtn.onClick.AddListener(LoadFirstPage);
        historyManager.lastBtn.onClick.AddListener(LoadLastPage);
        historyManager.nextBtn.onClick.AddListener(LoadNextPage);
        historyManager.previousBtn.onClick.AddListener(LoadPreviousPage);

    }
  
    public void LoadFirstPage()
    {
        CurrentPage = 1;
    }
    public void LoadNextPage()
    {
        if (CurrentPage < int.Parse(historyManager.lastPage.text))
        {
            CurrentPage++;
        }
    }
    public void LoadPreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }
    public void LoadLastPage()
    {
        CurrentPage = int.Parse(historyManager.lastPage.text);
    }


}

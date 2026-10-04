using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using Newtonsoft.Json.Linq;
using static Unity.Cinemachine.CinemachineOrbitalTransposer;
using System.Collections.Generic;
using TMPro;
using System.Collections;


public class PurchaseHistoryManager : MonoBehaviour
{
    public GameObject withdrawalInfo;
    public GameObject withdrawalTemplate;
    public List<WithdrawalHistoryData> history = new List<WithdrawalHistoryData>();
    public string localjson;
    public TMP_InputField RangeDateFrom, RangeDateTo;
    public HistoryManager historyManager;
    public int CurrentPage = 1;

    string dateFormat = "MM/dd/yyyy";
   
   
    public void OnEnable()
    {
        historyManager.firstBtn.onClick.AddListener(LoadFirstPage);
        historyManager.lastBtn.onClick.AddListener(LoadLastPage);
        historyManager.nextBtn.onClick.AddListener(LoadNextPage);
        historyManager.previousBtn.onClick.AddListener(LoadPreviousPage);
    }

    private void OnDisable()
    {
        historyManager.firstBtn.onClick.RemoveAllListeners();
        historyManager.lastBtn.onClick.RemoveAllListeners();
        historyManager.nextBtn.onClick.RemoveAllListeners();
        historyManager.previousBtn.onClick.RemoveAllListeners();
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

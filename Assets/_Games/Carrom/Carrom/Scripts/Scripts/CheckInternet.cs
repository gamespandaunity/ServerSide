using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using TMPro;
using BEKStudio;
using System.Data;
namespace BEKStudio
{
    public class CheckInternet : MonoBehaviour
{
    private bool isConnected;
    bool isDestroyed;

    private void Start()
    {
        isConnected = GameManager.Instance.isNetWorking;
        UpdateState();
        StartCoroutine(CheckInternetConnectivity());
    }

    private void OnDestroy()
    {
        isDestroyed = true;
        StopAllCoroutines();
    }
    

    IEnumerator CheckInternetConnectivity()
    {
        WaitForSeconds wait = new WaitForSeconds(5f);
        while (true)
        {
            yield return wait;
            UpdateState();
        }
    }

    private void UpdateState()
    {
        if (isDestroyed) return;

        if (!CheckInternetConnection())
        {
            if (isConnected)
            {
                InternetStateChange(false);
                GameManager.Instance.isNetWorking = false;
                isConnected = false;
            }
        }
        else
        {
            if (!isConnected)
            {
                InternetStateChange(true);
                GameManager.Instance.isNetWorking = true;
                isConnected = true;
            }
        }
    }

    bool CheckInternetConnection()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    void InternetStateChange(bool state)
    {
        if (!state)
        {
            //if (MenuController.Instance.vsScreen.activeInHierarchy)
            //{
            //    MenuController.Instance.VsBackBtn();
            //}
          //  MenuController.Instance.OpenMainScreen();
          //  MenuController.Instance.DisplayPopUp(true, "No Internet! Please Check Your Internet Connectivity");
        }
        else
        {
           // MenuController.Instance.DisplayPopUp(true, "Internet Is Back!");
        }
    }
}
}

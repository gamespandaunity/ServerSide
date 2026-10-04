using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace Snake_Ladder
{

    public class Loading : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI loadingText;
        [SerializeField] string textToShow;
        [SerializeField] float LifeTime;

        private bool isConnected = true;

        private void OnEnable()
        {
            isConnected = true;
            StartCoroutine(loadAnim());
            StartCoroutine(CheckInternetConnectivity());
            StartCoroutine(CheckLifeTime());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        IEnumerator loadAnim()
        {
            while (true)
            {
                loadingText.text = textToShow + ".";
                yield return new WaitForSeconds(0.5f);
                loadingText.text = textToShow + "..";
                yield return new WaitForSeconds(0.5f);
                loadingText.text = textToShow + "...";
                yield return new WaitForSeconds(0.5f);
            }
        }

        IEnumerator CheckInternetConnectivity()
        {
            WaitForSeconds wait = new WaitForSeconds(5f);
            while (true)
            {
                yield return wait;
                if (!CheckInternetConnection())
                {
                    if (isConnected)
                    {
                        OnNoInternet();
                        isConnected = false;
                    }
                }
                else
                {
                    isConnected = true;
                }
            }
        }

        IEnumerator CheckLifeTime()
        {
            yield return new WaitForSeconds(LifeTime);

            MenuManager.Instance.SetJoinFailedPopUpState(true, "Something Went Wrong!");
            MenuManager.Instance.SetLoadingPopUpState(false);
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

        void OnNoInternet()
        {
            Debug.Log("No internet connection!");
            MenuManager.Instance.EnablePreviousMenu();
            MenuManager.Instance.SetJoinFailedPopUpState(true, "No internet connection!");
            MenuManager.Instance.SetLoadingPopUpState(false);
        }

    }
}
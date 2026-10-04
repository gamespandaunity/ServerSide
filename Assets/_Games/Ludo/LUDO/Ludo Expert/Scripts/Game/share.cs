using AssemblyCSharp;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.Networking;
using LudoGame;

using UnityEngine.SceneManagement;

[Serializable]
public class Notice
{
    public string mass;
    public string link;
}

public class share : MonoBehaviour
{
    public GameObject ToastMessage;
    public GameObject refferText;
    private string fireUrl = "https://income2-9d47d-default-rtdb.firebaseio.com/note.json";

    string randomString = "hyiopweu98wyer0piwutu9rt8";
   // public Text textMsg, paymentAddress, coinText;
    private void Awake()
    {
        refferText.GetComponent<Text>().text = LudoGame.GameManager.Instance.playfabManager.PlayFabId;
      //  StartCoroutine(GetText());

    }

    IEnumerator GetText()
    {
        UnityWebRequest www = UnityWebRequest.Get(fireUrl);
        yield return www.SendWebRequest();

        if (www.isNetworkError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Notice data = new Notice();
            data = JsonUtility.FromJson<Notice>(www.downloadHandler.text);

            // Show results as text
           // textMsg.text = data.mass;
            //link.text = data.link;
            Debug.Log(data.mass);

            // Or retrieve results as binary data
        }
    }

    public void refreshBtn()
    {
        //coinText.text = PlayerPrefs.GetString("GetCoins", "0");

        Debug.Log("Refresh clicked ");

        //  playFabManager.CheckIfFirstTitleLogin(LudoGame.GameManager.Instance.playfabManager.PlayFabId, false);

        //string url = StaticStrings.UApacheUrl + "get_user_updated_coins";
        //string playFabId = LudoGame.GameManager.Instance.playfabManager.PlayFabId;
        //WWWForm form = new WWWForm();
        //form.AddField("playFabId", playFabId);
        //WWW www = new WWW(url, form);
        //StartCoroutine(WaitForRequestt(www));

    }


    public void buysell()
    {
        Application.OpenURL("https://youtu.be/Ksw16zOVUE0");
    }
    public void tel()
    {
        Application.OpenURL("https://youtube.com/channel/UCEeq-kWGtAp1slyVXl2jh2A");
    }
    public void buy()
    {
        Application.OpenURL("https://m.me/ludopaybd");
    }
    public void rul()
    {
        Application.OpenURL("https://sites.google.com/view/ludopaybd");
    }
    public void yt()
    {
        Application.OpenURL("https://www.youtube.com/channel/UCtKWSrRqlyFYhLO5sCfQc9w");
    }
    public void depyt()
    {
        Application.OpenURL("https://youtu.be/xCtppHjjOsw");
    }





    public void copyCode()
    {

        var textEditor = new TextEditor();
        textEditor.text = LudoGame.GameManager.Instance.playfabManager.PlayFabId;
        textEditor.SelectAll();
        textEditor.Copy();
        StartCoroutine(ShowToast());
        Debug.Log("Copied");
    }


    private IEnumerator ShowToast()
    {
        ToastMessage.SetActive(true);

        yield return new WaitForSeconds(2.0f);

        ToastMessage.SetActive(false);
    }
}





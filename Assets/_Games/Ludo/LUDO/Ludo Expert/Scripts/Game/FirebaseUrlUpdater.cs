using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

public class FirebaseUrlUpdater : MonoBehaviour
{
    public string dbUrl;
    private string urlListDBurl = "https://online-c932b-default-rtdb.firebaseio.com/URL";
    JSONNode url;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(loadUrl());
    }



    public IEnumerator loadUrl()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(urlListDBurl + ".json"))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                Debug.Log("error:" + webRequest.error);
            }
            else
            {
                url = JSON.Parse(webRequest.downloadHandler.text);
                dbUrl = url["url"];
                Debug.Log(dbUrl + "<<<<<<");
            }
        }
    }
}

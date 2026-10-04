using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using UnityEngine.Networking;
using UnityEngine.UI;
public class depnot : MonoBehaviour
{
    JSONNode data;    // Start is called before the first frame update
    public Text number;
    public string dbURL = "https://ludo-pay-dfaaa-default-rtdb.firebaseio.com";
    public GameObject ToastMessage;

    void Start()
    {
       StartCoroutine(loadData());
    }

    IEnumerator loadData()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(dbURL + "/depnot" + ".json"))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                Debug.Log("error:___________" + webRequest.error);
            }
            else
            {
                data = JSON.Parse(webRequest.downloadHandler.text);
                number.text = data;
                Debug.Log("==================");
                Debug.Log(data);


            }
        }
    }
    private IEnumerator ShowToast()
    {
        ToastMessage.SetActive(true);

        yield return new WaitForSeconds(2.0f);

        ToastMessage.SetActive(false);
    }
    public void copyCode()
    {

        var textEditor = new TextEditor();
        textEditor.text = number.text;
        textEditor.SelectAll();
        textEditor.Copy();
        StartCoroutine(ShowToast());
        Debug.Log("Copied");
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}

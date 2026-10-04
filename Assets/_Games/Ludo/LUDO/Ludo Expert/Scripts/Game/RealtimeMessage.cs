using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;
using UnityEngine.UI;
using LudoGame;

class Mess
{
    public string id;
    public string mess;
    public string avater;
}


public class RealtimeMessage : MonoBehaviour
{
    private string dbURL = "-1";
    private float timer = 0;
    private float intervaltime = 0.5f;
    public GameObject messageBox;
    public GameObject myMessageBox;
    public Transform parent;
    public ScrollRect Scroll;
    JSONNode data;
    int datacount = 0;
    public InputField messInput;
    public VerticalLayoutGroup layoutGroup;

    float messBoxHeight;
    float gap;
    public GameObject urlObj;
    bool isStart;

    private string urlListDBurl = "https://ludo-pay-dfaaa-default-rtdb.firebaseio.com/URL";
    JSONNode url;

    public List<Sprite> avater;

    // Start is called before the first frame update
    void Start()
    {

        messBoxHeight = messageBox.GetComponent<RectTransform>().rect.height;


        /*       float contentHeight = Scroll.content.sizeDelta.y;
               float contentShift = 2 * Time.deltaTime + ((messBoxHeight + gap ) * data.Count);
               Scroll.verticalNormalizedPosition -= contentShift / contentHeight;*/

        /*        StartCoroutine(loadUrl());
        */
        StartCoroutine(loadUrl());
        Scroll.verticalNormalizedPosition = 0;
        Canvas.ForceUpdateCanvases();

    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(dbURL + " <<<");
        if (Scroll.verticalNormalizedPosition >= 0.95f)
        {
            //  Debug.Log("end___");
            isStart = false;
        }
        else if (Scroll.verticalNormalizedPosition <= 0.05f)
        {
            // Debug.Log("start____");
            isStart = true;
        }

        timer += Time.deltaTime;
        if (timer > intervaltime && dbURL != "-1")
        {
            StartCoroutine(loadData());
            timer = 0;
        }
        if (!data.IsNull)
        {
            if (datacount != data.Count && dbURL != "-1")
            {
                updateUI();
            }
        }

        datacount = data.Count;
    }

    public void no()
    {
        sendMessage();
        Debug.Log("----------------------------------------------------"+data.Count);
    }
    public void sendMessage()
    {
        Scroll.verticalNormalizedPosition = 0;
        Canvas.ForceUpdateCanvases();
        if(!string.IsNullOrEmpty(messInput.text) || !string.IsNullOrWhiteSpace(messInput.text))
          StartCoroutine(postMassage(messInput.text));
        messInput.text = "";

        Debug.Log(1234);


    }

    [System.Obsolete]
    IEnumerator loadData()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(dbURL + "/messages" + ".json"))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                Debug.Log("error:" + webRequest.error);
            }
            else
            {
                data = JSON.Parse(webRequest.downloadHandler.text);
              
            }
        }
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
                dbURL = url["url"];

                Debug.Log(">>>>" + dbURL + "<<<<<<");
                StartCoroutine(loadData());
                Debug.Log("Data loded");
            }
        }
    }
    /*    IEnumerator loadUrl()
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
                    Debug.Log(url["url"] + "<<<<");
                }
            }
        }*/

    private int getAvaterIndex(string name)
    {
        return avater.FindIndex(x => x.name == name);
    }

    IEnumerator postMassage(string mess)
    {
        string url = dbURL + "/messages" + ".json";
        Mess data = new Mess();
        data.mess = mess;
        data.id = LudoGame.GameManager.Instance.playfabManager.PlayFabId;
        data.avater = LudoGame.GameManager.Instance.avatarMy.name;
        string form = JsonUtility.ToJson(data);

        var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(form);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        Debug.Log("Status Code: >>>>" + request.responseCode);
    }

    private void updateUI()
    {
        if (isStart)
        {
            Debug.Log("---------");
            Scroll.verticalNormalizedPosition = 0;
            Canvas.ForceUpdateCanvases();
            /*            float contentHeight = Scroll.content.sizeDelta.y;
                        float contentShift = 2 * Time.deltaTime + messBoxHeight;
                        Scroll.verticalNormalizedPosition -= contentShift / contentHeight;*/
        }

        foreach (Transform child in parent)
        {
            GameObject.Destroy(child.gameObject);
        }




        foreach (string key in data.Keys)
        {
            if (data[key]["id"] != LudoGame.GameManager.Instance.playfabManager.PlayFabId)
            {
                GameObject o = Instantiate(messageBox, parent);
                Transform i = o.transform.GetChild(0);
                i.GetChild(0).GetComponent<Image>().sprite = avater[getAvaterIndex(data[key]["avater"])];
                Transform t = o.transform.GetChild(2);
                t.GetChild(0).GetComponent<Text>().text = data[key]["mess"];
                o.GetComponent<chatItems>().playfabID = data[key]["id"];
            }
            else
            {
                GameObject o = Instantiate(myMessageBox, parent);
                Transform i = o.transform.GetChild(0);
                i.GetChild(0).GetComponent<Image>().sprite = LudoGame.GameManager.Instance.avatarMy;
                Transform t = o.transform.GetChild(2);
                t.GetChild(0).GetComponent<Text>().text = data[key]["mess"];
            }

        }

    }
}


// CHECK KOREN AMI ELTU HISU KORE ASI!!ok
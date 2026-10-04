using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SupportUIManager : MonoBehaviour
{
    // Start is called before the first frame update
    public Text supportText;
    public GameObject supportcontactsDetailinfo;
    public GameObject supportcontactstemplate;
    public GameObject panelErrorBg;
    public Text alerttittle;
    public Text alertDescription;

    void OnEnable()
    {
      

        if (Borderspanel.UnselectCoin != null)
        {
            Borderspanel.UnselectCoin(false);
        }
        if (Borderspanel.logoHandler != null)
        {
            Borderspanel.logoHandler(false);
        }
        StartCoroutine(ServerConnection.GetApiRequest(ServerConnection.Support_Contacts_Url() + staticVariables.UserProfiledata.user.country, OnSucess =>
        {
            WhatsappContacts whatsappContacts = JsonUtility.FromJson<WhatsappContacts>(OnSucess);
            if (whatsappContacts != null && whatsappContacts.status)
            {

                if (whatsappContacts.data.phone != null && whatsappContacts.data.phone.Count != 0)
                {
                    for (int i = 0; i < whatsappContacts.data.phone.Count; i++)
                    {
   
                            GameObject supportManager = (GameObject)Instantiate(supportcontactstemplate);
                            supportManager.transform.SetParent(supportcontactsDetailinfo.transform);
                            supportManager.GetComponent<SingleWhatsappContacts>().srNumber.text = (i + 1).ToString();
                            supportManager.GetComponent<SingleWhatsappContacts>().whatsappContacts.text = whatsappContacts.data.phone[i];
                            supportManager.transform.localScale = Vector3.one;
                            if (whatsappContacts.data.remarks != null)
                            {
                            }
                            else
                            {
                            }
                        
                    }
                }
                else
                {

                    supportText.text = "Sorry, no contact was found for your country";
                }
                
            }
            else
            {
             supportText.text = "Sorry, no contact was found for your country";

            }
        },
       OnFailed =>
       {
           supportText.text = "Sorry, no contact was found for your country";
       }));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void closedButton()
    {
        panelErrorBg.SetActive(false);
    }
    public void ReturnToBackDashboard()
    {
       SceneLoaderUtility.LoadScene("MainmenuScene"); 
    }
    private void OnDisable()
    {
        for (int i = 0; i < supportcontactsDetailinfo.transform.childCount; i++)
        {
            Destroy(supportcontactsDetailinfo.transform.GetChild(i).gameObject);
        }
    }
}


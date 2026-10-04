using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PopupMessageManager : MonoBehaviour
{
    public GameObject popupPanelObject;

    public GameObject inputPanelObject;

    public GameObject confirmPanelObject;

    public GameObject optionPanelObject;
    public GameObject reconnectionPanel;

    public TextMeshProUGUI popupHeaderText;

    public TextMeshProUGUI popupBodyText;

    public TextMeshProUGUI popupConfirmText;

    public static PopupMessageManager instance;

    public Button popupSubmitButton;

    public Button popupYesButton;

    public Button popupNoButton;

    public TMP_InputField popupInputField;

    public GameObject phraseItemPrefab;

    public Transform phraseListContent;

    public ScrollRect phraseScrollView;

    [Header("Option Panel")]
    public GameObject optionItemPrefab;

    public Transform optionListContent;

    public Button optionConfirmButton;

    public List<OptionPrefab> optionList;

    public List<string> tempOptions;

    [Header("PANELS")]
    public WaitingPanel waitingPanel;
    public SlowInternetPanel slowInternetPanel;
    public DisconnectedPanel disconnectedPanel;

    [Header("Scroll Detail Panel")]
    [FormerlySerializedAs("scrollDetailPanel")]
    public GameObject scrollDetailPanelObject;

    [FormerlySerializedAs("headerTextOfScroll")]
    public TextMeshProUGUI scrollDetailHeader;

    [FormerlySerializedAs("bodyTextOfScroll")]
    public TextMeshProUGUI scrollDetailBody;

    [Header("Reconnection")]
    public GameObject ReconnectionPanel;
    public TextMeshProUGUI ReconnectionText;
    public Button yesReconnectButton, noReconnectButton;

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }
    public void SetPanelStaus(bool val, string body = "", string header = "")
    {
        if (header != "") popupHeaderText.text = header;
        if (body != "") popupBodyText.text = body;
        popupPanelObject.SetActive(val);
    }
    public void SetPanelStausText(string body = "", string header = "")
    {
        if (header != "") popupHeaderText.text = header;
        if (body != "") popupBodyText.text = body;
    }
    public void ShowPopUp(string body = "", string header = "", float timeToShow = 3, Action methodToInvoke = null)
    {
        if (header != "") popupHeaderText.text = header;
        if (body != "") popupBodyText.text = body;
        StartCoroutine(showPanel(timeToShow, methodToInvoke));

    }
    IEnumerator showPanel(float time, Action methodToInvoke = null)
    {
        popupPanelObject.SetActive(true);
        yield return new WaitForSeconds(time);
        popupPanelObject.SetActive(false);
        if (methodToInvoke != null)
        {
            methodToInvoke.Invoke();
        }

    }
    public void ReconnectionPanelStatus(bool status)
    {
        if (SnokerNetwork.Instance && !SnokerNetwork.Instance._mainScript.initialized) return;
        status.Show("Reconnect Status");
        reconnectionPanel.SetActive(status);
        if (status)
        {
            SetWaitingPanel(false);
        }
    }
    public void ShowInputBox(string placeHolder = "", string buttonText = "", Action methodToInvoke = null, List<string> phrases = null)
    {
        inputPanelObject.SetActive(true);
        popupSubmitButton.onClick.RemoveAllListeners();
        popupInputField.text = "";
        if (popupInputField.placeholder is TextMeshProUGUI placeholderText)
        {
            placeholderText.text = placeHolder;
        }
        if (buttonText != "")
            popupSubmitButton.GetComponentInChildren<TextMeshProUGUI>().text = buttonText;
        if (methodToInvoke != null)
        {
            popupSubmitButton.onClick.AddListener(() =>
            {
                methodToInvoke.Invoke();
                inputPanelObject.SetActive(false);
            });
        }
        if (phrases != null)
        {
            if (phrases.Count > 0)
            {
                phraseScrollView.gameObject.SetActive(true);
                phraseListContent.Clear();
                for (int i = 0; i < phrases.Count; i++)
                {
                    int t = i;
                    GameObject prefabInstance = Instantiate(phraseItemPrefab, phraseListContent);
                    prefabInstance.GetComponentInChildren<TextMeshProUGUI>().text = phrases[i];
                    prefabInstance.GetComponent<Button>().onClick.AddListener(() => SelectPhrase(phrases[t]));
                }
            }

        }
    }

    void SelectPhrase(string text)
    {
        popupInputField.text = text;
        phraseScrollView.gameObject.SetActive(false);
    }
    public void ShowConfirmPanel(string body = "", Action methodToInvoke = null, string yesButtonText = "YES", bool yesButtonStatus = true, bool noButtonStatus = true)
    {
        "Confirm Panel is Active".Show();
        //if (header != "") headerText.text = header;
        try
        {
            popupConfirmText.text = body;
            "STEP 1 - Body Set".Show();
        }
        catch (Exception e)
        {
            $"ERROR on setting text: {e.Message}".Show();
        }
        "1".Show();
        if (confirmPanelObject)
            confirmPanelObject.SetActive(true);
        "2".Show();
        popupYesButton.onClick.RemoveAllListeners();
        popupYesButton.gameObject.SetActive(yesButtonStatus);
        popupNoButton.gameObject.SetActive(noButtonStatus);
        if (yesButtonText != "") popupYesButton.GetComponentInChildren<TextMeshProUGUI>().text = yesButtonText;
        if (methodToInvoke != null)
        {
            popupYesButton.onClick.AddListener(() =>
            {
                methodToInvoke?.Invoke();
                confirmPanelObject.SetActive(false);
            });
        }
    }

    public void ShowTextDetailPanel(string body = "", string header = "")
    {
        scrollDetailPanelObject.SetActive(true);
        scrollDetailHeader.text = header;
        scrollDetailBody.text = body;
    }
    public void SetWaitingPanel(bool status)
    {

        if (status)
        {
            waitingPanel.StartTimer();
            slowInternetPanel.PanelStatus(false);

        }
        else
        {

            waitingPanel.StopTimer();
        }
    }
    public void SetSlowInternetPanel(bool status)
    {
        slowInternetPanel.PanelStatus(status);
    }
    public void SetDisconnectedPanel(bool status)
    {
        disconnectedPanel.PanelStatus(status);
        if (status)
        {
            SetWaitingPanel(false);
            slowInternetPanel.PanelStatus(false);
        }
    }

    public void ReconnectionStatus(bool status, UnityAction onYes = null, UnityAction onNo = null)
    {
        ReconnectionPanel.SetActive(status);

        yesReconnectButton.onClick.RemoveAllListeners();
        noReconnectButton.onClick.RemoveAllListeners();
        if (onYes != null)
            yesReconnectButton.onClick.AddListener(onYes);
        if (onNo != null)
            noReconnectButton.onClick.AddListener(onNo);

        yesReconnectButton.onClick.AddListener(() =>
        {
            ReconnectionPanel.SetActive(false);
        });
        noReconnectButton.onClick.AddListener(() =>
        {
            ReconnectionPanel.SetActive(false);
        });

    }
}


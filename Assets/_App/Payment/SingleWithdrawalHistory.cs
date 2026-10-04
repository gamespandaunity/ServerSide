using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class SingleWithdrawalHistory : MonoBehaviour
{
    public Text serialNumber;
    public Text coinsAmount;
    public TextMeshProUGUI withdrawalStatus;
    public Text withdrawalDate;
    public Button deleteRequest;
    public string Delete_URL;
    public Image CoinImage;
    public Sprite goldcoin, silvercoin;
    public string requestName;
    private void Start()
    {
        deleteRequest.onClick.RemoveAllListeners();
        deleteRequest.onClick.AddListener(deleteRequestfun);
    }
    public void confirmationYesBtn()
    {
        StartCoroutine(ServerConnection.DeleteApiRequest(Delete_URL, OnSucess =>
        {
            GameObject ConfirmPanel = Instantiate(Resources.Load("RequestSucessPopUp"), ScreenNavigotor_Custom.GameScreenStack.Peek().Screenobj.transform.root) as GameObject;
            ConfirmPanel.GetComponent<ErrorPopUp>().InitMessage("Request Deleted Sucessfully");
            Destroy(gameObject);
        }));
    }

    public void deleteRequestfun()
    {
        staticVariables.shouldupdate = false;
        GameObject ConfirmPanel = Instantiate(Resources.Load("ConfirmAlert"), transform.root) as GameObject;
        ConfirmPanel.GetComponent<ConfirmAlert>().Init("Confirm", $"Are you Sure? \n you Want to Delete {requestName} Request.", () => confirmationYesBtn());
    }
}

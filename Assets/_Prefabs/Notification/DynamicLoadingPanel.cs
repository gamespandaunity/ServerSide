 
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class DynamicLoadingPanel : MonoBehaviour
{
    public Image bg;
    private void OnEnable()
    {
        //print("dynamic loading GAME ID ==>" + APIManager.gameid);
        SpritesManager.Instance.spritesScriptable.BgHandlerAccordingTo_Id(bg);
    }
    public void Start()
    {
        StartCoroutine(checkCurrentScene());
    }


    IEnumerator checkCurrentScene()
    {
        yield return new WaitForSeconds(1);
        if (SceneManager.GetActiveScene().name == "AightBallPool" || SceneManager.GetActiveScene().name == "Main_Menu")
        {
            Destroy(this.gameObject);
        }
        else
        {
            StartCoroutine(checkCurrentScene());
        }
    }
    public void ClosePanel()
    {
        //Photon Removal if (PhotonNetwork.InRoom)
        {

            //print("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);

            //Photon Removal   PhotonNetwork.LeaveRoom();
        }
        Destroy(gameObject);
    }
}

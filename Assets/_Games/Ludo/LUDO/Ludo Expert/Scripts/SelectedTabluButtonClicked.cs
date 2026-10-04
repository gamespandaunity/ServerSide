using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using LudoGame;

public class SelectedTabluButtonClicked : MonoBehaviour
{

    public int tableNumber;
    public int fee;

    // Use this for initialization

    void Start()
    {
        Debug.Log("start");
        gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
        gameObject.GetComponent<Button>().onClick.AddListener(startGame);
    }

    // Update is called once per frame
    void Update()
    {

    }



    public void startGame()
    {

        LudoGame.GameManager.Instance.GameScene = "GameScene";
        LudoGame.GameManager.Instance.requiredPlayers = tableNumber;

        Debug.Log("Fee: " + fee + "  Coins: " + LudoGame.GameManager.Instance.myPlayerData.GetCoins());
        if (LudoGame.GameManager.Instance.myPlayerData.GetCoins() >= fee)
        {

            if (LudoGame.GameManager.Instance.inviteFriendActivated)
            {
                LudoGame.GameManager.Instance.tableNumber = tableNumber;
                LudoGame.GameManager.Instance.payoutCoins = fee;
                LudoGame.GameManager.Instance.initMenuScript.backToMenuFromTableSelect();
                LudoGame.GameManager.Instance.playfabManager.challengeFriend(LudoGame.GameManager.Instance.challengedFriendID, "" + fee + ";" + tableNumber);

            }
            else if (LudoGame.GameManager.Instance.offlineMode)
            {
                LudoGame.GameManager.Instance.payoutCoins = fee;
                if (!LudoGame.GameManager.Instance.gameSceneStarted)
                {
                    SceneManager.LoadScene(LudoGame.GameManager.Instance.GameScene);
                    LudoGame.GameManager.Instance.gameSceneStarted = true;
                }
            }
            else
            {
                LudoGame.GameManager.Instance.tableNumber = tableNumber;
                LudoGame.GameManager.Instance.payoutCoins = fee;
                LudoGame.GameManager.Instance.facebookManager.startRandomGame();
            }

        }
        else
        {
            LudoGame.GameManager.Instance.dialog.SetActive(true);
        }

    }


}

using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NetworkManagement;

public class ChallengeItemPrefab : MonoBehaviour
{
    public Request Request;
    public Image profileImage, coinImage, bgimage;
    public TextMeshProUGUI PlayerName, winningAmount, betType;
    public GameObject LoadingPanel;
    public Sprite goldCoins, silverCoins;
    public string roomId;
    public void Initialized(Request request)
    {
        Request = request;
        PlayerName.text = request.first_name + " " + request.last_name;
        winningAmount.text = "WINNINGS: " + request.bet_amount;
        betType.text = request.bet_type;
        if (bgimage)
        {
            bgimage.color = request.bet_type.Contains("silver") ? new Color32(174, 249, 255, 255) : new Color32(255, 190, 0, 255);
            //bgimage.GetComponent<UIShadow>().effectColor = request.bet_type.Contains("silver") ? new Color32(174, 255, 255, 255) : new Color32(255, 200, 0, 255);
        }
        if (coinImage)
        {
            coinImage.sprite = request.bet_type.Contains("silver") ? silverCoins : goldCoins;

        }
        ServerConnection.DownloadSprite("/" + request.file_url, DownloadedImage =>
        {
            if (profileImage)
                profileImage.sprite = ConstantsData_M.ConvertTextureToSprite(DownloadedImage);
        });
    }
    public void AcceptChallenge()
    {
        if (Request != null)
        {
            roomId = Request.transaction_id;
            StartCoroutine(AcceptNotification());

        }
        else
        {
            ConstantsData_M.Log("Invalid format for result array");
        }
    }
    private IEnumerator AcceptNotification()
    {
        if (SceneManager.GetActiveScene().name == "Home")
        {
            if (Request != null && ApiAndRoomManager._instance != null)
            {
                staticVariables.isPlayingWithAI = false;
                staticVariables.isnotificationcounter = false;
                ApiAndRoomManager._instance.currentRoomId = roomId;
                ApiAndRoomManager._instance.winLoseChallengeId = Request.transaction_id;
                staticVariables.OpponetProfile = new NetworkManagement.PlayerProfile(
                    Request.first_name + " " + Request.last_name, Request.file_url,
                    Request.bet_amount, Request.game_id.ToString(),
                    Request.user_id.ToString(), Request.transaction_id,
                    !Request.bet_type.Contains("silver"), "12000");

                Instantiate(LoadingPanel, GameObject.Find("Lobby").transform);

                // Hand off to EdgegapAPIClient.JoinGame — it waits for OnServerReady if the
                // Edgegap server isn't ready yet, then connects with the correct address.
                // DO NOT call StartClient() directly here — server may still be booting.
                MirrorNetwork.Instance.edgegapAPIClient.JoinGame(Request.transaction_id, Request);
            }
            else
            {
                ConstantsData_M.Log("notificationUiManager or APIManager.instance is null.");
            }
        }
        else
        {
            SceneLoaderUtility.LoadScene("Home");
        }

        Destroy(gameObject);
        yield break;
    }

}

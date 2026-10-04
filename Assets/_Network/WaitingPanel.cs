using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaitingPanel : MonoBehaviour
{
    public Coroutine WaitingTimerCorotine;
    public Text notificationText;
    public GameObject panel;
    public void StartTimer()
    {
        Debug.Log("WaitingStartTimer");
        StopTimer();
        WaitingTimerCorotine = StartCoroutine(Show30SecondsTimer());

    }
    public void StopTimer()
    {
        if (SceneManager.GetActiveScene().name == "SnokkerMultiplayer")
            SnokerNetwork.Instance?._SnokerGameManager._SnokerCameraManager.blurGameView(false);
        panel.SetActive(false);
        if (WaitingTimerCorotine != null)
        {
            StopCoroutine(WaitingTimerCorotine);
            WaitingTimerCorotine = null;
        }
    }
    public IEnumerator Show30SecondsTimer()
    {
        if (ResultManager.isGameFinished)
        {
            SnokerNetwork.Instance?._SnokerGameManager._SnokerCameraManager.blurGameView(false);

            yield break;
        }
        if (ResultManagerForCricket.instance && ResultManagerForCricket.instance.isGameFinished)
        {
            yield break;
        }
        
        int remainingTime = 30;
        panel.SetActive(true);
        SnokerNetwork.Instance?._SnokerGameManager._SnokerCameraManager.blurGameView(true);

        notificationText.text = $"({remainingTime})\nWaiting for opponent";
        while (remainingTime > -1)
        {
            // Belt-and-suspenders: if the opponent has reconnected (both players present
            // again), cancel the countdown even if the explicit stop RPC/notification was missed.
            if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.currentPlayerCount >= 2)
            {
                panel.SetActive(false);
                yield break;
            }
            if (PopupMessageManager.instance.waitingPanel.panel.activeSelf == false)
            {
                break; // Exit if the popup panel is closed
            }
            notificationText.text = $"({remainingTime})\nWaiting for opponent";
            yield return new WaitForSecondsRealtime(1f);
            remainingTime--;
            //Photon Removal  if (ApiAndRoomManager.currentGameId != 15)
            //Photon Removal   PunNetwork.instance.CheckGameStatusOnJoin();
        }
        if (remainingTime <= 0)
        {
            //Photon Removal  if (ApiAndRoomManager.currentGameId != 15)
            //Photon Removal  PunNetwork.instance.DeclareWin("Opponet Time Out");
            //Photon Removal  else
            // FusionNetwork._fusionRpcManager.DeclareWin("Opponet Time Out");
            panel.SetActive(false);
            // SceneManager.LoadScene("Home");
            MirrorNetwork.OnWinCall?.Invoke("Opponent has disconnected. You are declared the winner.");
        }
    }


}

using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityExtensions;

public class LudoNetworkManager : NetworkBehaviour
{



    public static LudoNetworkManager instance;

    public List<NetworkConnectionToClient> players = new List<NetworkConnectionToClient>();


    #region Game Timer Syncing
    [Header("UI Timer")]
    // public TextMeshProUGUI countDownGameTimer;

    [Header("Countdown Settings")]
    [SyncVar(hook = nameof(OnTimeChanged))]
    public int remainingTime = 900;

    public int remainingminutes;
    public int remainingseconds;

    private Coroutine countdownCoroutine;

    [Server]
    private IEnumerator ServerCountdown()
    {
        remainingTime = 900;
        Debug.Log("Server Countdown Started." + remainingTime);
        while (remainingTime > 0)
        {
            // Pause the 5-min game clock whenever a player has dropped — wait until both
            // players are present again before ticking. Without this, the timer keeps
            // draining while one side is in the reconnect window and can end the game
            // even though the disconnected player still has time to come back.
            while (NetworkGameManager.Instance != null && NetworkGameManager.Instance.currentPlayerCount < 2)
            {
                yield return new WaitForSeconds(0.5f);
            }

            remainingTime--; // SyncVar updates all clients automatically
            yield return new WaitForSeconds(1f);
        }
        //while (remainingTime > 0)
        //{
        //   // if (!NetworkGameManager.Instance.IsPaused)
        //        remainingTime--;

        //    yield return new WaitForSeconds(1f);
        //}
        int creatorFinished = LudoGame.GameManager.Instance.playerObjects[0].finishedPawns;
        int joinerFinished = LudoGame.GameManager.Instance.playerObjects[1].finishedPawns;

        string winnerID;

        if (creatorFinished > joinerFinished)
            winnerID = NetworkGameManager.Instance.creatorData.playerId;
        else if (joinerFinished > creatorFinished)
            winnerID = NetworkGameManager.Instance.joinerData.playerId;
        else
            winnerID = NetworkGameManager.Instance.creatorData.playerId;

        RpcTimerEnded(winnerID);
    }

    // This runs on all clients whenever remainingTime changes
    private void OnTimeChanged(int oldTime, int newTime)
    {
        if (GameGUIController.insta.countDownGameTimer == null) return;
        int minutes = newTime / 60;
        int seconds = newTime % 60;

        GameGUIController.insta.countDownGameTimer.text = $"{minutes:00}:{seconds:00}";

        if (newTime > 180)
            GameGUIController.insta.countDownGameTimer.color = Color.green;
        else if (newTime > 60)
            GameGUIController.insta.countDownGameTimer.color = Color.white;
        else
            GameGUIController.insta.countDownGameTimer.color = Color.red;
    }

    [ClientRpc]
    private void RpcTimerEnded(string winnerID)
    {
        GameGUIController.insta.countDownGameTimer.text = "00:00";
        GameGUIController.insta.countDownGameTimer.color = Color.red;

        NetworkGameManager.Instance.CmdPlayerFinished(true, int.Parse(winnerID));
    }
    #endregion
    void Awake()
    {
        instance = this;

    }





    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("✅ OnStartServer called - Server started");

        if (NetworkServer.active)
        {
            Debug.Log("🔄 Waiting for players to connect...");
            this.DelayUntil(() => NetworkServer.connections.Count >= 2, () =>
            {

                Debug.Log($"✅ {NetworkServer.connections.Count} players connected, setting players...");
                this.Delay(4, () =>
                {

                    if (countdownCoroutine != null)
                        StopCoroutine(countdownCoroutine);
                    countdownCoroutine = StartCoroutine(ServerCountdown());
                });
            });
        }
    }





}


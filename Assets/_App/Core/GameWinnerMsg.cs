using BallPool;
using NaughtyAttributes;
using NetworkManagement;
using System.Collections;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityExtensions;

public class GameWinnerMsg : MonoBehaviour
{
    public GameObject win;
    public TMP_Text winLoseTxt;
    string myName_FromUserModel;
    public static bool IsGameFinished;
    private void Awake()
    {

        IsGameFinished = false;
    }

    private void OnEnable()
    {
        // ONLINE: the result flow comes from the server (RpcGameWin -> GameManager.ShowWinPanel ->
        // ResultManager), so auto-starting the local animation here duplicated the panel — that is
        // why this call was disabled (commit 32720d0e2, 17 Jul). But OFFLINE/AI this coroutine IS
        // the entire win/lose flow ("You Win/Lose" text -> 2.5s -> WinLoseGameManager spawn ->
        // HandleGameResultAltAI), and disabling it here removed the AI-mode result panel entirely.
        // Restore it for offline only.
        if (!BallPoolGameLogic.isOnLine)
            StartCoroutine(StartWinAnimations());
    }
    IEnumerator StartWinAnimations()
    {
        if (AightBallPoolPlayer.mainPlayer.isDraw && AightBallPoolPlayer.otherPlayer.isDraw)
        {
            winLoseTxt.text = "Match Draw";
            yield return new WaitForSeconds(2.5f);
            // instatiate win panel
            GameObject gb = Instantiate(win, transform);
            // ResultManagerFor8Ball winPanel = gb.GetComponent<ResultManagerFor8Ball>();
            CircularTimeController.instance.audioSource.Stop();
            CircularTimeController.instance.audioSource.clip = null;
            CircularTimeController.instance.turnAudioSource.Stop();
            CircularTimeController.instance.turnAudioSource = null;
            //winPanel.Draw();
            // ResultManagerForSnooker.instance.HandleGameResultAlt(true, gameWinner);
            if (ResultManager.GameSpawnedFinished == false)
            {
                ResultManager.GameSpawnedFinished = true;
                GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                if (enemyPrefab != null)
                {
                    "1".Show();
                    // Spawn at position (0,0,0)
                    var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                    gameObject.GetComponent<ResultManager>().Draw();
                }
                else
                {
                    Debug.LogError("WinLose GameManager prefab not found!");
                }
            }
        }
        else
        {
            Debug.Log("GameWinnerMsg: Starting Win Animations");
            BallPoolPlayer winner = BallPoolPlayer.GetWinner();
            myName_FromUserModel = staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name;

            winLoseTxt.text = winner.playerId == AightBallPoolPlayer.mainPlayer.playerId ? "You Win" : "You Lose";
            PlayerDetails PlayerData = new PlayerDetails(AightBallPoolPlayer.mainPlayer.playerId, myName_FromUserModel, $"/{staticVariables.UserProfiledata.user.file_url}");
            if (GameModeManager.isAI) // ai mode
            {
                PlayerDetails OpponentPlayerData = new PlayerDetails(GameModeManager.isAI ? 0 : int.Parse(EightBallPoolNetworkManager.opponentPlayer.userId), GameModeManager.isAI ? "AI" : EightBallPoolNetworkManager.opponentPlayer.userName,
               GameModeManager.isAI ? "" : $"/{EightBallPoolNetworkManager.opponentPlayer.imageURL}", GameModeManager.isAI);
            }
            yield return new WaitForSeconds(2.5f);
            // instatiate win panel
            GameObject gb = Instantiate(win, transform);
            // Win8Ball winPanel = gb.GetComponent<Win8Ball>();
            CircularTimeController.instance.audioSource.Stop();
            CircularTimeController.instance.audioSource.clip = null;
            CircularTimeController.instance.turnAudioSource.Stop();
            CircularTimeController.instance.turnAudioSource = null;
            if (GameModeManager.isAI) // ai mode
            {
                if (winner.playerId == AightBallPoolPlayer.mainPlayer.playerId)
                {
                    ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
                    // winPanel.Init(true, staticVariables.UserProfiledata.user._id.ToString(), true);
                    if (staticVariables.isGuest)
                        GuestDataManager._instance.UpdateGuestCoins(staticVariables.currentPrize * 2);
                    Debug.Log("Game Finished You win vs AI");
                    // winPanel.HandleGameResultAlt(true, staticVariables.UserProfiledata.user._id.ToString());
                    // ResultManagerForSnooker.instance.HandleGameResultAlt(true, gameWinner);
                    if (ResultManager.GameSpawnedFinished == false)
                    {
                        ResultManager.GameSpawnedFinished = true;
                        GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                        if (enemyPrefab != null)
                        {
                            "1".Show();
                            // Spawn at position (0,0,0)
                            var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                            gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(true, staticVariables.UserProfiledata.user._id.ToString());
                        }
                        else
                        {
                            Debug.LogError("WinLose GameManager prefab not found!");
                        }
                    }
                }
                else
                {
                    Debug.Log("Game Finished You Lose vs AI");
                    //winPanel.HandleGameResultAlt(false, staticVariables.UserProfiledata.user._id.ToString());
                    if (ResultManager.GameSpawnedFinished == false)
                    {
                        ResultManager.GameSpawnedFinished = true;
                        GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                        if (enemyPrefab != null)
                        {
                            "1".Show();
                            // Spawn at position (0,0,0)
                            var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                            gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(false, staticVariables.UserProfiledata.user._id.ToString());
                        }
                        else
                        {
                            Debug.LogError("WinLose GameManager prefab not found!");
                        }
                    }
                    //  winPanel.HandleGameResultAlt(true, "ai", true);

                }
            }
            // MULTIPLAYER
            else
            {
                Debug.Log("GameWinnerMsg: Multiplayer Mode Detected");
                IsGameFinished = true;
                $"{winner.playerId}".Show("Game Mode Manager Multiplayer");
                if (winner.playerId == staticVariables.UserProfiledata.user._id)
                {
                    // MyEightBallNetwork.Instance.CmdPlayerFinished();
                    // winPanel.HandleGameResultAlt(true, winner.playerId.ToString());
                    if (ResultManager.GameSpawnedFinished == false)
                    {
                        ResultManager.GameSpawnedFinished = true;
                        GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                        if (enemyPrefab != null)
                        {
                            "1".Show();
                            // Spawn at position (0,0,0)
                            var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                            gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(true, winner.playerId.ToString());
                        }
                        else
                        {
                            Debug.LogError("WinLose GameManager prefab not found!");
                        }
                    }
                    this.Delay(1, () =>
                    {
                        MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(NetworkGameManager.Instance.currentServerRequestId);
                    });
                    Debug.Log("Game Finished You win");
                }
                else
                {
                    // MyEightBallNetwork.Instance.CmdPlayerFinished();
                    // winPanel.HandleGameResultAlt(false, staticVariables.UserProfiledata.user._id.ToString());
                    if (ResultManager.GameSpawnedFinished == false)
                    {
                        ResultManager.GameSpawnedFinished = true;
                        GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                        if (enemyPrefab != null)
                        {
                            "1".Show();
                            // Spawn at position (0,0,0)
                            var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                            gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(false, staticVariables.UserProfiledata.user._id.ToString());
                        }
                        else
                        {
                            Debug.LogError("WinLose GameManager prefab not found!");
                        }
                    }
                    this.Delay(1, () =>
                    {
                        MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(NetworkGameManager.Instance.currentServerRequestId);
                    });
                    //  winPanel.HandleGameResultAlt(true, staticVariables.UserProfiledata.user._id.ToString());
                    Debug.Log("Game Finished You lose");
                    //  winPanel.HandleGameResultAlt(true, OpponentPlayerData.id.ToString(), false);
                }
                if (winner.playerId == AightBallPoolPlayer.mainPlayer.playerId)
                {
                    ConstantsData_M.Log("Game Finished You win");
                    //StartCoroutine(winPanel.Init(PlayerData, OpponentPlayerData, NetworkManager.mainPlayer.prize, true));
                }
                else
                {
                    ConstantsData_M.Log("Game Finished you lose");
                    //StartCoroutine(winPanel.Init(OpponentPlayerData, PlayerData, NetworkManager.mainPlayer.prize));
                }
            }
        }
    }


}

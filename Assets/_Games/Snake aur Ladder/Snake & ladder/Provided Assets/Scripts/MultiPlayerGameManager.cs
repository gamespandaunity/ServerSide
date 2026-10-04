using System.Collections;

using UnityEngine;
using UnityEngine.UI;

using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Snake_Ladder
{
    public class MultiPlayerGameManager : MonoBehaviour
    {

        public GamePlayController gamePlayController;
        public TurnTimer turnTimer;
        public int lastPlayerTurnId;
        public GameObject gameBoard;

        #region UNITY
        //public override void OnEnable()
        //{
        //    base.OnEnable();

        //    CountdownTimer.OnCountdownTimerHasExpired += OnCountdownTimerIsExpired;
        //    TurnTimer.TunrnTimerHasExpired += OnTurnTimerExipred;
        //    if (!PhotonNetwork.IsMasterClient)
        //    {
        //        gameBoard.transform.rotation = Quaternion.Euler(new Vector3(0, 0, 0));
        //    }
        //}

        public void Start()
        {
            //gamePlayController.myId = PhotonNetwork.IsMasterClient ? PLAYERS.PLAYER1.GetHashCode() : PLAYERS.PLAYER2.GetHashCode();
            //gamePlayController.aiId = PhotonNetwork.IsMasterClient ? PLAYERS.PLAYER2.GetHashCode() : PLAYERS.PLAYER1.GetHashCode();

            Hashtable props = new Hashtable
            {
              //  {GameConstants.PLAYER_LOADED_LEVEL, true}
            };
            //PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        //public override void OnDisable()
        //{
        //    base.OnDisable();
        //    //CountdownTimer.OnCountdownTimerHasExpired -= OnCountdownTimerIsExpired;
        //    //TurnTimer.TunrnTimerHasExpired -= OnTurnTimerExipred;
        //}

        #endregion



        #region PUN CALLBACKS

        // public override void OnDisconnected(DisconnectCause cause)
        // {
        //     //UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
        // }

        // public override void OnLeftRoom()
        // {
        //     PhotonNetwork.Disconnect();
        // }

     //   public override void OnMasterClientSwitched(Player newMasterClient)
      //  {
       //     if (PhotonNetwork.LocalPlayer.ActorNumber == newMasterClient.ActorNumber)
       //     {

        //    }
     //   }

       // public override void OnPlayerLeftRoom(Player otherPlayer)
     //   {
            //if(gamePlayController.gameOver.activeInHierarchy) return;
            ////if (GameManager.instance.UserName.Equals(otherPlayer.NickName) || GameManager.instance.OpponentName.Equals(otherPlayer.NickName))
            ////{
            //    // TO Switch to AI
            //    // gamePlayController.isWithAI = true;
            //    // gamePlayController.isOnlineMultiplayer = false;
            //    // turnTimer.isTimerRunning = false;
            //    // gamePlayController.Player1Timer.SetActive(false);
            //    // gamePlayController.Player2Timer.SetActive(false);
            //    // gamePlayController.StartGameInCaseOfDisconnect();

            //    // To Simply Show Popup
            //    gamePlayController.DisconnectedPanel.SetActive(true);
            //    if (otherPlayer.NickName == GameManager.instance.UserName)
            //    {
            //        gamePlayController.DisconnectedText.text = $"<color=red>You Got Disconnected</color>";
            //    }
            //    else
            //    {

            //        gamePlayController.DisconnectedText.text = $"<color=green> {otherPlayer.NickName} Got Disconnected</color>";
            //    }

            //}
     //   }

        //public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        //{
        //    if (!PhotonNetwork.IsMasterClient)
        //    {
        //        return;
        //    }

        //    Debug.Log("OnPlayerPropertiesUpdate........................");
        //    // if there was no countdown yet, the master client (this one) waits until everyone loaded the level and sets a timer start
        //    int startTimestamp;
        //    bool startTimeIsSet = CountdownTimer.TryGetStartTime(out startTimestamp);

        //    if (changedProps.ContainsKey(GameConstants.PLAYER_LOADED_LEVEL))
        //    {
        //        Debug.Log("PLAYER_LOADED_LEVEL........................");

        //        if (CheckAllPlayerLoadedLevel())
        //        {
        //            if (!startTimeIsSet)
        //            {
        //                Debug.Log("startTimeIsSet........................");

        //                CountdownTimer.SetStartTime();
        //            }
        //        }
        //        else
        //        {
        //            // not all players loaded yet. wait:

        //        }
        //    }
        //}

        //public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        //{
        //    object startTimeFromProps;

        //    if (propertiesThatChanged.TryGetValue(GameConstants.IS_GAME_OVER, out startTimeFromProps))
        //    {
        //        if ((bool)startTimeFromProps)
        //        {
        //            // GameOver();
        //        }
        //    }

        //    if (propertiesThatChanged.TryGetValue(GameConstants.PLAYER_TURN, out startTimeFromProps))
        //    {
        //        if ((bool)startTimeFromProps)
        //        {
        //            if (propertiesThatChanged.TryGetValue(GameConstants.TURN_ID, out startTimeFromProps))
        //            {
        //                SetPlayerTurn((int)startTimeFromProps);

        //            }
        //        }
        //    }
        //}



        #endregion


        #region Game Logic
        // called by OnCountdownTimerIsExpired() when the timer ended
        private void StartGame()
        {
            //   Debug.LogError("StartGame");

          //  if (PhotonNetwork.IsMasterClient)
            {
                SetTurnData(Random.Range(1, 3));

            }
        }

        void SetTurnData(int turnId)
        {
            Hashtable props = new Hashtable
                    {
                //        {GameConstants.PLAYER_TURN, true},
                //        {GameConstants.TURN_ID, turnId}
                    };
           // PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        public void SetPlayerTurn(int turnId)
        {
            gamePlayController.currentPlayerTurn = (PLAYERS)turnId;
            lastPlayerTurnId = turnId;

            gamePlayController.StartGame();
            turnTimer.ResetRound();
        }

        void OnTurnTimerExipred()
        {
            NextPlayerTurn();
        }

        public void NextPlayerTurn()
        {
            if (lastPlayerTurnId == PLAYERS.PLAYER1.GetHashCode())
            {
                SetTurnData(2);
            }
            else
            {
                SetTurnData(1);
            }
        }

        private bool CheckAllPlayerLoadedLevel()
        {
            //foreach (Player p in PhotonNetwork.PlayerList)
            //{
            //    object playerLoadedLevel;

            //    if (p.CustomProperties.TryGetValue(GameConstants.PLAYER_LOADED_LEVEL, out playerLoadedLevel))
            //    {
            //        if ((bool)playerLoadedLevel)
            //        {
            //            continue;
            //        }
            //    }

            //    return false;
            //}

            return true;
        }



        private void OnCountdownTimerIsExpired()
        {
            StartGame();
        }

        #endregion

        #region RPC
        public void SendBeadSelectedMessage(int playerId, int beadId)
        {
            turnTimer.isTimerRunning = false;
          //  photonView.RPC("RPCBeadSelectedMessage", RpcTarget.OthersBuffered, playerId, beadId);

        }
      //  [PunRPC]
        public void RPCBeadSelectedMessage(int playerId, int beadId)
        {

            gamePlayController.RemotedBeadSelected(playerId, beadId);
        }


        public void SendMoveSelectedMessage(int playerId, int beadId)
        {
       //     photonView.RPC("RPCMoveSelectedMessage", RpcTarget.OthersBuffered, playerId, beadId);

        }
      //  [PunRPC]
        public void RPCMoveSelectedMessage(int playerId, int beadId)
        {//

            gamePlayController.RemotedMoveSelected(playerId, beadId);
        }

        #endregion

    }
}
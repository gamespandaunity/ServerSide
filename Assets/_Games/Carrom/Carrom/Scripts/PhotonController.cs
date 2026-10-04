using ExitGames.Client.Photon.StructWrapping;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
//using UnityEditor.VersionControl;

namespace BEKStudio
{
    public class PhotonController //Photon Removal: MonoBehaviourPunCallbacks
    {
        public static PhotonController Instance;
        [FormerlySerializedAs("whichMode")] public string gameMode = "carrom";
        [FormerlySerializedAs("whichRoom")] public string selectedRoom;
        [FormerlySerializedAs("roomEntryPice")] public int roomEntryFee = 0;
        [FormerlySerializedAs("playWithBot")] public bool isPlayingWithBot;
        [FormerlySerializedAs("botAvatar")] public int botAvatarIndex;
        [FormerlySerializedAs("botName")] public string botPlayerName = "AI";
        [FormerlySerializedAs("gameOver")] public bool isGameOver;
        [FormerlySerializedAs("isOtherPlayerLeft")] public bool hasOtherPlayerLeft;
        public bool gameOver;
        public bool pauseGame;
        private void OnEnable()
        {
            //PunNetwork.OnLose += AnnounceDefeat;
            //PunNetwork.OnAwaitingOpponent += SetWaitingForOpponent;                                                                            //Photon Removal
            //PunNetwork.OnWin += AnnounceVictory;
            //PunNetwork.OnServerDraw += AnnounceDraw;
            //PunNetwork.OnServerDisconnected += OnDisconnect;
        }

        private void OnDisable()
        {
            //PunNetwork.OnLose -= AnnounceDefeat;
            //PunNetwork.OnAwaitingOpponent -= SetWaitingForOpponent;                                             //Photon Removal
            //PunNetwork.OnWin -= AnnounceVictory;
            //PunNetwork.OnServerDraw -= AnnounceDraw;
            //PunNetwork.OnServerDisconnected -= OnDisconnect;
        }


        //public void SetWaitingForOpponent(bool state)
        //{
        //    if (state == true)
        //    {
        //        gameOver.Show("isFinishTriggered");
        //        if (gameOver == false)
        //        {
        //            pauseGame = true;
        //            PopupMessageManager.instance.SetWaitingPanel(true);
        //        }
        //    }
        //    else
        //    {
        //        pauseGame = false;
        //        PopupMessageManager.instance.SetWaitingPanel(false);
        //    }
        //}
        private void coroutine()
        {
            //Photon Removal  StartCoroutine(checkPingAfterDelay());

        }

        private IEnumerator checkPingAfterDelay()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(1f);
                //Photon Removal  if (PhotonNetwork.GetPing() > 350 && PunNetwork.instance.currentState == PunNetwork.ConnectionState.Connected)
                {
                    PopupMessageManager.instance.SetSlowInternetPanel(true);
                    pauseGame = true;
                }
                //Photon Removal  else
                {
                    PopupMessageManager.instance.SetSlowInternetPanel(false);
                    //Photon Removal  if (PunNetwork.instance.currentState == PunNetwork.ConnectionState.Connected)
                    pauseGame = false;
                }
            }
        }
        public void AnnounceVictory()
        {
            if (gameOver == false)
            {
                PopupMessageManager.instance.SetPanelStaus(false, body: "You have been disconnected. The opponent is declared the winner.");
                PopupMessageManager.instance.SetDisconnectedPanel(false);
                ResultManagerForCarrom.instance.WinPlayer(true, staticVariables.UserProfiledata.user._id);
                ConstantsData_M.Log(true + "1");

            }
        }

        public void AnnounceDefeat()
        {
            if (PhotonController.Instance.gameOver == false)
            {
                PopupMessageManager.instance.SetPanelStaus(false, body: "You have been disconnected. The opponent is declared the winner.");
                PopupMessageManager.instance.SetDisconnectedPanel(false);
                ResultManagerForCarrom.instance.WinPlayer(false, staticVariables.UserProfiledata.user._id);
            }
        }

        public void AnnounceDraw(bool status, string body)
        {
            if (!status)
            {
                if (PhotonController.Instance.gameOver == false)
                {
                    PopupMessageManager.instance.SetPanelStaus(false);
                    PopupMessageManager.instance.SetDisconnectedPanel(false);
                    ResultManagerForCarrom.instance.Draw();
                }
            }
            else
            {
                //Photon RemovalPunNetwork.instance.DeclareDefeat("Unable to join");
            }
        }

        //public void OnDisconnect(bool status, string body)
        //{
        //    PopupMessageManager.instance.SetDisconnectedPanel(status);
        //    pauseGame = status;
        //}

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            gameMode = "carrom";
        }

        public void Start()
        {
            //PhotonNetwork.KeepAliveInBackground = 2f;
            //ConnectedToServer();
            // PhotonNetwork.CurrentRoom.PlayerTtl = 20000;
            coroutine();
        }



        public int FindPlayerIndex()
        {
            int count = 0;

            if (!GameManager.Instance.isOnline()) return 0;

            //Photon Removal foreach (var item in PhotonNetwork.PlayerList)
            {
                //Photon Removal   if (item == PhotonNetwork.LocalPlayer)
                {
                    return count;
                }
                count++;
            }
            return -1;
        }

        //Photon Removal  public bool IsMasterPlayer() => PhotonNetwork.IsMasterClient;


        public void EndTimeout()
        {
            //Photon Removal  StopAllCoroutines();
        }
        IEnumerator TriggerTimeout()
        {
            yield return new WaitForSecondsRealtime(6f);
            //Photon Removal  PhotonNetwork.LeaveRoom();
        }

        bool isPaused = false;
        float pausedTime = 0f;


        IEnumerator AwaitBackgroundReady()
        {

            yield return new WaitForSecondsRealtime(3);
            if (isPaused)
            {
                if (MessageDisplayScript.instance)
                    MessageDisplayScript.instance.ShowMessage("Game quit!", "App must stay open for multiplayer.");
                QuitGameSession();
            }

        }

        public void QuitGameSession()
        {
            isGameOver = true;
            //Photon Removal    PhotonNetwork.AutomaticallySyncScene = false;
            //Photon Removal  PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
            //Photon Removal  PhotonNetwork.CurrentRoom.PlayerTtl = 0;
            //Photon Removal  PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene("Home");
        }


        private void OnMenuSceneReady(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Home")
            {
                SceneManager.sceneLoaded -= OnMenuSceneReady;
                //Photon Removal PhotonNetwork.LeaveRoom();
            }
        }
    }
}

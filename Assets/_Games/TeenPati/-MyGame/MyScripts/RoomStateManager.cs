using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityExtensions;
using Mirror;

namespace TeenPattiGame
{
    public class RoomStateManager : MonoBehaviourPunCallBacksWithNSSCallBacks
    {
        public static RoomStateManager Instance;


        private void Awake()
        {
            "roommanager.............................................".Show();
            if (Instance == null)
                Instance = this;

        }

        public RoomState.STATE CurrentRoomState;


        public void UpdateLocalRoomState(RoomState.STATE state)
        {
            TPLog.Flow("RoomStateManager", "UpdateLocalRoomState (local only, no network) " + CurrentRoomState + " -> " + state);
            CurrentRoomState = state;
        }

        public void UpdateCurrentRoomState(RoomState.STATE state)
        {
            //Room State and Room Property should be update by Master Client and hence will be updated on all Clients.
            //Now every client will receive callback on network and update accordingly.
            //UpdateLocalRoomState(state);
            TPLog.Flow("RoomStateManager", "UpdateCurrentRoomState requested: " + CurrentRoomState + " -> " + state);
            if (!MatchHandler.isOffline())
            {

                if (NetworkServer.active)
                {
                    TPLog.Flow("RoomStateManager", "I am the SERVER -> writing room property + Rpc'ing state " + state + " to everyone");
                    "Update Current Room State".Show();
                    //if (MirrorNetwork.Instance.isMasterClient)
                    {
                        this.DelayUntil(() => TeenPattiNNetworkManager.instance, () =>
                    {
                        UpdateRoomStateProperty(state);
                        // photonView.RPC(nameof(UpdateThisStateOnNetwork), RpcTarget.All, state, "");
                        UpdateThisStateOnNetwork((RoomState.STATE)state, "");
                        TeenPattiNNetworkManager.instance.RpcUpdateThisStateOnNetwork((int)state, "");
                    });
                    }

                }
                else
                {
                    if (MirrorNetwork.Instance.isMasterClient)
                    {
                        TPLog.Flow("RoomStateManager", "I am the MASTER client -> sending Cmd for state " + state);
                        UpdateRoomStateProperty(state);
                        TeenPattiNNetworkManager.instance.CmdUpdateThisStateOnNetwork((int)state, "");
                    }
                    else
                    {
                        TPLog.Flow("RoomStateManager", "NOT master client -> state " + state + " NOT sent, waiting for master's Rpc");
                    }
                }
            }
            else
            {

                UpdateThisStateOnNetwork(state, "");
            }
        }


        public void UpdateCurrentState(RoomState.STATE state, string infoText)
        {
            //Room State and Room Property should be update by Master Client and hence will be updated on all Clients.
            //Now every client will receive callback on network and update accordingly.
            TPLog.Flow("RoomStateManager", "UpdateCurrentState requested: " + CurrentRoomState + " -> " + state + " info='" + infoText + "'");
            if (!MatchHandler.isOffline())
            {
                if (MirrorNetwork.Instance.isMasterClient)
                {
                    TPLog.Flow("RoomStateManager", "I am the MASTER client -> sending Cmd for state " + state + " info='" + infoText + "'");
                    UpdateRoomStateProperty(state);
                    //photonView.RPC(nameof(UpdateThisStateOnNetwork), RpcTarget.All, state, infoText);        
                    TeenPattiNNetworkManager.instance.CmdUpdateThisStateOnNetwork((int)state, infoText);
                }
                else
                {
                    TPLog.Flow("RoomStateManager", "NOT master client -> state " + state + " NOT sent, waiting for master's Rpc");
                }
            }
            else
            {

                UpdateThisStateOnNetwork(state, infoText);
            }
        }


        public void UpdateCurrentStateOnShowBtn(RoomState.STATE state)
        {
            //Room State and Room Property should be update by Master Client and hence will be updated on all Clients.
            //Now every client will receive callback on network and update accordingly.
            TPLog.Flow("RoomStateManager", "UpdateCurrentStateOnShowBtn requested: " + CurrentRoomState + " -> " + state);
            if (MatchHandler.isOffline())
            {
                TPLog.Flow("RoomStateManager", "Offline branch -> sending Cmd for state " + state);
                UpdateRoomStateProperty(state);
                //   photonView.RPC(nameof(UpdateThisStateOnNetwork), RpcTarget.All, state, "");
                TeenPattiNNetworkManager.instance.CmdUpdateThisStateOnNetwork((int)state, "");
            }
            else
            {

                TPLog.Flow("RoomStateManager", "Online branch -> applying state " + state + " locally only");
                UpdateThisStateOnNetwork(state, "");
            }

        }
        public void LeaveBetForce()
        {
            TPLog.Flow("RoomStateManager", "LeaveBetForce - forcing every player out of the table");
            // photonView.RPC(nameof(LeaveBetRpc), RpcTarget.All);
            TeenPattiNNetworkManager.instance.CmdLeaveBetRpc();
            Debug.LogError("photon.leave room");

        }

        public void UpdateCurrentStateOnShowBtn(RoomState.STATE state, string infoText)
        {
            //Room State and Room Property should be update by Master Client and hence will be updated on all Clients.
            //Now every client will receive callback on network and update accordingly.
            TPLog.Flow("RoomStateManager", "UpdateCurrentStateOnShowBtn requested: " + CurrentRoomState + " -> " + state + " info='" + infoText + "'");
            if (!MatchHandler.isOffline())
            {
                TPLog.Flow("RoomStateManager", "Online -> sending Cmd for state " + state + " info='" + infoText + "'");
                UpdateRoomStateProperty(state);
                //  photonView.RPC(nameof(UpdateThisStateOnNetwork), RpcTarget.All, state, infoText);
                TeenPattiNNetworkManager.instance.CmdUpdateThisStateOnNetwork((int)state, infoText);
            }
            else
            {
                UpdateThisStateOnNetwork(state, infoText);
            }

        }



        public RoomState.STATE GetCurrentRoomState()
        {
            return CurrentRoomState;
        }

        // [PunRPC]
        public void UpdateThisStateOnNetwork(RoomState.STATE state, string info)
        {
            TPLog.Flow("RoomStateManager", "ROOM STATE APPLIED: " + CurrentRoomState + " -> " + state + (string.IsNullOrEmpty(info) ? "" : " info='" + info + "'"));
            CurrentRoomState = state;
            Debug.Log("Current State is Set to " + state);
            OnUpdateCurrentState(state, info);
        }
        // [PunRPC]
        public void LeaveBetRpc()
        {

            Debug.LogError("photon.leave room");
            TPLog.Flow("RoomStateManager", "LeaveBetRpc - marking disconnect reason ApplicationQuit and going back to Home");
            MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
            if (NetworkGameManager.Instance)
            {
                TPLog.Flow("RoomStateManager", "Telling server my disconnect type (ApplicationQuit) before leaving");
                NetworkGameManager.Instance.CmdSendTeenPattiDisconnectType(
                    (int)MirrorNetwork.Instance.currentDisconnectReason,
                    staticVariables.UserProfiledata.user._id.ToString());
            }
            else
            {
                TPLog.Warn("RoomStateManager", "NetworkGameManager missing - leaving without telling the server why");
            }
            this.Delay(1, () => NetworkManager.singleton.StopClient());
            SceneManager.LoadScene("Home");
        }

        void UpdateRoomStateProperty(RoomState.STATE state)
        {
            if (NetworkServer.active)
            {
                TPLog.Flow("RoomStateManager", "Saving room state property on server: " + state);
                TeenPattiNNetworkManager.instance.roomCustomPropertiesTeenPatti.SetRoomStateProperty(LocalSettings.roomState, (int)state);
            }
            else
            {
                TPLog.Flow("RoomStateManager", "Not the server - room state property NOT saved (" + state + ")");
            }
        }


        void OnUpdateCurrentState(RoomState.STATE state, string infoText)
        {
            state.ToString().Show("Room State");
            switch (state)
            {
                case RoomState.STATE.WaitingForPlayers:
                    TPLog.Flow("RoomStateManager", "STATE -> WaitingForPlayers (table idle, waiting for enough players)");
                    TriggerStateWaitingForPlayers();
                    break;

                case RoomState.STATE.GameIsStarting:
                    TPLog.Flow("RoomStateManager", "STATE -> GameIsStarting (countdown before the hand)");
                    TriggerStateGameIsStarting();
                    break;

                case RoomState.STATE.CardDistributing:
                    TPLog.Flow("RoomStateManager", "STATE -> CardDistributing (dealing cards)");
                    TriggerStateCardDistributing();
                    break;

                case RoomState.STATE.GameIsPlaying:
                    TPLog.Flow("RoomStateManager", "STATE -> GameIsPlaying (turns are running)");
                    TriggerStateGameIsPlaying();
                    break;

                case RoomState.STATE.GameSideShow:
                    TPLog.Flow("RoomStateManager", "STATE -> GameSideShow");
                    TriggerStateSideShow();
                    break;

                case RoomState.STATE.WaitingForResults:
                    TPLog.Flow("RoomStateManager", "STATE -> WaitingForResults info='" + infoText + "'");
                    TriggerStateWaitingForResults(infoText);
                    break;

                case RoomState.STATE.ShowingResults:
                    TPLog.Flow("RoomStateManager", "STATE -> ShowingResults (winner shown, reset will follow)");
                    TriggerStateShowingResults();
                    break;


                case RoomState.STATE.ABFirstTurn:
                    TPLog.Flow("RoomStateManager", "STATE -> ABFirstTurn");
                    TriggerStateabFirstTurn();
                    break;

                case RoomState.STATE.ABSecondTurn:
                    TPLog.Flow("RoomStateManager", "STATE -> ABSecondTurn");
                    TriggerStateabSecondTurn();
                    break;
            }
        }



        public bool IsStarted()
        {


            return GetCurrentRoomState() != RoomState.STATE.GameIsStarting && GetCurrentRoomState() != RoomState.STATE.WaitingForPlayers && GetCurrentRoomState() != RoomState.STATE.CardDistributing;
        }




        public bool GetIsNotInStartedState()
        {
            return GetCurrentRoomState() == RoomState.STATE.GameIsStarting ||
                GetCurrentRoomState() == RoomState.STATE.WaitingForPlayers ||
                GetCurrentRoomState() == RoomState.STATE.CardDistributing;
        }


        #region OverrideFunctions


        // public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)        
        // {
        //     if (propertiesThatChanged.ContainsKey(LocalSettings.roomState))
        //     {
        //         Debug.Log("Changed in Room State To " + propertiesThatChanged.Values);
        //     }
        //     //Debug.Log("Room Property Updated" + propertiesThatChanged);
        //     base.OnRoomPropertiesUpdate(propertiesThatChanged);
        // }

        #endregion        

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Photon.Pun;
//using Photon.Realtime;
using System.Linq;
using Mirror;

namespace POKER
{
    public class RoomStateManager : MonoBehaviourPunCallBacksWithNSSCallBacks
    {
        public static POKER.RoomStateManager Instance;


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                //Constants_M.Log("Why it Does'not work");
            }
            NetworkGameManager.OnEventReceived += OnNetworkEvent;
        }

        private void OnDestroy()
        {
            NetworkGameManager.OnEventReceived -= OnNetworkEvent;
        }

        /// <summary>
        /// Handles room state events broadcast via NetworkGameManager.
        /// Fires on server (from CmdRiseEvent) and on all clients (from RiseEventRpc).
        /// </summary>
        private void OnNetworkEvent(int eventCode, string data, Mirror.NetworkConnectionToClient sender)
        {
            if (eventCode == (int)EnumNetworkEventCodes.UpdateRoomState)
            {
                string[] parts = data.Split('|');
                RoomState.STATE state = (RoomState.STATE)int.Parse(parts[0]);
                string info = parts.Length > 1 ? parts[1] : "";
                UpdateThisStateOnNetwork(state, info);
            }
        }

        public RoomState.STATE CurrentRoomState;


        public void UpdateLocalRoomState(RoomState.STATE state)
        {
            CurrentRoomState = state;
        }

        public void UpdateCurrentRoomState(RoomState.STATE state)
        {
            if (!MatchHandler.isOffline())
            {
                string data = ((int)state).ToString();
                if (NetworkServer.active)
                {
                    // Server: update locally then broadcast to all clients via NetworkGameManager RPC
                    UpdateThisStateOnNetwork(state, "");
                    NetworkGameManager.Instance.RiseEventRpc((int)EnumNetworkEventCodes.UpdateRoomState, data);
                }
                else
                {
                    // Client: send Command through NetworkGameManager (already has NetworkIdentity)
                    NetworkGameManager.Instance.CmdRiseEvent((int)EnumNetworkEventCodes.UpdateRoomState, data);
                }
            }
            else
            {
                UpdateThisStateAI(state, "");
            }
        }

        public void UpdateCurrentState(RoomState.STATE state, string infoText)
        {
            if (!MatchHandler.isOffline())
            {
                string data = ((int)state) + "|" + infoText;
                if (NetworkServer.active)
                {
                    UpdateThisStateOnNetwork(state, infoText);
                    NetworkGameManager.Instance.RiseEventRpc((int)EnumNetworkEventCodes.UpdateRoomState, data);
                }
                else
                {
                    NetworkGameManager.Instance.CmdRiseEvent((int)EnumNetworkEventCodes.UpdateRoomState, data);
                }
            }
            else
            {
                UpdateThisStateAI(state, infoText);
            }
        }


        public void UpdateCurrentStateOnShowBtn(RoomState.STATE state)
        {
            if (!MatchHandler.isOffline())
            {
                string data = ((int)state).ToString();
                if (NetworkServer.active)
                {
                    UpdateThisStateOnNetwork(state, "");
                    NetworkGameManager.Instance.RiseEventRpc((int)EnumNetworkEventCodes.UpdateRoomState, data);
                }
                else
                {
                    NetworkGameManager.Instance.CmdRiseEvent((int)EnumNetworkEventCodes.UpdateRoomState, data);
                }
            }
            else
            {
                UpdateThisStateAI(state, "");
            }

        }

        public void UpdateCurrentStateOnShowBtn(RoomState.STATE state, string infoText)
        {
            if (!MatchHandler.isOffline())
            {
                string data = ((int)state) + "|" + infoText;
                if (NetworkServer.active)
                {
                    UpdateThisStateOnNetwork(state, infoText);
                    NetworkGameManager.Instance.RiseEventRpc((int)EnumNetworkEventCodes.UpdateRoomState, data);
                }
                else
                {
                    NetworkGameManager.Instance.CmdRiseEvent((int)EnumNetworkEventCodes.UpdateRoomState, data);
                }
            }
            else
            {
                UpdateThisStateAI(state, infoText);
            }

        }



        public RoomState.STATE GetCurrentRoomState()
        {
            return CurrentRoomState;
        }

       // [ClientRpc]
        public void UpdateThisStateOnNetwork(RoomState.STATE state, string info)
        {
            CurrentRoomState = state;
            // Keep the SyncVar in sync so reconnecting clients know the current room state.
            if (NetworkServer.active && NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.syncedPokerRoomState = (int)state;

                // ABFirstTurn is the exact moment a betting round closes and every player's chips are
                // swept from in front of their seat into the middle - it is raised from
                // PokerActionPanel.OnClickCheckBtn once checkPockeBetPlaced() reports everybody done,
                // and OnRoomStateChangeToABFirstTurn then runs AllPokerBetsGoToFinalPoint. A raise
                // never reaches here, which is why money stays in front of the players until the
                // round actually closes.
                //
                // The reconnect restore separates "already in the pot" from "still in front of the
                // players" by subtracting the per-round totals, so those totals have to be zeroed at
                // this same boundary. Zeroing them only when the community-card stage got reported
                // left a ~2s hole - the length of the deal animation - in which a reconnecting player
                // saw the swept chips sitting back in front of the seats instead of in the pot.
                if (MatchHandler.IsPoker()
                    && (state == RoomState.STATE.ABFirstTurn || state == RoomState.STATE.ABSecondTurn)
                    && GameManager.Instance != null)
                {
                    foreach (PlayerInfo p in GameManager.Instance.playersList)
                        if (p != null) p.syncedPokerRoundBet = "0";
                }
            }
            Debug.Log("Current State is Set to " + state);
            OnUpdateCurrentState(state, info);
        }
        public void UpdateThisStateAI(RoomState.STATE state, string info)
        {
            CurrentRoomState = state;
            Debug.Log("Current State is Set to " + state);
            OnUpdateCurrentState(state, info);
        }

        void UpdateRoomStateProperty(RoomState.STATE state)
        {
            // Room state is already synced via syncedPokerRoomState SyncVar
            // (set in UpdateThisStateOnNetwork) + event system. PUN room property no longer needed.
        }


        void OnUpdateCurrentState(RoomState.STATE state, string infoText)
        {
            switch (state)
            {
                case RoomState.STATE.WaitingForPlayers:
                    TriggerStateWaitingForPlayers();
                    break;

                case RoomState.STATE.GameIsStarting:
                    TriggerStateGameIsStarting();
                    break;

                case RoomState.STATE.CardDistributing:
                    TriggerStateCardDistributing();
                    break;

                case RoomState.STATE.GameIsPlaying:
                    TriggerStateGameIsPlaying();
                    break;

                case RoomState.STATE.GameSideShow:
                    TriggerStateSideShow();
                    break;

                case RoomState.STATE.WaitingForResults:
                    TriggerStateWaitingForResults(infoText);
                    break;

                case RoomState.STATE.ShowingResults:
                    TriggerStateShowingResults();
                    break;


                case RoomState.STATE.ABFirstTurn:
                    TriggerStateabFirstTurn();
                    break;

                case RoomState.STATE.ABSecondTurn:
                    TriggerStateabSecondTurn();
                    break;
            }
        }



        public bool IsStarted()
        {
            //Debug.LogError("My Room State RPC is: " + GetCurrentState().ToString());
            //Debug.LogError("My Room State Property is: " + room.GetRoomStateProperty(LocalSettings.roomState));

            return GetCurrentRoomState() != RoomState.STATE.GameIsStarting && GetCurrentRoomState() != RoomState.STATE.WaitingForPlayers;

        }




        public bool GetIsNotInStartedState()
        {
            return GetCurrentRoomState() == RoomState.STATE.GameIsStarting ||
                GetCurrentRoomState() == RoomState.STATE.WaitingForPlayers ||
                GetCurrentRoomState() == RoomState.STATE.CardDistributing;
        }


        #region OverrideFunctions


      

        #endregion        

    }
}
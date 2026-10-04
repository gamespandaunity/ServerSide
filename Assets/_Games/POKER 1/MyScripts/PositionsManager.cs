
using ExitGames.Client.Photon;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace POKER
{
    public class PositionsManager : NetworkBehaviour
    {
        public static PositionsManager Instance;

        public int playerPosition;

        [ShowOnly]
        public bool[] isBooked = new bool[2];


        [ShowOnly]
        public bool[] localIsBooked;

        [ShowOnly]
        public Transform[] localSeats;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            // Initialize the local copy of the isBooked array
            // if (!MatchHandler.isWingoLottary())
            // {
            localIsBooked = new bool[isBooked.Length];
            Array.Copy(isBooked, localIsBooked, isBooked.Length);
            //}
            //else
            //{
            // isBooked = new bool[LocalSettings.GetMaxPlayers()];
            // localIsBooked = new bool[isBooked.Length];
            // Array.Copy(isBooked, localIsBooked, isBooked.Length);
            //}


        }


        public void AssignMyLocalPositionWithAllOtherClients()
        {
            GameManager gameManagerInstance = GameManager.Instance;
            int myNetworkSeat = gameManagerInstance.myLocalSeat;


            // Iterate over all seat indices and adjust them
            for (int i = 0; i < gameManagerInstance.playersList.Count; i++)
            {
                // if (CheckPositioAvailability())
                // {
                // Calculate the difference between myNetwork seat and desired seat indices
                if (MatchHandler.isOffline())
                {
                    if (gameManagerInstance.playersList[i].name == LocalSettings.AI_Name)
                        myNetworkSeat++;
                    //if (UIManager.Instance.GetAIPlayerInfo() != null)
                    //    if (!UIManager.Instance.GetAIPlayerInfo().gameObject.activeInHierarchy && UIManager.Instance.GetAIPlayerInfo().gameObject.name == LocalSettings.AI_Name)
                    //        myNetworkSeat++;
                }
                int indexDiff = gameManagerInstance.playersList[i].myNetworkSeat - myNetworkSeat;
                int adjustedIndex = (indexDiff + 2) % 2;
                //gameManagerInstance.position_availability[adjustedIndex].networkSeat = adjustedIndex;
                Debug.Log("Altered Seats are " + adjustedIndex);
                gameManagerInstance.position_availability[adjustedIndex].is_reserved = gameManagerInstance.playersList[i].gameObject;
                if (MatchHandler.isOffline())
                {
                    LocalSettings.SetPosAndRect(gameManagerInstance.playersList[i].gameObject, gameManagerInstance.position_availability[adjustedIndex].Pos, gameManagerInstance.PlayerTable);

                }
                else
                {
                    if (gameManagerInstance.playersList[i].IsMine() == true || NetworkServer.active)
                    {
                        LocalSettings.SetPosAndRect(gameManagerInstance.playersList[i].gameObject, gameManagerInstance.position_availability[0].Pos, gameManagerInstance.PlayerTable);

                    }
                    else
                    {
                        LocalSettings.SetPosAndRect(gameManagerInstance.playersList[i].gameObject, gameManagerInstance.position_availability[1].Pos, gameManagerInstance.PlayerTable);

                    }
                }
                // }
                //  else
                //{
                //    if (MatchHandler.isWingoLottary())
                //    {
                //        if (i >= GameManager.Instance.position_availability.Length)
                //            LocalSettings.SetPosAndRect(gameManagerInstance.playersList[i].gameObject, gameManagerInstance.positionAvailabilityFull, gameManagerInstance.positionAvailabilityFull.parent);
                //    }
                //}


            }

            //Debug.LogError("Altering the seats");        
        }


        public void ReArrangePlayerSeatsAccordingToNetworkPositions()
        {
            GameManager gameManagerInstance = GameManager.Instance;
            int myNetworkSeat = gameManagerInstance.myLocalSeat;

            // Iterate over all seat indices and adjust them
            for (int i = 0; i < gameManagerInstance.position_availability.Length; i++)
            {
                // Calculate the difference between myNetwork seat and desired seat indices
                int indexDiff = myNetworkSeat;
                int adjustedIndex = (indexDiff + 2) % 2;
                //Debug.Log("Altered Seats are " + adjustedIndex);
                gameManagerInstance.position_availability[i].sitHere.GetComponent<SitHere>().positionToSit = adjustedIndex;
                gameManagerInstance.position_availability[i].networkSeat = adjustedIndex;
                myNetworkSeat++;
            }
            gameManagerInstance.position_availability[0].is_reserved = null;

        }


        void ClearTheReserveSeatsFirst()
        {
            GameManager gameManagerInstance = GameManager.Instance;
            for (int i = 0; i < gameManagerInstance.playersList.Count; i++)
            {
                gameManagerInstance.position_availability[i].is_reserved = null;
            }
        }




        public void AssignPositionOfthisPlayer(PlayerInfo info)
        {
            Debug.Log("I am calling---------------");
            int position = ReturnAvailableSeatAndSet();
            //info.AssignNetworkSeat(position);

            // Update the local copy of the isBooked array
            localIsBooked[position] = true;

            if (!MatchHandler.isOffline())
            {
                info.playerCustomProperties.SetCustomData(LocalSettings.networkPosition, position);
                // Sync seat assignment to all clients via PlayerInfo's ClientRpc
                if (NetworkServer.active)
                    info.AssignNetworkSeatToAllInstanceOfThisPlayer(position);
                else
                    info.CmdReAssignNetworkSeat(position);
            }
            else
            {
                Debug.LogError("Cheeck Here...." + info.gameObject.name);
                if (info.gameObject.name == LocalSettings.AI_Name)
                    info.AssignNetworkSeatToAllInstanceOfThisPlayer(position);
                else
                    info.AssignNetworkSeatToAllInstanceOfThisPlayer(position);
            }
            // Update custom property for room
            //ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
            //roomProps[info.photonView.ViewID.ToString()] = position;
            //PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        }

        public void ReleasePosition(int position)
        {
            // BUG FIX: the whole body is wrapped now, not just the Cmd call. Seen live:
            // even reading this NetworkBehaviour's own isClient/netIdentity property (e.g.
            // for a diagnostic log) can NRE inside Mirror when this object's identity isn't
            // in a clean state (observed right as a player is being pulled OutOfTable
            // mid-round). Any uncaught exception here — from the property read OR the Cmd
            // send — used to abort the rest of the caller
            // (PlayerCurrentState.TriggerStateOutOfTable), so the local seat/UI cleanup after
            // it never ran and the game got stuck with only 1 player able to act.
            try
            {
                // Update the local copy of the isBooked array
                localIsBooked[position] = false;

                Debug.Log($"[ReleasePosition] Releasing position={position}, isServer={NetworkServer.active}");

                // Update isBooked array on all clients
                if (!MatchHandler.isOffline())
                {
                    if (NetworkServer.active)
                        UpdateIsBookedArray(localIsBooked);
                    else
                        CmdUpdateIsBookedArray(localIsBooked);
                }
                else
                    UpdateIsBookedArrayAI(localIsBooked);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ReleasePosition] Failed for position={position} — swallowed so caller's cleanup still runs: {e}");
            }
        }

        [ClientRpc]
        public void UpdateIsBookedArray(bool[] newIsBooked)
        {
            // Update the isBooked array with the new values
            isBooked = newIsBooked;
        }

        [Command(requiresAuthority = false)]
        public void CmdUpdateIsBookedArray(bool[] newIsBooked)
        {
            UpdateIsBookedArray(newIsBooked);
        }

        public void UpdateIsBookedArrayAI(bool[] newIsBooked)
        {
            // Update the isBooked array with the new values
            isBooked = newIsBooked;
        }

        private int ReturnAvailableSeatAndSet()
        {
            for (int i = 0; i < isBooked.Length; i++)
            {
                if (!isBooked[i])
                {
                    if (!MatchHandler.isOffline())
                        NetworkGameManager.Instance.RiseEventRpc((int)EnumNetworkEventCodes.BookSeat, i.ToString());
                    BookThisSeat(i);
                    return i;
                }
            }
            return 0;
        }

        public void SitHere(int pos)
        {
            Debug.Log("Seat here " + pos);
            // Room seating record synced via CustomData — PUN room property no longer needed
            UIManager.Instance.GetMyPlayerInfo().AssignNetworkSeat(pos);
            //SendRPC(.photonView, "AssignNetworkSeatToAllInstanceOfThisPlayer", RpcTarget.AllBuffered, pos);
            if (!MatchHandler.isOffline())
                UIManager.Instance.GetMyPlayerInfo().playerCustomProperties.SetCustomData(LocalSettings.networkPosition, pos);

            UIManager.Instance.GetMyPlayerInfo().ActivatePlayerAgainOnNetwork();

            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                {
                    BookThisSeat(pos);
                    NetworkGameManager.Instance.RiseEventRpc((int)EnumNetworkEventCodes.BookSeat, pos.ToString());
                }
                else
                    NetworkGameManager.Instance.CmdRiseEvent((int)EnumNetworkEventCodes.BookSeat, pos.ToString());
            }
            else
                BookThisSeat(pos);
        }

        public void AISitHere(int pos)
        {

            UIManager.Instance.GetAIPlayerInfo().AssignNetworkSeat(pos);

            UIManager.Instance.GetAIPlayerInfo().ActivatePlayerAgainOnNetwork();
            if (!MatchHandler.isOffline())
            {
                if (NetworkServer.active)
                {
                    BookThisSeat(pos);
                    NetworkGameManager.Instance.RiseEventRpc((int)EnumNetworkEventCodes.BookSeat, pos.ToString());
                }
                else
                    NetworkGameManager.Instance.CmdRiseEvent((int)EnumNetworkEventCodes.BookSeat, pos.ToString());
            }
            else
                BookThisSeat(pos);
        }


        public void BookThisSeat(int index)
        {
            //   Debug.LogError("Out of Range Index is " + index);
            isBooked[index] = true;
        }


        //public override void OnMasterClientSwitched(Player newMasterClient)
        //{
        //    Debug.Log("Masterrrrrrrrrrrrrrrrrrr Left");
        //    //foreach (var rpc in bufferedRPCs)
        //    //{
        //    //    if (rpc.TargetView != null)
        //    //    {
        //    //        rpc.TargetView.RPC(rpc.RpcMethodName, newMasterClient, rpc.RpcData);
        //    //    }
        //    //}
        //    //bufferedRPCs.Clear();
        //    RefreshListOnMasterClientSwitched();


        //}
        private List<RpcContainer> bufferedRPCs = new List<RpcContainer>();
        void RefreshListOnMasterClientSwitched()
        {
            for (int i = 0; i < isBooked.Length; i++)
            {
                isBooked[i] = false;
                localIsBooked[i] = false;
            }

            for (int i = 0; i < GameManager.Instance.playersList.Count; i++)
            {
                GameManager.Instance.playersList[i].ReAssignNetworkSeat();
                localIsBooked[GameManager.Instance.playersList[i].myNetworkSeat] = true;
                isBooked[GameManager.Instance.playersList[i].myNetworkSeat] = true;
            }


        }

        //public override void OnPlayerLeftRoom(Player otherPlayer)
        //{
        //    AssignMyLocalPositionWithAllOtherClients();
        //}



        //public override void OnPlayerEnteredRoom(Player newPlayer)
        //{
        //    Debug.Log("Player Entered the Room named " + newPlayer.NickName);
        //    if (!thisphoton.IsMine)
        //    {
        //        AssignMyLocalPositionWithAllOtherClients();
        //        Invoke("RefreshAfterSomeTime", 2f);
        //    }
        //}

        //void RefreshAfterSomeTime()
        //{
        //    AssignMyLocalPositionWithAllOtherClients();
        //}


        //public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        //{
        //    Debug.Log("Player name ---- " + targetPlayer.NickName + " Updated its " + changedProps.Values);
        //}







        // SendRPC and RpcContainer removed — Photon-specific helpers replaced by
        // Mirror [Command] + [ClientRpc] patterns.
    }
}
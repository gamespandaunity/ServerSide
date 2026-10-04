
using TeenPattiGame;
using ExitGames.Client.Photon;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityExtensions;
using Mirror;

public class PositionsManager : MonoBehaviour
{
    public static PositionsManager Instance;

    public int playerPosition;

    [ShowOnly]
    public bool[] isBooked = new bool[2];


    [ShowOnly]
    public bool[] localIsBooked;

    [ShowOnly]
    public Transform[] localSeats;

    // NetworkIdentity thisphoton;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        // thisphoton = GetComponent<NetworkIdentity>();
        localIsBooked = new bool[isBooked.Length];
        Array.Copy(isBooked, localIsBooked, isBooked.Length);
        RefreshAfterSomeTime();
    }


    public void AssignMyLocalPositionWithAllOtherClients()
    {
        TeenPattiGame.GameManager gameManagerInstance = TeenPattiGame.GameManager.Instance;
        int myNetworkSeat = gameManagerInstance.myLocalSeat;
        Debug.Log(gameManagerInstance.playersList.Count);

        // Iterate over all seat indices and adjust them
        for (int i = 0; i < gameManagerInstance.playersList.Count; i++)
        {
            if (MatchHandler.isOffline())
            {
                if (gameManagerInstance.playersList[i].name == LocalSettings.AI_Name)
                {
                    myNetworkSeat++;
                    Debug.Log("AI found" + gameManagerInstance.playersList[i].name);
                }
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
                if (gameManagerInstance.playersList[i].this_photonView.isOwned == true || NetworkServer.active)
                {
                    TPLog.Flow("PositionsManager", "'" + gameManagerInstance.playersList[i].name + "' is MY player -> bottom seat (0)");
                    LocalSettings.SetPosAndRect(gameManagerInstance.playersList[i].gameObject, gameManagerInstance.position_availability[0].Pos, gameManagerInstance.PlayerTable);

                }
                else
                {
                    TPLog.Flow("PositionsManager", "'" + gameManagerInstance.playersList[i].name + "' is the opponent -> top seat (1)");
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
        TeenPattiGame.GameManager gameManagerInstance = TeenPattiGame.GameManager.Instance;
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
        TeenPattiGame.GameManager gameManagerInstance = TeenPattiGame.GameManager.Instance;
        for (int i = 0; i < gameManagerInstance.playersList.Count; i++)
        {
            gameManagerInstance.position_availability[i].is_reserved = null;
        }
    }




    public void AssignPositionOfthisPlayer(PlayerInfo info)
    {
        //Debug.Log("I am calling---------------");
        int position = ReturnAvailableSeatAndSet();
        Debug.Log("I am calling---------------" + position);
        TPLog.Flow("PositionsManager", "AssignPositionOfthisPlayer - '" + info.name + "' gets seat " + position);
        //info.AssignNetworkSeat(position)
        // Update the local copy of the isBooked array
        localIsBooked[position] = true;

        if (!MatchHandler.isOffline())
        {
            this.DelayUntil(() => TeenPattiNNetworkManager.instance, () =>
                {
                    if (NetworkServer.active)
                    {
                        TPLog.Flow("PositionsManager", "Server writes the seating record and pushes seat " + position + " to every client");
                        TeenPattiNNetworkManager.instance.roomCustomPropertiesTeenPatti.SetRoomSeatingRecord("room_seating", isBooked);
                        // TeenPattiNNetworkManager.instance.roomCustomPropertiesTeenPatti.SetRoomSeatingRecord("room_seating", isBooked);
                        info.AssignNetworkSeatToAllInstanceOfThisPlayer(position);
                        info.RpcAssignNetworkSeatToAllInstanceOfThisPlayer(position);
                        info.SetCustomData((int)info.this_photonView.netId, position);

                        info.RpcSetCustomData((int)info.this_photonView.netId, position);
                    }
                    else
                    {
                        TPLog.Flow("PositionsManager", "Not the server -> seat assignment will arrive from the server");
                    }
                });
        }
        else
        {

            Debug.LogError("Cheeck Here...." + info.gameObject.name);
            if (info.gameObject.name == LocalSettings.AI_Name)
                info.AssignNetworkSeatToAllInstanceOfThisPlayer(position);
            else
                info.AssignNetworkSeatToAllInstanceOfThisPlayer(position);
            //AssignNetworkSeatToAllInstanceOfThisPlayer(position);
        }
        // Update custom property for room
        //ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable();
        //roomProps[info.photonView.ViewID.ToString()] = position;
        //PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
    }

    public void ReleasePosition(int position)
    {
        TPLog.Flow("PositionsManager", "ReleasePosition - seat " + position + " is free again");
        // Update the local copy of the isBooked array
        localIsBooked[position] = false;

        Debug.Log("Master Left but still here coming");

        // Call an RPC to update the isBooked array on all clients
        if (!MatchHandler.isOffline())
            TeenPattiNNetworkManager.instance.CmdUpdateIsBookedArray(localIsBooked);
        //SendRPC(thisphoton, "UpdateIsBookedArray", RpcTarget.AllBuffered, localIsBooked);        
        else
            UpdateIsBookedArray(localIsBooked);
    }

    //  [PunRPC]
    public void UpdateIsBookedArray(bool[] newIsBooked)
    {
        TPLog.Flow("PositionsManager", "Seat map updated -> [" + string.Join(",", newIsBooked) + "]");
        // Update the isBooked array with the new values
        isBooked = newIsBooked;
    }

    private int ReturnAvailableSeatAndSet()
    {
        for (int i = 0; i < isBooked.Length; i++)
        {
            if (!isBooked[i])
            {
                TPLog.Flow("PositionsManager", "Seat " + i + " is free -> booking it");
                if (!MatchHandler.isOffline())
                {
                    //SendRPC(thisphoton, "BookThisSeat", RpcTarget.AllBuffered, i);       //photon removal
                    if (NetworkServer.active)
                    {
                        BookThisSeat(i);
                        TeenPattiNNetworkManager.instance.RpcBookThisSeat(i);
                    }
                    else
                    {
                        TPLog.Flow("PositionsManager", "Not the server -> the booking Rpc comes from the server");
                    }
                }
                else
                    BookThisSeat(i);
                return i;
            }
        }
        TPLog.Warn("PositionsManager", "All seats are booked -> falling back to seat 0");
        return 0;
    }


    public void SitHere(int pos)
    {
        Debug.Log("Seat here " + pos);
        TPLog.Flow("PositionsManager", "SIT HERE pressed -> taking seat " + pos);
        if (!MatchHandler.isOffline())
        {
            TeenPattiNNetworkManager.instance.roomCustomPropertiesTeenPatti.CmdSetRoomSeatingRecord("room_seating", isBooked);
        }
        TeenPattiGame.UIManager.Instance.GetMyPlayerInfo().AssignNetworkSeat(pos);
        if (!MatchHandler.isOffline())
        {
            TeenPattiGame.UIManager.Instance.GetMyPlayerInfo().playerCustomProperties.SetCustomData(LocalSettings.networkPosition, pos);
        }
        TeenPattiGame.UIManager.Instance.GetMyPlayerInfo().ActivatePlayerAgainOnNetwork();
        if (!MatchHandler.isOffline())
        {
            TPLog.Flow("PositionsManager", "Telling every client that seat " + pos + " is now mine");
            //thisphoton.RPC("BookThisSeat", RpcTarget.All, pos);
            TeenPattiNNetworkManager.instance.CmdBookThisSeat(pos);
        }
        else
            BookThisSeat(pos);
    }

    public void AISitHere(int pos)
    {

        TeenPattiGame.UIManager.Instance.GetAIPlayerInfo().AssignNetworkSeat(pos);

        TeenPattiGame.UIManager.Instance.GetAIPlayerInfo().ActivatePlayerAgainOnNetwork();
        if (!MatchHandler.isOffline())
        {
            //   thisphoton.RPC("BookThisSeat", RpcTarget.All, pos);
            TeenPattiNNetworkManager.instance.CmdBookThisSeat(pos);
        }
        else
            BookThisSeat(pos);
    }


    public void BookThisSeat(int index)
    {
        TPLog.Flow("PositionsManager", "Seat " + index + " marked as booked");
        //   Debug.LogError("Out of Range Index is " + index);
        isBooked[index] = true;
    }


    // public override void OnMasterClientSwitched(Player newMasterClient)
    // {
    //     Debug.Log("Masterrrrrrrrrrrrrrrrrrr Left");

    //     RefreshListOnMasterClientSwitched();


    // }
    private List<RpcContainer> bufferedRPCs = new List<RpcContainer>();
    void RefreshListOnMasterClientSwitched()
    {
        for (int i = 0; i < isBooked.Length; i++)
        {
            isBooked[i] = false;
            localIsBooked[i] = false;
        }

        for (int i = 0; i < TeenPattiGame.GameManager.Instance.playersList.Count; i++)
        {
            TeenPattiGame.GameManager.Instance.playersList[i].ReAssignNetworkSeat();
            localIsBooked[TeenPattiGame.GameManager.Instance.playersList[i].myNetworkSeat] = true;
            isBooked[TeenPattiGame.GameManager.Instance.playersList[i].myNetworkSeat] = true;
        }


    }

    // public override void OnPlayerLeftRoom(Player otherPlayer)    
    // {
    //     AssignMyLocalPositionWithAllOtherClients();
    // }



    public void OnPlayerEnteredRoom()
    {
        if (!UIManager.Instance.myPlayerInfo.isOwned)
        {
            TPLog.Flow("PositionsManager", "Another player entered -> re-laying out the seats on my screen");
            AssignMyLocalPositionWithAllOtherClients();
            Invoke("RefreshAfterSomeTime", 2f);
        }
        else
        {
            TPLog.Flow("PositionsManager", "OnPlayerEnteredRoom for my own player -> seat layout untouched");
        }
    }

    void RefreshAfterSomeTime()
    {
        AssignMyLocalPositionWithAllOtherClients();
    }


    // public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)     
    // {
    //     Debug.Log("Player name ---- " + targetPlayer.NickName + " Updated its " + changedProps.Values);
    // }

    // public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)                
    // {
    //     //Debug.Log("This Room Seating Updated as " + propertiesThatChanged.ToStringFull());
    // }






    // public void SendRPC(NetworkIdentity targetView, string rpcMethodName, string target, params object[] rpcData)            
    // {
    //     if (MirrorNetwork.Instance.isMasterClient)
    //     {
    //         targetView.RPC(rpcMethodName, target, rpcData);

    //     }
    //     else
    //     {
    //     }
    // }




}

[Serializable]
public class RpcContainer
{
    public NetworkIdentity TargetView;
    public string RpcMethodName;
    public object[] RpcData;

    public RpcContainer(NetworkIdentity targetView, string rpcMethodName, object[] rpcData)       //photon removal
    {
        TargetView = targetView;
        RpcMethodName = rpcMethodName;
        RpcData = rpcData;
    }
}


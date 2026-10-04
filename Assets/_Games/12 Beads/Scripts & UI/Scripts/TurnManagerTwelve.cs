using ExitGames.Client.Photon;
using UnityEngine;
namespace Twelve
{
    public class TurnManagerTwelve //Photon Removal: MonoBehaviourPunCallbacks, IOnEventCallback
    {
    private const byte TurnChangeEventCode = 1; // Custom event code for turn change

    private new void OnEnable()
    {
            //Photon Removal PhotonNetwork.AddCallbackTarget(this);
        }

        private new void OnDisable()
    {
            //Photon Removal PhotonNetwork.RemoveCallbackTarget(this);
        }

        public enum Player
    {
        ME,
        OTHER
    }

    public Player currentPlayer = Player.OTHER;

    // Method to check if it's your turn
    public bool IsMineTurn()
    {
        return currentPlayer == Player.ME;
    }

    // Method to broadcast turn change
    public void BroadcastTurnChange(Player newPlayer)
    {
        object[] content = new object[] { (int)newPlayer };
            //Photon Removal RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            //Photon Removal     SendOptions sendOptions = new SendOptions { Reliability = true };
            //Photon Removal      PhotonNetwork.RaiseEvent(TurnChangeEventCode, content, raiseEventOptions, sendOptions);
        }

        // Method to handle received events
        public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == TurnChangeEventCode)
        {
            object[] data = (object[])photonEvent.CustomData;
            Player receivedPlayer = (Player)(int)data[0];

            if (receivedPlayer != Player.ME)
            {
                currentPlayer = receivedPlayer;
                //Debug.Log("It's now the turn of: " + receivedPlayer);
            }
        }
    }

    // Example method to change turns (could be triggered by gameplay logic)
    public void ChangeTurn()
    {
        currentPlayer = (currentPlayer == Player.ME) ? Player.OTHER : Player.ME;
        BroadcastTurnChange(currentPlayer);
    }
}
}


 
using TMPro;
public class ChechActiveRooms //Photon Removal : MonoBehaviourPunCallbacks
{
    public TMP_Text ActiveRooms;
    public void getActiveRooms()
    {
        // Ensure that PUN is connected to the Photon Cloud or your Photon Server
        //if (!PhotonNetwork.IsConnected)
        //{
        //    Constants_M.Log("PUN is not connected. Make sure you are connected to the Photon Cloud or your Photon Server.");
        //    return;
        //}

        //// Get the list of rooms currently available in the lobby
        //RoomInfo[] rooms = PhotonNetwork.GetRoomList();

        //// Count the number of active rooms
        //int activeRoomCount = rooms.Length;
        //ActiveRooms.text = "Total Active rooms: " + activeRoomCount.ToString();
        ////Debug.Log("Active room count in the lobby: " + activeRoomCount);
    }
}




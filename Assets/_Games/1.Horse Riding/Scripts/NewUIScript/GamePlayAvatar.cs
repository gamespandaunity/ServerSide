 
using UnityEngine;
using UnityEngine.UI;
 
 
public class GamePlayAvatar:MonoBehaviour //Photon Removal : MonoBehaviourPunCallbacks
{
    [SerializeField] private Image playerAvatar;
    [SerializeField] private Text playerName;
 
    //public void BroadcastPlayerInfo(string joinedPlayerName, int joinedPlayerAvatarID)
    //{
    //    //Debug.Log($"{joinedPlayerName} joined the room.");
    //    photonView.RPC(nameof(SetProperties), RpcTarget.AllBuffered, joinedPlayerName, joinedPlayerAvatarID);
 
    //}
    //[PunRPC]
    //private void SetProperties(string text , int Avatar)
    //{
    //    //Debug.Log("properties set");
    //    playerAvatar.sprite = PlayerDataController.Instance.playerAwaterList[Avatar];
    //    playerName.text = text;
    //}
}

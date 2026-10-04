using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Snake_Ladder
{
    public class SetPlayerInfo //: MonoBehaviourPun
    {
        [SerializeField] TextMeshProUGUI PlayerName;
        [SerializeField] Image Avatar;

        private void Start()
        {
            //if (photonView.IsMine)
            //{
            //    //Debug.Log("I'm Master Client");
            //    SetParentAndStretch(ReferenceManager.Instance.playerAvatarTransform);             //Photon Removal
            //}
            //else
            //{
            //    //Debug.Log("I'm not Master Client");
            //    SetParentAndStretch(ReferenceManager.Instance.opponentAvatarTransform);
            //}
        }

        private void SetParentAndStretch(Transform parent)
        {
            //transform.SetParent(parent);
            //RectTransform rectTransform = GetComponent<RectTransform>();                             //Photon Removal

            //if (rectTransform != null)
            //{
            //    rectTransform.anchorMin = Vector2.zero; // Anchors to stretch from the bottom-left
            //    rectTransform.anchorMax = Vector2.one; // Anchors to stretch to the top-right
            //    rectTransform.offsetMin = Vector2.zero; // No offset on the bottom-left
            //    rectTransform.offsetMax = Vector2.zero; // No offset on the top-right
            //    rectTransform.localPosition = Vector3.zero; // Set local position to zero if necessary
            //}
            //else
            //{
            //    ConstantsData_M.Log("No RectTransform found on the GameObject.");
            //}
        }

        public string GetPlayerName()
        {
            return PlayerName.text;
        }

        //[PunRPC]           //Photon Removal
        void setPlayerInfo(string name, int index)
        {
            Avatar.sprite = ReferenceManager.Instance.avatarSprites[index];
            PlayerName.text = name;
        }

        // BroadCasts
        public void BroadCastStartMatch()
        {
            // photonView.RPC("setPlayerInfo", RpcTarget.AllBuffered);                  //Photon Removal
        }

        public void BroadCastRoomUi(string name, int index)
        {
            //Debug.Log("Broadcasting");
            //photonView.RPC(nameof(setPlayerInfo), RpcTarget.AllBuffered, name, index);      //Photon Removal
        }
    }
}
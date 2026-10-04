//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//namespace Snake_Ladder
//{
//    public class SetPlayerInfo : MonoBehaviourPun
//    {
//        [SerializeField] TextMeshProUGUI PlayerName;
//        [SerializeField] Image Avatar;

//        private void Start()
//        {
//            if (photonView.IsMine)
//            {
//                Debug.Log("I'm Master Client");
//             //Wasi     SetParentAndStretch(FusionGameReferenceManager.Instance.avatar1Transform);
//            }
//            else
//            {
//                Debug.Log("I'm not Master Client");
//             //Wasi     SetParentAndStretch(FusionGameReferenceManager.Instance.avatar2Transform);
//            }
//        }

//        private void SetParentAndStretch(Transform parent)
//        {
//            transform.SetParent(parent);
//            RectTransform rectTransform = GetComponent<RectTransform>();

//            if (rectTransform != null)
//            {
//                rectTransform.anchorMin = Vector2.zero; // Anchors to stretch from the bottom-left
//                rectTransform.anchorMax = Vector2.one; // Anchors to stretch to the top-right
//                rectTransform.offsetMin = Vector2.zero; // No offset on the bottom-left
//                rectTransform.offsetMax = Vector2.zero; // No offset on the top-right
//                rectTransform.localPosition = Vector3.zero; // Set local position to zero if necessary
//            }
//            else
//            {
//                Debug.LogError("No RectTransform found on the GameObject.");
//            }
//        }

//        public string GetPlayerName()
//        {
//            return PlayerName.text;
//        }

//        [PunRPC]
//        void setPlayerInfo(string name, int index)
//        {
//         //Wasi     Avatar.sprite = FusionGameReferenceManager.Instance.Avatars[index];
//            PlayerName.text = name;
//        }

//        // BroadCasts
//        public void BroadCastStartMatch()
//        {
//            photonView.RPC("setPlayerInfo", RpcTarget.AllBuffered);
//        }

//        public void BroadCastRoomUi(string name, int index)
//        {
//            Debug.Log("Broadcasting");
//            photonView.RPC(nameof(setPlayerInfo), RpcTarget.AllBuffered, name, index);
//        }
//    }
//}
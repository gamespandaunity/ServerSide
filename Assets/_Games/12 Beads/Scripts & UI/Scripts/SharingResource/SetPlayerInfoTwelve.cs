using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

namespace Twelve
{
    public class SetPlayerInfoTwelve : NetworkBehaviour
    {
        [SerializeField] TextMeshProUGUI PlayerName;
        [SerializeField] Image Avatar;

        private void Start()
        {
            if (isLocalPlayer)
            {
                Debug.Log("I'm Master Client");
               //Wasi   SetParentAndStretch(FusionGameReferenceManager.Instance.avatar1Transform);
            }
            else
            {
                Debug.Log("I'm not Master Client");
            //Wasi      SetParentAndStretch(FusionGameReferenceManager.Instance.avatar2Transform);
            }
        }

        private void SetParentAndStretch(Transform parent)
        {
            transform.SetParent(parent);
            RectTransform rectTransform = GetComponent<RectTransform>();

            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero; // Anchors to stretch from the bottom-left
                rectTransform.anchorMax = Vector2.one; // Anchors to stretch to the top-right
                rectTransform.offsetMin = Vector2.zero; // No offset on the bottom-left
                rectTransform.offsetMax = Vector2.zero; // No offset on the top-right
                rectTransform.localPosition = Vector3.zero; // Set local position to zero if necessary
            }
            else
            {
                Debug.LogError("No RectTransform found on the GameObject.");
            }
        }

        public string GetPlayerName() => PlayerName.text;

        void SetPlayerInfo(string name, int index)
        {
            //Wasi Avatar.sprite = FusionGameReferenceManager.Instance.Avatars[index];
            PlayerName.text = name;
        }
        public void BroadCastRoomUi(string name, int index)
        {
            // The obsolete authority-free room UI relay was removed; this prefab is legacy local UI.
            SetPlayerInfo(name, index);
        }
    }
        // BroadCasts
        // public void BroadCastStartMatch()
        // {
        //     photonView.RPC("setPlayerInfo", RpcTarget.AllBuffered);
        // }

        // public void BroadCastRoomUi(string name, int index)
        // {
        //     Debug.Log("Broadcasting");
        //     photonView.RPC(nameof(setPlayerInfo), RpcTarget.AllBuffered, name, index);
        // }
    
}

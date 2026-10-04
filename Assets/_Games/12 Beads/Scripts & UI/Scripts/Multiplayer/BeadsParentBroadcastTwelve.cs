using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Twelve
{
    public class BeadsParentBroadcastTwelve : NetworkBehaviour //Photon Removal: MonoBehaviourPun
    {
        private void Start()
        {
            BroadCastRoomUi();
        }
        void SetParent()
        {
            transform.SetParent(NetworkManagerTwelve.Instance.beadsParent.transform);
        }
        public void BroadCastRoomUi()
        {
            Debug.Log("Broadcasting");
            // The obsolete parent RPC was removed; spawned views parent themselves locally.
            SetParent();
           //Wasi photonView.RPC(nameof(SetParent), RpcTarget.AllBuffered);
        }
        //Photon Removal  [PunRPC]
        // void SetParentToTransform()
        // {
        //     //Photon Removal  transform.SetParent(NetworkManagerTwelve.Instance.allBeadsParent.transform);
        // }
        // public void BroadcastRoomUIUpdates()
        // {
        //     //Debug.Log("Broadcasting");
        //     //Photon Removal photonView.RPC(nameof(SetParentToTransform), RpcTarget.AllBuffered);
        // }
    }
}

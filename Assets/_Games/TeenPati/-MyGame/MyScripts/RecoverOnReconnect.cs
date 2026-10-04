// using System.Collections.Generic;
// using Photon.Realtime;
// using UnityEngine;

// public class RecoverOnReconnect : MonoBehaviour, IConnectionCallbacks
// {
//     private LoadBalancingClient loadBalancingClient;
//     private AppSettings appSettings;

//     public RecoverOnReconnect(LoadBalancingClient loadBalancingClient, AppSettings appSettings)
//     {
//         this.loadBalancingClient = loadBalancingClient;
//         this.appSettings = appSettings;
//         this.loadBalancingClient.AddCallbackTarget(this);
//         Debug.LogError(" Step 7   ");
//     }

//     ~RecoverOnReconnect()
//     {
//         this.loadBalancingClient.RemoveCallbackTarget(this);
//     }

//     void IConnectionCallbacks.OnDisconnected(DisconnectCause cause)
//     {
//         if (this.CanRecoverFromDisconnect(cause))
//         {
//             Debug.LogError(" Step    2    ");
//             this.Recover();
//         }
//     }

//     private bool CanRecoverFromDisconnect(DisconnectCause cause)
//     {
//         switch (cause)
//         {
//             // the list here may be non exhaustive and is subject to review
//             case DisconnectCause.Exception:
//             case DisconnectCause.ServerTimeout:
//             case DisconnectCause.ClientTimeout:
//             case DisconnectCause.DisconnectByServerLogic:
//             case DisconnectCause.DisconnectByServerReasonUnknown:
//                 Debug.LogError(" Step    3    ");
//                 return true;
//         }
//         Debug.LogError(" Step    6    ");
//         return false;
//     }

//     private void Recover()
//     {
//         Debug.LogError(" Step    4    ");
//         if (!loadBalancingClient.ReconnectAndRejoin())
//         {
//             Debug.LogError("ReconnectAndRejoin failed, trying Reconnect");
//             if (!loadBalancingClient.ReconnectToMaster())
//             {
//                 Debug.LogError("Reconnect failed, trying ConnectUsingSettings");
//                 if (!loadBalancingClient.ConnectUsingSettings(appSettings))
//                 {
//                     Debug.LogError("ConnectUsingSettings failed");
//                 }
//             }
//         }
//     }

//     #region Unused Methods

//     void IConnectionCallbacks.OnConnected()
//     {
//         Debug.LogError(" Step    8    ");
//     }

//     void IConnectionCallbacks.OnConnectedToMaster()
//     {
//         Debug.LogError(" Step    9    ");
//     }

//     void IConnectionCallbacks.OnRegionListReceived(RegionHandler regionHandler)
//     {
//         Debug.LogError(" Step    10    ");
//     }

//     void IConnectionCallbacks.OnCustomAuthenticationResponse(Dictionary<string, object> data)
//     {
//         Debug.LogError(" Step    11    ");
//     }

//     void IConnectionCallbacks.OnCustomAuthenticationFailed(string debugMessage)
//     {
//         Debug.LogError(" Step    12    ");
//     }

//     #endregion
// }
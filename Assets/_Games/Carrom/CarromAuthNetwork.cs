using Mirror;
using UnityEngine;

public class CarromAuthNetwork : NetworkBehaviour
{
    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        Debug.Log("CueBall Auth Changed assigned to you");
    }
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        Debug.Log("CueBall Auth Changed Removed from you");

    }
    public NetworkIdentity cueballidentity;
    // public NetworkRigidbodyReliable2D networkRigidbody;
    public NetworkTransformUnreliable networkTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    bool ServerHasAuthority()
    {
        // On server: check if connectionToClient is null
        return netIdentity.connectionToClient == null;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSyncDirection();
    }
    void UpdateSyncDirection()
    {
        if (NetworkServer.active)
        {
            // SERVER SIDE
            if (cueballidentity.connectionToClient != null)
            {
                // A client has authority (ball-in-hand mode)
                // Server should RECEIVE updates from client
                networkTransform.syncDirection = SyncDirection.ClientToServer;
                // networkRigidbody.syncDirection = SyncDirection.ClientToServer;
            }
            else
            {
                // Server has authority (physics mode)
                // Server should SEND updates to clients
                networkTransform.syncDirection = SyncDirection.ServerToClient;
                // networkRigidbody.syncDirection = SyncDirection.ServerToClient;
            }
        }
        else
        {
            // CLIENT SIDE
            if (cueballidentity.isOwned)
            {
                // I own the ball (ball-in-hand)
                // I should SEND position updates
                networkTransform.syncDirection = SyncDirection.ClientToServer;
                //  networkRigidbody.syncDirection = SyncDirection.ClientToServer;
            }
            else
            {
                // Server owns the ball (physics mode)
                // I should RECEIVE updates
                networkTransform.syncDirection = SyncDirection.ServerToClient;
                // networkRigidbody.syncDirection = SyncDirection.ServerToClient;
            }
        }
    }
    [ContextMenu("Remove Authority")]
    public void RemoveAuthority()
    {
        cueballidentity.RemoveClientAuthority();
    }
}

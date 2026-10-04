using Mirror;
using UnityEngine;

public class cueBallNetwork : NetworkBehaviour
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
    public NetworkRigidbodyUnreliable networkRigidbody;
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
        if (cueballidentity == null) return;
        if (!isServer)
        {
            if (cueballidentity.isOwned)
            {
                // I own the ball (ball-in-hand) - I send position updates
                networkTransform.syncDirection = SyncDirection.ClientToServer;
                if (networkRigidbody != null)
                {
                    networkRigidbody.syncDirection = SyncDirection.ClientToServer;
                }
            }
            else
            {
                // Server owns the ball (physics mode) - I receive updates
                networkTransform.syncDirection = SyncDirection.ServerToClient;
                if (networkRigidbody != null)
                {
                    networkRigidbody.syncDirection = SyncDirection.ServerToClient;
                }
            }
        }
        else
        {
            if (ServerHasAuthority())
            {
                networkTransform.syncDirection = SyncDirection.ServerToClient;
                if (networkRigidbody != null)
                {
                    networkRigidbody.syncDirection = SyncDirection.ServerToClient;
                }
            }
            else
            {
                networkTransform.syncDirection = SyncDirection.ClientToServer;
                if (networkRigidbody != null)
                {
                    networkRigidbody.syncDirection = SyncDirection.ClientToServer;
                }
            }
        }
    }
}

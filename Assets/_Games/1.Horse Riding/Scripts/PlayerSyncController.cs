using UnityEngine;
using Mirror;

public class PlayerSyncController : NetworkBehaviour
{
    public PlayerPositionController positionController;

    [SyncVar] private float playerPos = 1;

    private void Start()
    {
        positionController = gameObject.GetComponent<PlayerPositionController>();
    }

    private void FixedUpdate()
    {
        
     //   if (positionController && !isLocalPlayer)
        {
            // Remote players use the synced playerPos value
            positionController.TotalDistanecPoints = playerPos;
        }
    }

    /// <summary>
    /// Mirror's equivalent to Photon's IPunObservable.OnPhotonSerializeView
    /// Called automatically by Mirror's NetworkIdentity
    /// </summary>
    public override void OnSerialize(NetworkWriter writer, bool initialState)
    {
        if (positionController != null)
        {
            writer.WriteFloat(positionController.TotalDistanecPoints);
        }
        else
        {
            writer.WriteFloat(playerPos);
        }
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        if (positionController != null)
        {
            playerPos = reader.ReadFloat();
        }
    }
}
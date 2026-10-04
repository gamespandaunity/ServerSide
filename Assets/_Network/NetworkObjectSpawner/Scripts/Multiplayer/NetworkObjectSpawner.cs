using Mirror;
using UnityEngine;

public class NetworkObjectSpawner : NetworkBehaviour
{
    public GameObject NetworkBehaviorScript;
    void Start()
    {
        if (NetworkServer.active)
        {
            GameObject obj = Instantiate(NetworkBehaviorScript);
            NetworkServer.Spawn(obj);
        }
    }

}

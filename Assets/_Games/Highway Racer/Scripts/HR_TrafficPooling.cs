using Mirror;
using Shapes2D;
using System.Collections;
using System.Collections.Generic;
using Twelve;
using UnityEngine;

[AddComponentMenu("BoneCracker Games/Highway Racer/Traffic/HR Traffic Pooling")]
public class HR_TrafficPooling : NetworkBehaviour
{

    #region SINGLETON PATTERN
    public static HR_TrafficPooling instance;
    public static HR_TrafficPooling Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<HR_TrafficPooling>();
            return instance;
        }
    }
    #endregion

    public TrafficCars[] trafficCars;
    public Transform[] lines;

    [System.Serializable]
    public class TrafficCars
    {
        public GameObject trafficCar;
        public int frequence = 1;
    }

    private List<HR_TrafficCar> _trafficCars = new List<HR_TrafficCar>();
    internal GameObject container;

    private bool isMultiplayer = false;

    // ✅ Dictionary to store all player positions (netId -> position)
    private Dictionary<uint, Vector3> playerPositions = new Dictionary<uint, Vector3>();

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Start()
    {
        isMultiplayer = PlayerPrefs.GetInt("Multiplayer", 0) == 1;
        if (isMultiplayer && !NetworkServer.active)
        {
            CreateTraffic();
        }
        if (!isMultiplayer)
        {
            CreateTraffic();
        }



    }
    //public override void OnStartServer()
    //{
    //    base.OnStartServer();
    //    Debug.Log("OnStartServerTraffic");
    //    ServerSpwan();
    //    //  gamePlayController.MirrorCreateTeamsBeads();




    //}

    /// <summary>
    /// ✅ Client side: Find all spawned traffic cars
    /// </summary>


    /// <summary>
    /// ✅ Client se position receive karke store karo
    /// </summary>
    public void RegisterPlayerPosition(uint netId, Vector3 position)
    {
        if (!NetworkServer.active)
            return;

        if (playerPositions.ContainsKey(netId))
            playerPositions[netId] = position;
        else
            playerPositions.Add(netId, position);

        //Debug.Log($"🎯 SERVER: Registered player {netId} at position {position}");
    }

    /// <summary>
    /// ✅ Player disconnect hone pe remove karo
    /// </summary>
    public void UnregisterPlayerPosition(uint netId)
    {
        if (!NetworkServer.active)
            return;

        if (playerPositions.ContainsKey(netId))
        {
            playerPositions.Remove(netId);
            Debug.Log($"❌ SERVER: Unregistered player {netId}");
        }
    }

    private void Update()
    {
        // ✅ OFFLINE: Camera ke base pe traffic animate ho
        // ✅ MULTIPLAYER: Sirf server traffic ko animate karega

        //if (isMultiplayer)
        //{
        //    // Multiplayer mein sirf SERVER traffic control karega
        //    if (!NetworkServer.active)
        //        return; // Clients kuch nahi karenge
        //}

        AnimateTraffic();
    }

    private void CreateTraffic()
    {
        Debug.Log("🚗 Creating Traffic Cars...");

        container = new GameObject("Traffic Container");

        for (int i = 0; i < trafficCars.Length; i++)
        {
            for (int k = 0; k < trafficCars[i].frequence; k++)
            {
                GameObject go;

                //if (!isMultiplayer)
                {
                    // ✅ OFFLINE MODE - Normal instantiate
                    Debug.Log("Offline - Creating Traffic");
                    go = Instantiate(trafficCars[i].trafficCar, Vector3.zero, Quaternion.identity);
                    _trafficCars.Add(go.GetComponent<HR_TrafficCar>());
                    go.SetActive(false);
                    go.transform.SetParent(container.transform, true);
                }
                //else
                //{
                //    // ✅ MULTIPLAYER MODE - Server spawn karega, sab pe dikhai dega
                //    if (NetworkServer.active)
                //    {
                //        Debug.Log($"🌐 SERVER: Creating Traffic {i}-{k} via NetworkServer.Spawn");

                //        GameObject spawned = Instantiate(trafficCars[i].trafficCar, Vector3.zero, Quaternion.identity);


                //        // ⚠️ CRITICAL: Pehle SetActive(true) karo, phir spawn
                //        //  spawned.SetActive(true); // Must be active before spawning

                //        // Server network spawn karega (clients ko bhi spawn hogi)
                //        NetworkServer.Spawn(spawned);

                //        HR_TrafficCar tc = spawned.GetComponent<HR_TrafficCar>();
                //        if (tc != null)
                //        {
                //            _trafficCars.Add(tc);
                //            tc.trafficIndex = _trafficCars.Count - 1; // Index assign karo
                //        }

                //        // Ab deactivate karo
                //        spawned.SetActive(false);
                //        spawned.transform.SetParent(container.transform, true);
                //    }
                //    else
                //    {
                //        Debug.Log("⏳ CLIENT: Waiting for server to spawn traffic...");
                //    }
                //}
            }
        }

        if (NetworkServer.active || !isMultiplayer)
            Debug.Log($"✅ Total traffic cars created: {_trafficCars.Count}");
    }
    public void ServerSpwan()
    {
        Debug.Log("🚗 SERVER: Spawning Traffic Cars...");

        //  container = new GameObject("Traffic Container");

        for (int i = 0; i < trafficCars.Length; i++)
        {
            for (int k = 0; k < trafficCars[i].frequence; k++)
            {
                GameObject go = Instantiate(trafficCars[i].trafficCar, Vector3.zero, Quaternion.identity);

                //  go.transform.SetParent(container.transform, true);

                // ATTN: Do NOT deactivate before spawn
                go.SetActive(true);

                NetworkServer.Spawn(go);   // Now spawn active object

                // Now safe to deactivate (server + clients)
                HR_TrafficCar tc = go.GetComponent<HR_TrafficCar>();
                _trafficCars.Add(tc);

                tc.RpcSetActiveState(false);  // Sync deactivate on all clients  
            }
        }

        Debug.Log($"✅ SERVER: Total traffic cars created: {_trafficCars.Count}");
    }

    /// <summary>
    /// Animates the traffic cars
    /// OFFLINE: Camera ke base pe
    /// MULTIPLAYER: Server synced player positions ke base pe
    /// </summary>
    private void AnimateTraffic()
    {
        //if (isMultiplayer)
        //{
        //    // ✅ MULTIPLAYER: Server uses synced player positions
        //    if (!NetworkServer.active)
        //        return;

        //    if (playerPositions.Count == 0)
        //    {
        //        //Debug.LogWarning("⚠️ No player positions received yet for traffic spawning");
        //        return;
        //    }

        //    // Check traffic against ALL synced player positions
        //    foreach (var playerPos in playerPositions.Values)
        //    {
        //        for (int i = 0; i < _trafficCars.Count; i++)
        //        {
        //            if (_trafficCars[i] == null) continue;

        //            // Agar kisi bhi player ke saamne ya peeche traffic nahi hai
        //            if (playerPos.z > (_trafficCars[i].transform.position.z + 100) ||
        //                playerPos.z < (_trafficCars[i].transform.position.z - 300))
        //            {
        //                ReAlignTraffic(_trafficCars[i], playerPos);
        //                break; // Is car ko realign ho gaya
        //            }
        //        }
        //    }
        //}
        //else
        //{
        // ✅ OFFLINE MODE: Camera ke base pe
        if (!Camera.main || !Camera.main.transform)
            return;

        Vector3 cameraPosition = Camera.main.transform.position;

        for (int i = 0; i < _trafficCars.Count; i++)
        {
            if (_trafficCars[i] == null) continue;

            if (cameraPosition.z > (_trafficCars[i].transform.position.z + 90) ||
                cameraPosition.z < (_trafficCars[i].transform.position.z - 250))
            {
                ReAlignTraffic(_trafficCars[i], cameraPosition);
            }
        }
        // }
    }

    /// <summary>
    /// Realigns the traffic car based on reference position
    /// </summary>
    private void ReAlignTraffic(HR_TrafficCar realignableObject, Vector3 referencePosition)
    {
        if (realignableObject == null) return;

        // ✅ Server decides to activate car
        if (!realignableObject.gameObject.activeSelf)
        {
            //if (isMultiplayer && NetworkServer.active)
            //{
            //    // Server locally activate karo pehle
            //    realignableObject.gameObject.SetActive(true);

            //    // Phir RPC bhejega sab clients ko
            //    realignableObject.RpcSetActiveState(true);

            //    Debug.Log($"🟢 SERVER: Activated traffic {realignableObject.trafficIndex}");
            //}
            //else
            {
                realignableObject.gameObject.SetActive(true);
            }
        }

        int randomLine = Random.Range(0, lines.Length);

        realignableObject.currentLine = randomLine;
        realignableObject.transform.position = new Vector3(
            lines[randomLine].position.x,
            lines[randomLine].position.y,
            (referencePosition.z + (Random.Range(100, 300)))
        );

        switch (HR_GamePlayHandler.Instance.mode)
        {
            case (HR_GamePlayHandler.Mode.OneWay):
                realignableObject.transform.rotation = Quaternion.identity;
                break;
            case (HR_GamePlayHandler.Mode.TwoWay):
                if (realignableObject.transform.position.x <= 0f)
                    realignableObject.transform.rotation = Quaternion.identity * Quaternion.Euler(0f, 180f, 0f);
                else
                    realignableObject.transform.rotation = Quaternion.identity;
                break;
            case (HR_GamePlayHandler.Mode.TimeAttack):
                realignableObject.transform.rotation = Quaternion.identity;
                break;
            case (HR_GamePlayHandler.Mode.Bomb):
                realignableObject.transform.rotation = Quaternion.identity;
                break;
        }

        realignableObject.OnReAligned();

        // Agar traffic overlap kar rahi hai to deactivate karo
        if (CheckIfClipping(realignableObject.triggerCollider))
        {
            //if (isMultiplayer && NetworkServer.active)
            //{
            //    // Server locally deactivate karo pehle
            //    realignableObject.gameObject.SetActive(false);

            //    // Phir RPC bhejega sab clients ko
            //    realignableObject.RpcSetActiveState(false);

            //    Debug.Log($"🔴 SERVER: Deactivated traffic {realignableObject.trafficIndex} (clipping)");
            //}
            //else
            {
                realignableObject.gameObject.SetActive(false);
            }
        }
    }

    private bool CheckIfClipping(BoxCollider trafficCarBound)
    {
        if (trafficCarBound == null) return false;

        for (int i = 0; i < _trafficCars.Count; i++)
        {
            if (_trafficCars[i] == null) continue;

            if (!trafficCarBound.transform.IsChildOf(_trafficCars[i].transform) && _trafficCars[i].gameObject.activeSelf)
            {
                if (HR_BoundsExtension.ContainBounds(trafficCarBound.transform, trafficCarBound.bounds, _trafficCars[i].triggerCollider.bounds))
                    return true;
            }
        }
        return false;
    }
}
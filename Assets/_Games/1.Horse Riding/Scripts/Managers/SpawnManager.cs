using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tanks.Utilities;
using UnityEngine.Serialization;
public class SpawnManager : Singleton<SpawnManager> {

    [FormerlySerializedAs("spawnPoints")] public List<SpawnPoint> availableSpawnPoints  = new List<SpawnPoint>();
    int spawnIndex  = 0;
	protected override void Awake()
	{
		base.Awake();
	}



    public Transform GetSpawnTransformAtIndex(int playerId)
    {

        Transform temp = null;
        if (playerId < availableSpawnPoints .Count)
        {
            temp = availableSpawnPoints [playerId].transform;
        }
        if (temp == null)
        {
            temp = availableSpawnPoints [spawnIndex ].transform;
        }
        ////Debug.Log("Player ..."+playerId);
        //spawnPoints[i].goalController.playerID = playerId;
        spawnIndex ++;
        spawnIndex  %= availableSpawnPoints .Count;
        return temp;
    }


}

using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using Tanks.FX;
using System;
using Random = UnityEngine.Random;
using Tanks.Extensions;
 
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Tanks.Pickups
{
	//This struct is used to define spawner-relevant parameters for powerups in the CrateSpawner.
	[Serializable]
	public struct PowerupDefinition
	{
		//The name of the powerup. Not used for anything other than convenient reference.
		public string name;

		//The prefab for this powerup.
		public GameObject powerupPrefab;

		//The relative probability of this object spawning.
		public int dropWeighting;
	}

	//This server-only class is responsible for spawning powerup pods at set intervals on multiplayer maps.
	public class CrateSpawner : MonoBehaviour
	{

        public static CrateSpawner Instance;

        private void Awake()
        {
            Instance = this;
        }
        [FormerlySerializedAs("m_PowerupsToSpawn")][SerializeField] protected PowerupDefinition[] spawnablePowerups;

        [FormerlySerializedAs("powerSpawnPoints")] public Transform[] powerupSpawnPoints;

        [FormerlySerializedAs("positionToSpawn")] public List<Vector3> spawnPositions;

        private void Start()
		{
            //Photon Removal if (PhotonNetwork.IsMasterClient)
            {
                for (int i =0; i< powerupSpawnPoints.Length; i++ )
                {
                    //Photon Removal       GameObject dropPod = PhotonNetwork.Instantiate(spawnablePowerups[0].powerupPrefab.name, powerupSpawnPoints[i].position, Quaternion.identity, 0);

                }

            }

        }
       // void spawnNow()
        void spawnImmediately()
        {
            if (spawnPositions.Count>0)
            {
                Vector3 vector3 = spawnPositions[0];
                //Photon Removal  PhotonNetwork.Instantiate(spawnablePowerups[0].powerupPrefab.name, vector3, Quaternion.identity, 0);
                spawnPositions.Remove(vector3);
            }
           
        }

       // public void spawnPowerUp(Vector3 vector3)
        public void generatePowerUp(Vector3 vector3)
        {
            if (spawnPositions.Contains(vector3))
                return;
            spawnPositions.Add(vector3);
            Invoke("spawnImmediately",2);
        }
		

		

		

       
	}
}

using MalbersAnimations;
 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class MultiPlayerEnemyCreator : MonoBehaviour
{
    public static MultiPlayerEnemyCreator Instance;
    [FormerlySerializedAs("eneyVehicle")] public List<GameObject> enemyVehicles;
    [FormerlySerializedAs("spawnPositions")] public List<Transform> spawnTransforms;

    [FormerlySerializedAs("waypointsContainer")] public List<HorseAiWaypointsContainer> waypointContainers;
    [FormerlySerializedAs("target")] public List<Transform> targetTransforms;

    [FormerlySerializedAs("AiList")] public List<AnimalAIControl> aiControllers;

    [FormerlySerializedAs("id")] public int playerId = 2;


    private void Awake()
    {
        Instance = this;
        aiControllers = new List<AnimalAIControl>();
    }

  //  public void StartAI()
    public void InitializeAI()
    {
        for (int i = 0; i < aiControllers.Count; i++)
        {
            aiControllers[i].enabled = true;
        }
    }

  //  public void CreatAIPlayer()
    public void CreateAIPlayer()
    {
        Debug.Log("Creating AI");
        if (MultiPlayerGame.isTimeTrial)
        {
            return;
        }

        // HudMenuManager.TotalEnemiesToKill++;
        // MConstants.setAIData();
        // if (Tanks.Networking.NetworkManager.s_Instance.gameType == NetworkGameType.Singleplayer)
        // {
        int idex = 0;
        for (int i = 0; i < MultiPlayerGame.NO_AI_CAR; i++)
        {
            int rndV = i; //Random.Range(0, eneyVehicle.Count);
            int rndP = i; //Random.Range(0, spawnPositions.Count);
            int rndWP = i; //Random.Range(0, waypointsContainer.Count);
            int rndT = i; //Random.Range(0, target.Count);

            Transform tempTransform = spawnTransforms[idex];

            GameObject newVehicle = GameObject.Instantiate(enemyVehicles[rndV], tempTransform.position,
                tempTransform.rotation);
            Debug.Log("Creating newVehicle"+newVehicle);
            // GameObject newVehicle = (GameObject)GameObject.Instantiate(eneyVehicle[rndV], tempTransform.position + (Vector3.up), tempTransform.rotation);
            newVehicle.GetComponent<AnimalAIControl>().target = targetTransforms[rndT];
            
            // setting random target path while it should have to set according spawn index
            // newVehicle.GetComponent<AnimalAIControl>().target = target[rndT];
            //
            
            newVehicle.GetComponent<PlayerPositionController>().waypointsContainer = waypointContainers[rndWP];

            // newVehicle.GetComponent<RCC_AICarController>().isSemiFollowPlayer = isSemiFollowPlayer[rndAI];
            aiControllers.Add(newVehicle.GetComponent<AnimalAIControl>());
            DemoGameManagers.Instance.addAiPlayer(newVehicle.GetComponent<PlayerPositionController>());

            newVehicle.GetComponent<PlayerPositionController>().PlayerName = MultiPlayerGame.TempNamesList[i].aiName;
            newVehicle.GetComponent<PlayerPositionController>().playerCountry =
                MultiPlayerGame.TempNamesList[i].aiCountry;
            newVehicle.GetComponent<PlayerPositionController>().awatarID = MultiPlayerGame.TempNamesList[i].awatarID;
            //Debug.Log("AWAtart Id  " + newVehicle.GetComponent<PlayerPositionController>().awatarID);

            //newVehicle.GetComponent<RCCUNetworkPlayer>().SetPlayerId(id);
            //newVehicle.GetComponent<PlayerManager>().AddMissilePickup(Random.Range(4,10));
            // eneyVehicle.Remove(eneyVehicle[rndV]);
            //spawnPositions.Remove(spawnPositions[rndP]);                
            //waypointsContainer.Remove(waypointsContainer[rndWP]);
            //target.Remove(target[rndT]);

            idex++;
            playerId++;
        }

        // }
    }
}
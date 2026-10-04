using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using Mirror;
using MalbersAnimations;
using UnityStandardAssets.Utility;
using UnityEngine.Serialization;

/// <summary>
/// A simple manager script for Mirror demo scene. It has an array of networked spawnable player vehicles, public methods, restart, and quit application.
/// </summary>
public class HorsePhotonDemo : MonoBehaviour
{
    [FormerlySerializedAs("orbit")] public HorseSmoothFollow thirdPersonCameraFollow;
    [FormerlySerializedAs("selectableVehicles")] public Animal[] availableMounts;
    [FormerlySerializedAs("selectedCarIndex")] public int selectedMountIndex = 0;
    [FormerlySerializedAs("selectedBehaviorIndex")] public int selectedAIBehaviorIndex = 0;
    [FormerlySerializedAs("isHorse")] public bool isHorseisMountedOnHorse;

    Transform playerSpawnPoint;
    public void Start()
    {
        SpawnVehicle(0);
       // if (ReadyToGO.Instance != null) ReadyToGO.Instance.prepareForGameStart();
    }
    public void SpawnVehicle(int index)
    {
        selectedMountIndex = GetVehicleID();
        playerSpawnPoint = SpawnManager.s_Instance.GetSpawnTransformAtIndex(MultiPlayerGame.isSinglePlayer ? 0 : index);

        Vector3 lastKnownPos = Vector3.zero;
        Quaternion lastKnownRot = Quaternion.identity;
        Animal newVehicle;

        if (lastKnownPos == Vector3.zero)
        {
            lastKnownPos = playerSpawnPoint.position;
            lastKnownRot = playerSpawnPoint.rotation;
        }

        lastKnownRot.x = 0f;
        lastKnownRot.z = 0f;

        // Mirror: Spawn networked vehicle
        GameObject vehicleGO = Instantiate(availableMounts[selectedMountIndex].gameObject, lastKnownPos, lastKnownRot);
        NetworkIdentity networkIdentity = vehicleGO.GetComponent<NetworkIdentity>();

        if (networkIdentity != null)
        {
            // Spawn on network
            NetworkServer.Spawn(vehicleGO);
        }

        newVehicle = vehicleGO.GetComponent<Animal>();

        PlayerPositionController playerPosController = newVehicle.GetComponent<PlayerPositionController>();
       // if (playerPosController != null && networkIdentity != null && networkIdentity.isLocalPlayer)
       if (playerPosController != null)
        {
        //    playerPosController.isLocalPlayerControlled = true;
            DemoGameManagers.Instance.playerController = playerPosController;
            PlayerCameraManager.Instance.AssignCameraTarget(newVehicle.GetComponent<PlayerPositionController>());
        }

        HorseMobileButton.Instance.horseController = newVehicle;

        PlayerPowerController powerController = newVehicle.GetComponent<PlayerPowerController>();
        if (powerController != null)
        {
            HorseMobileButton.Instance.powerBoostController = powerController;
            HorseMobileButton.Instance.powerBoostController.OnBoostChanged += HorseMobileButton.Instance.IncreaseNitroCharge;
        }

        SetVehicleData(vehicleGO);
    }

    int GetVehicleID()
    {
        int id = 0;

        // Get from player data if available
        if (PlayerDataController.instance != null)
        {
            id = PlayerDataController.instance.playerStats.CurrentSelectedVehicle - 1;
        }

        // Determine vehicle ID based on server/client status
        if (NetworkServer.active)
        {
            id = 0;
        }
        else if (NetworkClient.active)
        {
            id = 1;
        }

        return id;
    }

    void SetVehicleData(GameObject newVehicle)
    {
        if (MultiPlayerGame.isSinglePlayer && MultiPlayerEnemyCreator.Instance)
        {
            MultiPlayerGame.setAIData();
            MultiPlayerEnemyCreator.Instance.CreateAIPlayer();
        }
    }

    public void ChooseVehicle(int index)
    {
        selectedMountIndex = index;
    }

    public void ChooseBehavior(int index)
    {
        selectedAIBehaviorIndex = index;
    }

    public void RestartGameScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
using MalbersAnimations;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPowerController : NetworkBehaviour
{
    [SyncVar] public bool isAI;
    [SyncVar] public float NoS = 100f;

    public Animal animalController;
    public AnimalAIControl animalAIController;
    private PlayerPositionController playerPos;

    private float NoSConsumption = 10f;
    private float NoSRegenerateTime = 5f;
    private int AINosCount = 0;
    private float lastTimeCheckForNos;

    public event Action<int> OnBoostChanged;

    private void Awake()
    {
        animalController = GetComponent<Animal>();
        animalAIController = GetComponent<AnimalAIControl>();
        playerPos = GetComponent<PlayerPositionController>();
        lastTimeCheckForNos = Time.time;
    }

    private void Start()
    {
        lastTimeCheckForNos = Time.time;
    }

    private void Update()
    {
        // Skip AI in tutorial mode
        if (Tutorials.isTutorialActive && isAI)
            return;

        // Every 10 seconds, check for AI NOS boost
        if ((Time.time >= lastTimeCheckForNos + 10 && isAI && AINosCount > 0) ||
            (Time.time >= lastTimeCheckForNos + 10 && isAI && DemoGameManagers.Instance.GetAIPlayerPriority(playerPos)))
        {
            lastTimeCheckForNos = Time.time;
            int rand = UnityEngine.Random.Range(0, 1000);
            if (rand > 400)
            {
                ApplyAINos();
                AINosCount = -1;
            }
        }
    }

    private void ApplyAINos()
    {
        animalController.Shift = true;
    }

    private void LateUpdate()
    {
        if (Tutorials.isTutorialActive && isAI)
            return;

        NOS();
    }

    private void NOS()
    {
        if (isAI && (animalController.IsJumping || !animalAIController.enabled || !animalAIController.Agent.enabled))
        {
            animalController.Shift = false;
        }

        if (animalController.Shift && NoS > 0)
        {
            NoS -= NoSConsumption * Time.fixedDeltaTime;
            NoSRegenerateTime = 0f;
        }
        else
        {
            if (isAI)
            {
                animalController.Shift = false;
                if (NoS < 100 && NoSRegenerateTime > 3)
                    NoS += (NoSConsumption / 1.5f) * Time.fixedDeltaTime;

                NoSRegenerateTime += Time.fixedDeltaTime;
            }
            else
            {
                if (NoS < 100 && NoSRegenerateTime > 3)
                    NoS += (NoSConsumption / 1.5f) * Time.fixedDeltaTime * 0.5f;

                NoSRegenerateTime += Time.fixedDeltaTime;
            }
        }
    }

    [Command]
    public void CmdAddNOSPickup(int nosCount)
    {
        // Update NOS on the server and sync to all clients
        AINosCount += nosCount;
        RpcOnBoostChanged(nosCount);
    }

    [ClientRpc]
    private void RpcOnBoostChanged(int nosCount)
    {
        OnBoostChanged?.Invoke(nosCount);
    }

    public void AddNOSPickup(int nosCount)
    {
        if (isAI)
        {
            AINosCount += nosCount;
        }
        else
        {
            if (isLocalPlayer)
            {
                CmdAddNOSPickup(nosCount);
            }
        }
    }

    public void AddPickupName(string pickupName)
    {
        // Reserved for future use (e.g., showing pickup name in UI)
    }
}
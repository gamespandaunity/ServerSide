namespace CarRace 
{
//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2022 BoneCracker Games
// http://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
using System;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller/Misc/RCC Teleporter")]
public class RCC_Teleporter : MonoBehaviour
{

 //  PlayerPositionSystem playerPositionSystem;
    Transform spawnPoint;
    bool isMultiplayer = false;
    private void Start()
    {
            //Photon Removal  if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom)
            //Photon Removal     isMultiplayer = true;
            //Photon Removal   else
        //   playerPositionSystem = FindObjectOfType<PlayerPositionSystem>();
    }
    void OnTriggerEnter(Collider col)
    {
        if (col.GetComponent<SphereCollider>() != null) //AI cars have sphere colliders, retun in that case
            return;
        if (isMultiplayer)
        {
                //Photon Removal   if (!PhotonNetwork.LocalPlayer.IsLocal)
                return;
            RCC_CarControllerV3 carController = col.gameObject.GetComponentInParent<RCC_CarControllerV3>();
            if (carController == null)
                return;
            //var wrongDir = carController.GetComponent<CheckWrongDirection_M>();
            //if (wrongDir == null)
            //    return;
            //spawnPoint = carController.GetComponent<CheckWrongDirection_M>().GetPlayersLastCheckPoint();
            //if (spawnPoint == null)
            //    return;
            RCC.Transport(carController, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            RCC_CarControllerV3 carController = col.gameObject.GetComponentInParent<RCC_CarControllerV3>();
          //  if (!carController || !playerPositionSystem) // Found RCC and Position System References
           //     return;
          //  spawnPoint = playerPositionSystem.GetPlayersLastCheckPoint(carController.GetComponent<CarController>().playerID);
            if (spawnPoint)
                RCC.Transport(carController, spawnPoint.position, spawnPoint.rotation);
        }
    }

}

}
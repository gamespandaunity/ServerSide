//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CarRace;

/// <summary>
/// Fixes the floating origin when player gets too far away from the origin. Repositions all important and necessary gameobjects to the 0 point.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Gameplay/HR Fix Floating Origin")]
public class HR_FixFloatingOrigin : MonoBehaviour {

    private List<GameObject> targetGameObjects = new List<GameObject>();        //  Necessary gameobjects.
    public float zLimit = 1000f;        //  Target Z limit.

    private void ResetBack() {

        targetGameObjects = new List<GameObject>();

        //  Getting necessary gameobjects.
        if (targetGameObjects.Count < 1) {

            targetGameObjects.Add(HR_TrafficPooling.Instance.container);
            targetGameObjects.Add(HR_RoadPooling.instance.allRoads);
            targetGameObjects.Add(RCC_SceneManager.Instance.activePlayerVehicle.gameObject);
            targetGameObjects.Add(FindObjectOfType<HR_CarCamera>().gameObject);

        }

        //  Creating parent gameobject. Adding necessary gameobjects, repositioning them, and lastly destroy the parent.
        GameObject parentGameObject = new GameObject("Parent");

        for (int i = 0; i < targetGameObjects.Count; i++)
            targetGameObjects[i].transform.SetParent(parentGameObject.transform, true);

        parentGameObject.transform.position -= Vector3.forward * zLimit;

        for (int i = 0; i < targetGameObjects.Count; i++)
            targetGameObjects[i].transform.SetParent(null);

        Destroy(parentGameObject);

    }

    private void Update() {

        //  If no player vehicle found, return.
        if (!RCC_SceneManager.Instance.activePlayerVehicle)
            return;

        //  Getting distance.
        float distance = RCC_SceneManager.Instance.activePlayerVehicle.transform.position.z;

        //  If distance exceeds the limits, reset.
       // if (distance >= zLimit)
           // ResetBack();

    }

}

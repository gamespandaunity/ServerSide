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
/// Upgrades traction strength of the car controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Handling")]
public class HR_VehicleUpgrade_Handling : MonoBehaviour {

    private RCC_CarControllerV3 _carController;
    public RCC_CarControllerV3 carController {

        get {

            if (_carController == null)
                _carController = GetComponentInParent<RCC_CarControllerV3>();

            return _carController;

        }

    }

    private int _handlingLevel = 0;
    public int handlingLevel {
        get {
            return _handlingLevel;
        }
        set {
            if (value <= 5)
                _handlingLevel = value;
        }
    }

    private float defHandling = -1;
    [HideInInspector] public float maxHandling = .4f;

    public void Initialize() {

        if(defHandling == -1)
            defHandling = carController.steerHelperAngularVelStrength;

        //  Setting upgraded handling strength if saved.
        handlingLevel = PlayerPrefs.GetInt(transform.root.name + "HandlingLevel");
        carController.steerHelperAngularVelStrength = Mathf.Lerp(defHandling, maxHandling, handlingLevel / 5f);
        carController.steerHelperLinearVelStrength = Mathf.Lerp(defHandling, maxHandling, handlingLevel / 5f);

    }

    /// <summary>
    /// Updates handling strength and save it.
    /// </summary>
    public void UpdateStats() {

        if (!carController)
            return;

        carController.steerHelperAngularVelStrength = Mathf.Lerp(defHandling, maxHandling, handlingLevel / 5f);
        carController.steerHelperLinearVelStrength = Mathf.Lerp(defHandling, maxHandling, handlingLevel / 5f);
        PlayerPrefs.SetInt(transform.root.name + "HandlingLevel", handlingLevel);

    }

    private void Update() {

        if (!carController)
            return;

        //  Make sure max handling is not smaller.
        if (maxHandling < carController.steerHelperAngularVelStrength)
            maxHandling = carController.steerHelperAngularVelStrength;

    }

}

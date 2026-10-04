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
/// Upgrades brake torque of the car controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Brake")]
public class HR_VehicleUpgrade_Brake : MonoBehaviour {

    private RCC_CarControllerV3 _carController;
    public RCC_CarControllerV3 carController {

        get {

            if (_carController == null)
                _carController = GetComponentInParent<RCC_CarControllerV3>();

            return _carController;

        }

    }

    private int _brakeLevel = 0;
    public int brakeLevel {
        get {
            return _brakeLevel;
        }
        set {
            if (value <= 5)
                _brakeLevel = value;
        }
    }

    private float defBrake = -1;
    [HideInInspector] public float maxBrake = 4000f;

    public void Initialize() {

        if(defBrake == -1)
            defBrake = carController.brakeTorque;

        //  Setting upgraded brake torque if saved.
        brakeLevel = PlayerPrefs.GetInt(transform.root.name + "BrakeLevel");
        carController.brakeTorque = Mathf.Lerp(defBrake, maxBrake, brakeLevel / 5f);

    }

    /// <summary>
    /// Updates brake torque and save it.
    /// </summary>
    public void UpdateStats() {

        if (!carController)
            return;

        carController.brakeTorque = Mathf.Lerp(defBrake, maxBrake, brakeLevel / 5f);
        PlayerPrefs.SetInt(transform.root.name + "BrakeLevel", brakeLevel);

    }

    private void Update() {

        if (!carController)
            return;

        //  Make sure max brake is not smaller.
        if (maxBrake < carController.brakeTorque)
            maxBrake = carController.brakeTorque;

    }

}

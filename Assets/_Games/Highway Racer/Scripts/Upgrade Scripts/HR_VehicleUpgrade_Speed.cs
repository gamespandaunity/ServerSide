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
/// Upgrades max speed of the car controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Speed")]
public class HR_VehicleUpgrade_Speed : MonoBehaviour {

    private RCC_CarControllerV3 _carController;
    public RCC_CarControllerV3 carController {

        get {

            if (_carController == null)
                _carController = GetComponentInParent<RCC_CarControllerV3>();

            return _carController;

        }

    }

    private int _speedLevel = 0;
    public int speedLevel {
        get {
            return _speedLevel;
        }
        set {
            if (value <= 5)
                _speedLevel = value;
        }
    }

    private float defSpeed = -1;
    [HideInInspector] public float maxSpeed = 280f;

    public void Initialize() {

        if(defSpeed == -1)
            defSpeed = carController.maxspeed;

        //  Setting upgraded speed if saved.
        speedLevel = PlayerPrefs.GetInt(transform.root.name + "SpeedLevel");
        carController.maxspeed = Mathf.Lerp(defSpeed, maxSpeed, speedLevel / 5f);

    }

    /// <summary>
    /// Updates max speed and save it.
    /// </summary>
    public void UpdateStats() {

        if (!carController)
            return;

        carController.maxspeed = Mathf.Lerp(defSpeed, maxSpeed, speedLevel / 5f);
        PlayerPrefs.SetInt(transform.root.name + "SpeedLevel", speedLevel);

    }

    private void Update() {

        if (!carController)
            return;

        //  Make sure max speed is not smaller.
        if (maxSpeed < carController.maxspeed)
            maxSpeed = carController.maxspeed;

    }

}

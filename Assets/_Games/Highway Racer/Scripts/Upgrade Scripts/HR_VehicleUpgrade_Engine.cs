//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using CarRace;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Upgrades engine of the car controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Engine")]
public class HR_VehicleUpgrade_Engine : MonoBehaviour {

    private RCC_CarControllerV3 _carController;
    public RCC_CarControllerV3 carController {

        get {

            if (_carController == null)
                _carController = GetComponentInParent<RCC_CarControllerV3>();

            return _carController;

        }

    }

    private int _engineLevel = 0;
    public int engineLevel {
        get {
            return _engineLevel;
        }
        set {
            if (value <= 5)
                _engineLevel = value;
        }
    }

    private float defEngine = -1;
    [HideInInspector] public float maxEngine = 750f;

    public void Initialize() {

        if(defEngine == -1)
            defEngine = carController.maxEngineTorque;

        //  Setting upgraded engine torque if saved.
        engineLevel = PlayerPrefs.GetInt(transform.root.name + "EngineLevel");
        carController.maxEngineTorque = Mathf.Lerp(defEngine, maxEngine, engineLevel / 5f);

    }

    /// <summary>
    /// Updates engine torque and save it.
    /// </summary>
    public void UpdateStats() {

        if (!carController)
            return;

        carController.maxEngineTorque = Mathf.Lerp(defEngine, maxEngine, engineLevel / 5f);
        PlayerPrefs.SetInt(transform.root.name + "EngineLevel", engineLevel);

    }

    private void Update() {

        if (!carController)
            return;

        //  Make sure max torque is not smaller.
        if (maxEngine < carController.maxEngineTorque)
            maxEngine = carController.maxEngineTorque;

    }

}

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
/// Upgrades NOS of the car controller.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR NOS")]
public class HR_VehicleUpgrade_NOS : MonoBehaviour {

    private RCC_CarControllerV3 _carController;
    public RCC_CarControllerV3 carController {

        get {

            if (_carController == null)
                _carController = GetComponentInParent<RCC_CarControllerV3>();

            return _carController;

        }

    }

    internal bool nosState = false;

    public void Initialize() {

        //  Setting NOS if saved.
        nosState = PlayerPrefs.HasKey(transform.root.name + "NOS");
        carController.useNOS = nosState;

    }

    /// <summary>
    /// Updates NOS and save it.
    /// </summary>
    public void UpdateStats() {

        if (!carController)
            return;

        carController.useNOS = nosState;

        if (nosState)
            PlayerPrefs.SetInt(transform.root.name + "NOS", 1);

    }

}

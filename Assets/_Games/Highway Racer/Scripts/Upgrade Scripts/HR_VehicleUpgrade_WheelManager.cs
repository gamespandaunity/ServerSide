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
/// Manager for upgradable wheels.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Wheel Manager")]
public class HR_VehicleUpgrade_WheelManager : MonoBehaviour {

    private RCC_CarControllerV3 carController;

    public void Initialize() {

        carController = GetComponentInParent<RCC_CarControllerV3>();
        StartCoroutine(CheckWheel());

    }

    private IEnumerator CheckWheel() {

        yield return new WaitForFixedUpdate();

        // If last selected wheel found, change the wheels.
        int wheelIndex = PlayerPrefs.GetInt(transform.root.name + "SelectedWheel", -1);

        if (wheelIndex != -1)
            RCC_Customization.ChangeWheels(carController, HR_Wheels.Instance.wheels[wheelIndex].wheel, true);

    }

    /// <summary>
    /// Changes the wheel with target wheel index.
    /// </summary>
    /// <param name="wheelIndex"></param>
    public void UpdateWheel(int wheelIndex) {

        PlayerPrefs.SetInt(transform.root.name + "SelectedWheel", wheelIndex);
        RCC_Customization.ChangeWheels(carController, HR_Wheels.Instance.wheels[wheelIndex].wheel, true);

    }

}

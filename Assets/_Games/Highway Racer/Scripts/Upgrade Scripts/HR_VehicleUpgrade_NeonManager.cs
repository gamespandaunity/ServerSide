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

/// <summary>
/// Manager for all upgradable neons.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Neon Manager")]
public class HR_VehicleUpgrade_NeonManager : MonoBehaviour {

    private HR_VehicleUpgrade_Neon[] neon;       //  All upgradable neons.
    private int selectedIndex = -1;     //  Last selected nepn.

    public string red = "Neon_Red";
    public string green = "Neon_Green";
    public string blue = "Neon_Blue";
    public string white = "Neon_White";
    public string orange = "Neon_Orange";
    public string yellow = "Neon_Yellow";

    public void Initialize() {

        neon = GetComponentsInChildren<HR_VehicleUpgrade_Neon>(true);
        CheckUpgrades();

    }

    /// <summary>
    /// Only selected neon will be enabled. All other will be disabled.
    /// </summary>
    public void CheckUpgrades() {

        for (int i = 0; i < neon.Length; i++)
            neon[i].gameObject.SetActive(false);

        selectedIndex = PlayerPrefs.GetInt(transform.root.name + "SelectedNeon", -1);

        if (selectedIndex != -1)
            neon[selectedIndex].gameObject.SetActive(true);

    }

    /// <summary>
    /// Unlocks target index, enables, and saves it.
    /// </summary>
    /// <param name="index"></param>
    public void Upgrade(int index) {

        selectedIndex = index;

        for (int i = 0; i < neon.Length; i++)
            neon[i].gameObject.SetActive(false);

        if (index != -1)
            neon[index].gameObject.SetActive(true);

        switch (selectedIndex) {

            case 0:
                PlayerPrefs.SetInt(transform.root.name + red, 1);
                break;

            case 1:
                PlayerPrefs.SetInt(transform.root.name + green, 1);
                break;

            case 2:
                PlayerPrefs.SetInt(transform.root.name + blue, 1);
                break;

            case 3:
                PlayerPrefs.SetInt(transform.root.name + yellow, 1);
                break;

            case 4:
                PlayerPrefs.SetInt(transform.root.name + orange, 1);
                break;

            case 5:
                PlayerPrefs.SetInt(transform.root.name + white, 1);
                break;

        }

        PlayerPrefs.SetInt(transform.root.name + "SelectedNeon", selectedIndex);

    }

    public void DisableAll() {

        neon = GetComponentsInChildren<HR_VehicleUpgrade_Neon>(true);

        for (int i = 0; i < neon.Length; i++)
            neon[i].gameObject.SetActive(false);

    }

}

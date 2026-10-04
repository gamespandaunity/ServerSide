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
/// Manager for all upgradable sirens.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Siren Manager")]
public class HR_VehicleUpgrade_SirenManager : MonoBehaviour {

    private HR_VehicleUpgrade_Siren[] sirens;        //  All sirens
    private int selectedIndex = -1;     //  Last selected siren index.

    public void Initialize() {

        sirens = GetComponentsInChildren<HR_VehicleUpgrade_Siren>(true);
        CheckUpgrades();

    }

    /// <summary>
    /// Disabling all sirens, and only enabled selected siren and saves it.
    /// </summary>
    public void CheckUpgrades() {

        if (sirens == null)
            return;

        for (int i = 0; i < sirens.Length; i++)
            sirens[i].gameObject.SetActive(false);

        selectedIndex = PlayerPrefs.GetInt(transform.root.name + "SelectedSiren", -1);

        if (selectedIndex != -1)
            sirens[selectedIndex].gameObject.SetActive(true);

    }

    /// <summary>
    /// Unlocks the target index and saves it.
    /// </summary>
    /// <param name="index"></param>
    public void Upgrade(int index) {

        selectedIndex = index;

        for (int i = 0; i < sirens.Length; i++)
            sirens[i].gameObject.SetActive(false);

        if (selectedIndex != -1)
            sirens[index].gameObject.SetActive(true);

        PlayerPrefs.SetInt(transform.root.name + "SelectedSiren", selectedIndex);

    }

    public void DisableAll() {

        //  Getting all sirens.
        sirens = GetComponentsInChildren<HR_VehicleUpgrade_Siren>(true);

        for (int i = 0; i < sirens.Length; i++)
            sirens[i].gameObject.SetActive(false);

    }

}

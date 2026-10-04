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
/// Manager for upgradable spoilers.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Spoiler Manager")]
public class HR_VehicleUpgrade_SpoilerManager : MonoBehaviour {

    public HR_VehicleUpgrade_Spoiler[] spoiler;        //  All upgradable spoilers.
    private int selectedIndex = -1;     //  Last selected spoiler index.
    public bool paintSpoilers = true;       //  Painting the spoilers?

    public void Initialize() {

        //  Getting all upgradable spoilers.
        spoiler = GetComponentsInChildren<HR_VehicleUpgrade_Spoiler>(true);
        CheckUpgrades();

    }

    /// <summary>
    /// Disabling all other spoilers, and enabling only selected spoiler.
    /// </summary>
    public void CheckUpgrades() {

        for (int i = 0; i < spoiler.Length; i++)
            spoiler[i].gameObject.SetActive(false);

        selectedIndex = PlayerPrefs.GetInt(transform.root.name + "SelectedSpoiler", -1);

        if (selectedIndex != -1)
            spoiler[selectedIndex].gameObject.SetActive(true);

    }

    /// <summary>
    /// Unlocks target spoiler index and saves it.
    /// </summary>
    /// <param name="index"></param>
    public void Upgrade(int index) {

        selectedIndex = index;

        for (int i = 0; i < spoiler.Length; i++)
            spoiler[i].gameObject.SetActive(false);

        if (index != -1)
            spoiler[index].gameObject.SetActive(true);

        PlayerPrefs.SetInt(transform.root.name + "SelectedSpoiler", selectedIndex);

    }

    /// <summary>
    /// Painting.
    /// </summary>
    /// <param name="newColor"></param>
    public void Paint(Color newColor) {

        for (int i = 0; i < spoiler.Length; i++)
            spoiler[i].UpdatePaint(newColor);

    }

    public void DisableAll() {

        //  Getting all upgradable spoilers.
        spoiler = GetComponentsInChildren<HR_VehicleUpgrade_Spoiler>(true);

        for (int i = 0; i < spoiler.Length; i++)
            spoiler[i].gameObject.SetActive(false);

    }

}

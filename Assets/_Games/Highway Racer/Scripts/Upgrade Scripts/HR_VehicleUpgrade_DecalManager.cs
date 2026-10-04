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
/// Manager for all upgradable decals.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Decal Manager")]
public class HR_VehicleUpgrade_DecalManager : MonoBehaviour {

    public HR_VehicleUpgrade_Decal[] decal;     //  All upgradable decals.

    public Material[] materials;
    public Material nullMaterial;
    public int selectedIndex = 0;

    public void Initialize() {

        decal = GetComponentsInChildren<HR_VehicleUpgrade_Decal>(true);

        for (int i = 0; i < decal.Length; i++)
            decal[i].Initialize();

    }

    /// <summary>
    /// Sets target material to the decal.
    /// </summary>
    /// <param name="index"></param>
    public void SetDecalMaterial(int index) {

        decal = GetComponentsInChildren<HR_VehicleUpgrade_Decal>(true);
        decal[selectedIndex].lastSelected = index;

        if (index == -1)
            decal[selectedIndex].ClearDecal();
        else
            decal[selectedIndex].UpdateDecal(materials[index]);

    }

    public void DisableAll() {

        decal = GetComponentsInChildren<HR_VehicleUpgrade_Decal>(true);

        for (int i = 0; i < decal.Length; i++)
            decal[i].ClearDecalWithoutSave();

    }

}

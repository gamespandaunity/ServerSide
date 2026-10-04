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
/// Upgradable paint.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Material")]
public class HR_VehicleUpgrade_Material : MonoBehaviour {

    public Material[] materials;       //  Materials
    public MeshRenderer bodyRenderer;

    private void OnEnable() {

        //  Getting last saved color for this vehicle.
        int mat = PlayerPrefs.GetInt(transform.root.name + "BodyMaterial", -1);

        //  Paint.
        if (mat != -1)
            bodyRenderer.material = materials[mat];

    }

    /// <summary>
    /// Paint the material with target color and save it.
    /// </summary>
    /// <param name="newColor"></param>
    public void UpdatePaint(int newMaterialIndex) {

        if (bodyRenderer)
            bodyRenderer.material = materials[newMaterialIndex];

        PlayerPrefs.SetInt(transform.root.name + "BodyMaterial", newMaterialIndex);

    }

}

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
/// Upgradable paint.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Paint")]
public class HR_VehicleUpgrade_Paint : MonoBehaviour {

    public MeshRenderer bodyRenderer;       //  Target renderer for painting.
    public int index = 0;       //  Index of the target material.
    private Color color = Color.gray;        //  Default color.

    private void OnEnable() {

        //  Getting last saved color for this vehicle.
        color = RCC_PlayerPrefsX.GetColor(transform.root.name + "BodyColor", HR_HighwayRacerProperties.Instance._defaultBodyColor);

        //  Paint.
        if (bodyRenderer)
            bodyRenderer.materials[index].color = color * .7f;

    }

    /// <summary>
    /// Paint the material with target color and save it.
    /// </summary>
    /// <param name="newColor"></param>
    public void UpdatePaint(Color newColor) {

        if (bodyRenderer)
            bodyRenderer.materials[index].color = newColor * .75f;

        RCC_PlayerPrefsX.SetColor(transform.root.name + "BodyColor", newColor);
        PlayerPrefs.SetInt(transform.root.name + "BodyColor" + newColor.ToString(), 1);

    }

}

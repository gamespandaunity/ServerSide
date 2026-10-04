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
/// Upgradable spoiler.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Spoiler")]
public class HR_VehicleUpgrade_Spoiler : MonoBehaviour {

    public MeshRenderer bodyRenderer;       //  Renderer of the spoiler.
    public int index = -1;       //  Material index of the renderer.
    public Color color = Color.gray;     //  Default color.

    private void OnEnable() {

        //  If index is set to -1, no need to paint it.
        if (index == -1)
            return;

        //  Getting saved color of the spoiler.
        color = RCC_PlayerPrefsX.GetColor(transform.root.name + "BodyColor", Color.gray);

        //  Painting target material.
        if (bodyRenderer)
            bodyRenderer.materials[index].color = color;

    }

    /// <summary>
    /// Painting.
    /// </summary>
    /// <param name="newColor"></param>
    public void UpdatePaint(Color newColor) {

        if (index == -1)
            return;

        if (bodyRenderer)
            bodyRenderer.materials[index].color = newColor;

        RCC_PlayerPrefsX.SetColor(transform.root.name + "BodyColor", newColor);

    }

    private void Reset() {

        bodyRenderer = GetComponent<MeshRenderer>();

    }

}

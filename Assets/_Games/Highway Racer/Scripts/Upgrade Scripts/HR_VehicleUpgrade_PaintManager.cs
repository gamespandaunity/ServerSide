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
/// Manager for painters.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Paint Manager")]
public class HR_VehicleUpgrade_PaintManager : MonoBehaviour {

    private HR_VehicleUpgrade_Paint[] paints;       //  All painters.

    public void Initialize() {

        //  Getting all painters.
        paints = GetComponentsInChildren<HR_VehicleUpgrade_Paint>(true);

        for (int i = 0; i < paints.Length; i++)
            paints[i].UpdatePaint(RCC_PlayerPrefsX.GetColor(transform.root.name + "BodyColor", HR_HighwayRacerProperties.Instance._defaultBodyColor));

    }

    /// <summary>
    /// Runs all painters with the target color.
    /// </summary>
    /// <param name="newColor"></param>
    public void Paint(Color newColor) {

        //  Getting all painters.
        paints = GetComponentsInChildren<HR_VehicleUpgrade_Paint>(true);

        for (int i = 0; i < paints.Length; i++)
            paints[i].UpdatePaint(newColor);

    }

    private void Reset() {

        if (transform.Find("Paint_1")) {

            paints = new HR_VehicleUpgrade_Paint[1];
            paints[0] = transform.Find("Paint_1").gameObject.GetComponent<HR_VehicleUpgrade_Paint>();
            return;

        }

        paints = new HR_VehicleUpgrade_Paint[1];
        GameObject newPaint = new GameObject("Paint_1");
        newPaint.transform.SetParent(transform);
        newPaint.transform.localPosition = Vector3.zero;
        newPaint.transform.localRotation = Quaternion.identity;
        paints[0] = newPaint.AddComponent<HR_VehicleUpgrade_Paint>();

    }

}

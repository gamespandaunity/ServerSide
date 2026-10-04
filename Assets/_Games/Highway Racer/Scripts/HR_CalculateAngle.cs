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
/// Calculates forward steering angle.
/// </summary>
public class HR_CalculateAngle {

    public static float CalculateAngle(Quaternion transformA, Quaternion transformB) {

        var forwardA = transformA * Vector3.forward;
        var forwardB = transformB * Vector3.forward;

        float angleA = Mathf.Atan2(forwardA.x, forwardA.z) * Mathf.Rad2Deg;
        float angleB = Mathf.Atan2(forwardB.x, forwardB.z) * Mathf.Rad2Deg;

        var angleDiff = Mathf.DeltaAngle(angleA, angleB);

        return angleDiff;

    }

}

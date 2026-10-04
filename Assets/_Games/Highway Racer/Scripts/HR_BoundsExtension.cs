//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using System.Collections;

/// <summary>
/// Checks max bounds of the transform.
/// </summary>
public static class HR_BoundsExtension {

    public static bool ContainBounds(Transform t, Bounds bounds, Bounds target) {

        if (bounds.Contains(target.ClosestPoint(t.position)))
            return true;

        return false;

    }

}

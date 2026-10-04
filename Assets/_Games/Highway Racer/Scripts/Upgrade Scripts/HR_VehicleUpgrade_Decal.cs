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
/// Switches decal material.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Upgrade/HR Decal")]
public class HR_VehicleUpgrade_Decal : MonoBehaviour {

    private HR_VehicleUpgrade_DecalManager _decalManager;
    public HR_VehicleUpgrade_DecalManager decalManager {

        get {

            if (_decalManager == null)
                _decalManager = GetComponentInParent<HR_VehicleUpgrade_DecalManager>();

            return _decalManager;

        }

    }

    private MeshRenderer _decalRenderer;
    public MeshRenderer decalRenderer {

        get {

            if (_decalRenderer == null)
                _decalRenderer = GetComponentInChildren<MeshRenderer>();

            return _decalRenderer;

        }

    }

    internal int lastSelected = -1;     //  Last selected decal material index.

    public void Initialize() {

        lastSelected = PlayerPrefs.GetInt(transform.root.name + transform.name, -1);        //  Getting last selected decal material.

        // If last selected found, set it.
        if (lastSelected == -1)
            decalRenderer.material = decalManager.nullMaterial;
        else
            UpdateDecal(GetComponentInParent<HR_VehicleUpgrade_DecalManager>().materials[lastSelected]);

    } 

    /// <summary>
    /// Updates decal with target material.
    /// </summary>
    /// <param name="mat"></param>
    public void UpdateDecal(Material mat) {

        decalRenderer.material = mat;
        PlayerPrefs.SetInt(transform.root.name + transform.name, lastSelected);

    }

    /// <summary>
    /// Clears decal material.
    /// </summary>
    public void ClearDecal() {

        PlayerPrefs.SetInt(transform.root.name + transform.name, -1);
        decalRenderer.material = decalManager.nullMaterial;

    }

    /// <summary>
    /// Clears decal material without saving it.
    /// </summary>
    public void ClearDecalWithoutSave() {

        decalRenderer.material = decalManager.nullMaterial;

    }

}

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
/// Fake transparent box for emmisive lighting.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Traffic/HR Light Box")]
public class HR_LightBox : MonoBehaviour {

    private Light _light;
    private MeshRenderer boxRenderer;

    private void Awake() {

        _light = GetComponent<Light>();
        boxRenderer = GetComponentInChildren<MeshRenderer>();

    }


    private void Update() {

        if (!_light || !boxRenderer)
            return;

        boxRenderer.material.SetColor("_BaseColor", _light.color * _light.intensity);
        boxRenderer.material.SetColor("_EmissionColor", _light.color * _light.intensity);

    }

}

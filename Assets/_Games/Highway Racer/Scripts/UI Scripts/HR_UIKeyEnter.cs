//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// UI Button click invoker with old input.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Key Enter")]
public class HR_UIKeyEnter : MonoBehaviour {

    public string inputName = "Submit";
    private Button button;

    private void Start() {

        button = GetComponent<Button>();

    }

    private void Update() {

        if (Input.GetButtonDown(inputName))
            button.onClick.Invoke();

    }

}
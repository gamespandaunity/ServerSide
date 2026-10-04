//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

/// <summary>
/// Mobile UI Drag used for orbiting Showroom Camera.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Mobile Drag")]
public class HR_UIMobileDrag : MonoBehaviour, IDragHandler, IEndDragHandler {

    private HR_ShowroomCamera showroomCamera;

    private void Awake() {

        showroomCamera = FindObjectOfType<HR_ShowroomCamera>();

    }

    public void OnDrag(PointerEventData data) {

        showroomCamera.OnDrag(data);

    }

    public void OnEndDrag(PointerEventData data) {



    }

}

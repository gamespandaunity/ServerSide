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
using UnityEngine.EventSystems;
using CarRace;

[AddComponentMenu("BoneCracker Games/Highway Racer/UI/HR UI Horn")]
public class HR_UIHorn : MonoBehaviour/*, IPointerDownHandler, IPointerUpHandler*/
{

    public bool isPressing = false;

    private void OnEnable()
    {

        if (!RCC_Settings.Instance.mobileControllerEnabled)
        {

            gameObject.SetActive(false);
            return;

        }

    }

    private void Update()
    {

        //if (isPressing)
        //    RCC_SceneManager.Instance.activePlayerVehicle.highBeamHeadLightsOn = true;
        //else
        //    RCC_SceneManager.Instance.activePlayerVehicle.highBeamHeadLightsOn = false;

    }
    public void MyToggleHeadlights()
    {
        if (HighwayCarNetwork.ins != null)
        {
            HighwayCarNetwork.ins.ToggleHeadlights();
            Debug.Log("MyToggleHeadlights");
        }
    }
    //public void OnPointerClick(PointerEventData eventData)
    //{
    //    if (HighwayCarNetwork.ins != null)
    //    {
    //        HighwayCarNetwork.ins.ToggleHeadlights();
    //    }
    //}

    //public void OnPointerDown(PointerEventData eventData) {

    //    isPressing = true;

    //}

    //public void OnPointerUp(PointerEventData eventData) {

    //    isPressing = false;

    //}

}

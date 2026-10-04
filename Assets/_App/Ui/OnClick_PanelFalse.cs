using BallPool;
using System;
using System.Drawing;
using UnityEngine;
using UnityEngine.EventSystems;

public class OnClick_PanelFalse : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        gameObject.SetActive(false);
        ShotController.shotControllerInstance.enabled = true;
    }
    
    //private void OnEnable()
    //{
    //    InputOutput.OnMouseState += MouseStateHandler;
    //}
    //private void OnDisable()
    //{
    //    InputOutput.OnMouseState -= MouseStateHandler;
    //}
    //private void MouseStateHandler(MouseState mouseState)
    //{
    //    if ( mouseState == MouseState.Up)
    //    {

    //        gameObject.SetActive(false);

    //    }
    //    if (mouseState == MouseState.PressAndMove)
    //    {

    //    }
    //}
}

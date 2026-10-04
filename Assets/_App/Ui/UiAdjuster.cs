using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class UiAdjuster : MonoBehaviour
{
    public RectTransform inputFieldRectTransform; // Assign this in the inspector
    public RectTransform Panel;
    public Vector2 initPos;
 

    private void Start()
    {

        
        if (Panel != null && Panel.GetComponent<RectTransform>() != null)
        {
            initPos = Panel.anchoredPosition;
        }
        else
        {
            ConstantsData_M.Log("Panel or RectTransform is not assigned or found");
        }
    }
   public void SSelection()
     {  
        float screenHeight = Screen.height; // Get screen height
        float inputFieldY = inputFieldRectTransform.position.y; // Get input field's current y position
        
        float relativePositionToTop = screenHeight - inputFieldY; // Calculate position relative to the top of the screen
        //Debug.Log($"screen height {screenHeight} and  input y position {inputFieldY}  difference is => {relativePositionToTop}  of object =>{gameObject.name}");
        // Assuming the keyboard height is still given as 0, you might need to set a fixed value for now

        if (TouchScreenKeyboard.visible)
        {
          
      
             Panel.anchoredPosition = new Vector2(initPos.x, initPos.y + relativePositionToTop); // Adjust the y-offset as necessary
        }
        else
        {
            Panel.anchoredPosition = initPos;
        }
    }
  

  
}



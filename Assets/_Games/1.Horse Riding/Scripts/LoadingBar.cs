using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace HorseRiding
{

    public class LoadingBar : MonoBehaviour
{
    public Image fillBar;
    float value = 0;
    public void OnValueUpdate(float value)
    {
        //value = value + Time.deltaTime*0.5f;
        fillBar.fillAmount = value;
    }

}
}

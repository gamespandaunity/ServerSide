using UnityEngine;
using UnityEngine.UI;

public class ShotSlider : MonoBehaviour
{
    [SerializeField] private Slider shotSlider;
    [SerializeField] private Image cueSliderImage;

	void Update () 
    {
        cueSliderImage.fillAmount = 1.0f - shotSlider.value;
	}
}

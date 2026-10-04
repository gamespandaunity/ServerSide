using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomWeather : MonoBehaviour
{
    public Material[] skyMats;
   
    private void OnEnable()
    {
        int rand = Random.Range (0,skyMats.Length);
        RenderSettings.skybox = skyMats [rand];
    }
}

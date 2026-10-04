using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Splash : MonoBehaviour
{
    void Start()
    {
        
        Invoke("LoadScene", 3);
        
    }

    public void LoadAd()
    {

        LoadScene();
    }

    public void LoadScene(){
        Application.LoadLevelAsync("UIScene");
    }
   
}

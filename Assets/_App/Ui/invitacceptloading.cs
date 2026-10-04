using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class invitacceptloading : MonoBehaviour
{
    public GameObject parent;
    public Image   bg;
    private void OnEnable()
    {
        SpritesManager.Instance.spritesScriptable.BgHandlerAccordingTo_Id(bg);
        
    }
    void Update()
    {
        if (SceneManager.GetActiveScene().name=="AightBallPool" || SceneManager.GetActiveScene().name == "Main_Menu")
        {
            
            Destroy(parent);
            
        }
    }
}

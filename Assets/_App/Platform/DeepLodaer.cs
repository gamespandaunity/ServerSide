using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeepLodaer : MonoBehaviour
{


    public Image Logo_, bg;
    public Slider loadingSlider;

    [SerializeField] private float sliderMoverValue;
    private void OnEnable()
    {
      

    }
    private void Start()
    {
        //print("invited person profile deep loader" + staticVariables.invitedpersonProfile.gameId);
       ApiAndRoomManager.currentGameId = int.Parse(staticVariables.invitedpersonProfile.gameId) ;
        //print("apimanager" + APIManager.gameid);


        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(Logo_);
        SpritesManager.Instance.spritesScriptable.BgHandlerAccordingTo_Id(bg);

       


    }
    void Update()
    {



        if (loadingSlider.value < loadingSlider.maxValue)
        {
            loadingSlider.value += sliderMoverValue * Time.deltaTime;
        }
        if (loadingSlider.value == loadingSlider.maxValue)
        {
            gameObject.SetActive(false);
        }
        
    }

}

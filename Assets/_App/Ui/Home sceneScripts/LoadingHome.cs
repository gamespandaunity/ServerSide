using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NetworkManagement;
public class LoadingHome : MonoBehaviour
{
    public SpritesHOlder genericSpriteHolder;
    public Image Logo_, bg;
    public Slider loadingSlider;
    public Sprite genericBG;
    [SerializeField] private float sliderMoverValue;
    [SerializeField] bool IsmainMenu = false;

    private void OnEnable()
    {
        if (!IsmainMenu)
        {
            //print("MAINMENU == FALSE");
            if (staticVariables.isfromreferllinks)
            {
                //print("IS FROM REFER LINKS");
                if (SceneManager.GetActiveScene().buildIndex == 7)
                {
                    loadingSlider.value = 0.4f;
                    //print("BUILD INDEX == 7");
                }
                else
                {
                    //print("ELSE --==--> BUILD INDEX  =-=-7 " );
                    PlayerProfile profle = staticVariables.invitedpersonProfile;
                    loadingSlider.value = 0;
                   
                        genericSpriteHolder.LogoChangerWith_GameId(Logo_);
                   
                  
            genericSpriteHolder.BgHandlerAccordingTo_Id(bg);
                }
                genericSpriteHolder.BgHandlerAccordingTo_Id(bg);
            }
            else
            {
                //print("NNOT FROM R E F ER     L I N K S");
                loadingSlider.value = 0;
                genericSpriteHolder.LogoChangerWith_GameId(Logo_);
                genericSpriteHolder.BgHandlerAccordingTo_Id(bg);
            }


        }
        else
        {
            //print("MAINMENU == TRUE");
            if (SceneManager.GetActiveScene().name == "MainmenuScene")
            {
                bg.sprite = genericBG;
                //print("MAINMENU == TRUE   SCENE NEMA MAIN MENU SCENE ");
            }
            else
            {
            genericSpriteHolder.LogoChangerWith_GameId(Logo_);
            genericSpriteHolder.BgHandlerAccordingTo_Id(bg);
                //print("MAINMENU == TRUE   SCENE NEMA MAIN MENU SCENE   ===>>> ELSE PART");
            }
        }

    }

    void Update()
    {



        if (loadingSlider.value < loadingSlider.maxValue)
        {
            loadingSlider.value += sliderMoverValue * Time.deltaTime;
        }
        if ( loadingSlider.value == loadingSlider.maxValue)
        {
            gameObject.SetActive(false);
        }

    }
}

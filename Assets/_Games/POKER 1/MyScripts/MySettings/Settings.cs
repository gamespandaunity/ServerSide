namespace POKER 
{
//
using POKER;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

public class Settings : MonoBehaviour
{

    public Transform OnOffBtn;
    public Transform ViberateBtn;
    public Transform SuperBaharBtn;
    public Transform Notification;
    public Transform autoSeatedBtn;
    // Start is called before the first frame update
    void Start()
    {

        Notification.gameObject.SetActive(true);
        SuperBaharBtn.parent.gameObject.SetActive(false);
        autoSeatedBtn.parent.gameObject.SetActive(/*LocalSettings.IsMenuScene() ? true :*/ false);
        Set_Btn_Turn(OnOffBtn, LocalSettings.GetSoundEffect());
        Set_Btn_Turn(autoSeatedBtn, LocalSettings.Get_Auto_Seated_Status());
       

        SetViberationBtn(LocalSettings.mobilVibration);

    }


    

    public void Set_Auto_Seated_Value()
    {
        LocalSettings.Set_Auto_Seated_Status(!LocalSettings.Get_Auto_Seated_Status());
        Set_Btn_Turn(autoSeatedBtn, LocalSettings.Get_Auto_Seated_Status());
    }


    public void SetSoundStatus()
    {
        LocalSettings.SetSoundEffect(!LocalSettings.GetSoundEffect());
        Set_Btn_Turn(OnOffBtn, LocalSettings.GetSoundEffect());

        // BG music handling
        Menu_Manager menu_Manager = FindObjectOfType<Menu_Manager>();
        if (menu_Manager != null)
            menu_Manager.HandleBGMusic();
    }

    public void SetViberationBtnStatus()
    {
        LocalSettings.mobilVibration = !LocalSettings.mobilVibration;
        SetViberationBtn(LocalSettings.mobilVibration);

    }

    void Set_Btn_Turn(Transform ButtonName, bool isTrue)
    {
        ButtonName.GetChild(0).gameObject.SetActive(isTrue);
        ButtonName.GetChild(1).gameObject.SetActive(!isTrue);
    }


    void SetViberationBtn(bool isTrue)
    {
        ViberateBtn.GetChild(0).gameObject.SetActive(isTrue);
        ViberateBtn.GetChild(1).gameObject.SetActive(!isTrue);
        LocalSettings.Vibrate();
    }


    public void OpenLink(string url)
    {
        Application.OpenURL(url);
    }


   
}

}
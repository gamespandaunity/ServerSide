using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using CarRace;
using UnityEngine.Serialization;

public class SettingMenuManger : MonoBehaviour
{
    [FormerlySerializedAs("Tilt")] public Image tiltControlImage;
    [FormerlySerializedAs("Steering")] public Image steeringControlImage;
    [FormerlySerializedAs("buttons")] public Image buttonControlImage;

    [FormerlySerializedAs("TiltSprite")] public Sprite[] tiltSprites;
    [FormerlySerializedAs("SteeringSprite")] public Sprite[] steeringSprites;
    [FormerlySerializedAs("buttonsSprite")] public Sprite[] buttonSprites;

    [FormerlySerializedAs("toggleSound")] public Toggle toggleSound;
    [FormerlySerializedAs("toggleQuality")] public Toggle toggleQuality;

    [FormerlySerializedAs("qualityDialog")] public GameObject qualityDialog;

    [FormerlySerializedAs("highQualityWarning")] public GameObject highQualityWarning;


    void OnEnable()
    {
        if (PlayerDataController.instance == null)
        {
            return;
        }
        SwitchInputMethod(PlayerDataController.instance.playerStats.SelectedControl);
        //ToggleSound (PlayerDataController.Instance.playerData.isSoundOn);
        toggleSound.isOn = PlayerDataController.instance.playerStats.isSoundOn;
        toggleQuality.isOn = PlayerDataController.instance.playerStats.isHighQuality;
        qualityDialog.SetActive(false);

    }


    // public void ToggleQuality()
    public void ToggleGraphicsQuality()
    {
        qualityDialog.SetActive(true);
        StartCoroutine(CountdownRoutine());
    }

    //  private IEnumerator DecrementCount()
    private IEnumerator CountdownRoutine()
    {
        float pauseEndTime = Time.realtimeSinceStartup + 0.1f;
        while (Time.realtimeSinceStartup < pauseEndTime)
        {
            yield return 0;
        }
        ApplyGraphicsQuality();
    }

    //  void changeQuality()
    void ApplyGraphicsQuality()
    {

        //
        highQualityWarning.SetActive(!toggleQuality.isOn);
        //

        if (toggleQuality.isOn)
        {
            QualitySettings.SetQualityLevel(5, true);
            //toggleSound.isOn = isOn;
        }
        else
        {
            QualitySettings.SetQualityLevel(0, true);

            //toggleSound.isOn = isOn;
        }
        qualityDialog.SetActive(false);
        PlayerDataController.instance.playerStats.isHighQuality = toggleQuality.isOn;

        PlayerDataController.instance.SaveData();
    }
    // public void ToggleSound(){
    public void ToggleGameAudio()
    {

        if (toggleSound.isOn)
        {

            AudioListener.volume = 1;
            //toggleSound.isOn = isOn;
        }
        else
        {

            AudioListener.volume = 0;
            //toggleSound.isOn = isOn;
        }
        PlayerDataController.instance.playerStats.isSoundOn = toggleSound.isOn;

        PlayerDataController.instance.SaveData();
    }

    // public void ChangeController(int index)
    public void SwitchInputMethod(int index)
    {

        switch (index)
        {

            case 0://Buttons
                   //RCC_Settings.Instance.useAccelerometerForSteering = false;
                RCC_Settings.Instance.mobileController = RCC_Settings.MobileController.TouchScreen;
                //RCC_Settings.Instance.useSteeringWheelForSteering = false;
                PlayerDataController.instance.playerStats.SelectedControl = index;
                tiltControlImage.sprite = tiltSprites[0];
                steeringControlImage.sprite = steeringSprites[0];
                buttonControlImage.sprite = buttonSprites[1];

                break;
            case 1://Tilt
                   //RCC_Settings.Instance.useAccelerometerForSteering = true;
                   //RCC_Settings.Instance.useSteeringWheelForSteering = false;
                PlayerDataController.instance.playerStats.SelectedControl = index;
                RCC_Settings.Instance.mobileController = RCC_Settings.MobileController.Gyro;

                tiltControlImage.sprite = tiltSprites[1];
                steeringControlImage.sprite = steeringSprites[0];
                buttonControlImage.sprite = buttonSprites[0];

                break;
            case 2://Steering
                tiltControlImage.sprite = tiltSprites[0];
                steeringControlImage.sprite = steeringSprites[1];
                buttonControlImage.sprite = buttonSprites[0];
                //RCC_Settings.Instance.useAccelerometerForSteering = false;
                //RCC_Settings.Instance.useSteeringWheelForSteering = true;
                RCC_Settings.Instance.mobileController = RCC_Settings.MobileController.SteeringWheel;

                PlayerDataController.instance.playerStats.SelectedControl = index;

                break;

        }
        PlayerDataController.instance.SaveData();

    }

    // public void ShowPrivacyPolicy()
    public void DisplayPrivacyPolicyPopup()
    {
        MainMenuManager.Instance.showSubMenu(SubMenuNames.PRIVACY);
    }
}

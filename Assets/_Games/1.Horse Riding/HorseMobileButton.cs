//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2017 BoneCracker Games
// http://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MalbersAnimations;
using UnityEngine.Serialization;

/// <summary>
/// Receiving inputs from UI buttons, and feeds active vehicles on your scene.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller/UI/Mobile/Horse Mobile Button")]
public class HorseMobileButton : MonoBehaviour
{
    public static HorseMobileButton Instance;

    // Getting an Instance of Main Shared RCC Settings.


    [FormerlySerializedAs("gasButton")] public HorseUIController accelerateButton;
    [FormerlySerializedAs("gradualGasButton")] public HorseUIController smoothAccelerateButton;
    [FormerlySerializedAs("brakeButton")] public HorseUIController brakeButtonUI;
    [FormerlySerializedAs("leftButton")] public HorseUIController steerLeftButton;
    [FormerlySerializedAs("rightButton")] public HorseUIController steerRightButton;
    [FormerlySerializedAs("handbrakeButton")] public HorseUIController handbrakeButtonUI;
    [FormerlySerializedAs("NOSButton")] public HorseUIController nitroButton;
    [FormerlySerializedAs("NOSButtonSteeringWheel")] public HorseUIController steeringNitroButton;
    [FormerlySerializedAs("gearButton")] public GameObject gearToggleButton;

   [SerializeField] private float accelerationInput = 0f;
    [SerializeField]private float brakeForceInput = 0f;
    [SerializeField]private float steeringLeftInput = 0f;
  [SerializeField]  private float steeringRightInput = 0f;
   [SerializeField] private float steeringInput = 0f;
    [SerializeField]private float handbrakeForce = 0f;
   [SerializeField] private float nitroInputStrength = 1f;
   [SerializeField] private float gyroscopeInput = 0f;
   [SerializeField] private float joystickDirectionInput = 0f;
   [SerializeField] private bool isNitroAvailable = false;
   [SerializeField] private bool nitroActivated = false;
    [SerializeField]private Vector3 initialBrakeBtnPosition;
   [SerializeField] float maxSpeedLimit = 180;


    [FormerlySerializedAs("animalController")] public Animal horseController;
    [FormerlySerializedAs("playerPowerController")] public PlayerPowerController powerBoostController;

    // Detects when the horse hits a hurdle and is falling. While it is falling we
    // ignore run / boost / steer input so the horse drops straight to the ground and
    // only responds to controls again once it has landed. Resolved lazily from the horse.
    private HorseHurdleCollisionDetection hurdleDetector;

    [FormerlySerializedAs("NosData")] public Image[] nitroUIBars;

    [FormerlySerializedAs("jumpBtnButtonsControls")] public GameObject jumpButtonUIGroup;
    [FormerlySerializedAs("jumpBtnTiltControls")] public GameObject jumpTiltUIGroup;

    [FormerlySerializedAs("Tutorials")] public Tutorials tutorialManager;

    public static System.Action OnGameStarted;
    public float GetAccelerationInput() => accelerationInput;
public float GetBrakeInput() => brakeForceInput;
public float GetSteerLeftInput() => steeringLeftInput;
public float GetSteerRightInput() => steeringRightInput;
public float GetSteeringInput() => steeringInput;
public float GetGyroscopeInput() => gyroscopeInput;
public float GetJoystickDirectionInput() => joystickDirectionInput;

    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {

        //if (gasButton)
        //gasButton.gameObject.SetActive(false);
        if (smoothAccelerateButton)
            smoothAccelerateButton.gameObject.SetActive(false);
        if (steerLeftButton)
            steerLeftButton.gameObject.SetActive(false);
        if (steerRightButton)
            steerRightButton.gameObject.SetActive(false);
        if (brakeButtonUI)
            brakeButtonUI.gameObject.SetActive(false);
        //if(steeringWheel)
        //	steeringWheel.gameObject.SetActive(false);
        if (handbrakeButtonUI)
            handbrakeButtonUI.gameObject.SetActive(false);
        //if (NOSButton)
        //    NOSButton.gameObject.SetActive(false);
        //if (NOSButtonSteeringWheel)
        //    NOSButtonSteeringWheel.gameObject.SetActive(false);
        if (gearToggleButton)
            gearToggleButton.gameObject.SetActive(false);
        //if (joystick)
        //    joystick.gameObject.SetActive(false);

        //enabled = false;
        return;



        initialBrakeBtnPosition = brakeButtonUI.transform.position;

        //vehicleMaxSpeed = PlayerDataController.Instance.playerData.CarsList[PlayerDataController.Instance.playerData.CurrentSelectedVehicle - 1].Speed + 50;
        maxSpeedLimit = 5000;

    }

    // public void AddNosData(int nosToAdd)
    public void IncreaseNitroCharge(int nosToAdd)
    {
        PlayerDataController.instance.playerStats.NitrosCount += nosToAdd;
        HudMenuManager.instance.updateUIContent();
    }
    //  public void disableControlles()
    public void DisableControls()
    {

        if (accelerateButton)
            accelerateButton.gameObject.SetActive(false);
        if (steerLeftButton)
            steerLeftButton.gameObject.SetActive(false);
        if (steerRightButton)
            steerRightButton.gameObject.SetActive(false);
        if (brakeButtonUI)
            brakeButtonUI.gameObject.SetActive(false);
        //if (steeringWheel)
        //    steeringWheel.gameObject.SetActive(false);
        if (handbrakeButtonUI)
            handbrakeButtonUI.gameObject.SetActive(false);
        if (nitroButton)
            nitroButton.gameObject.SetActive(false);
        if (gearToggleButton)
            gearToggleButton.gameObject.SetActive(false);

        if (jumpButtonUIGroup)
            jumpButtonUIGroup.SetActive(false);
        if (jumpTiltUIGroup)
            jumpTiltUIGroup.SetActive(false);

        //
        if (MultiPlayerGame.isChampion)
        {
            if (MConstants.CurrentCHAMPION_MODE == MConstants.CHAMPION_MODES.DUABI_CHAMPION && MultiPlayerGame.isChampion && MConstants.CurrentLevelNumber < 5)
            {
                tutorialManager.HideAllTutorialElements();
            }
        }
        //
    }

    // public void enableControlles()
    public void EnableControls()
    {

        // fire game start event
        OnGameStarted?.Invoke();
        //

        if (MConstants.CurrentCHAMPION_MODE == MConstants.CHAMPION_MODES.DUABI_CHAMPION && MultiPlayerGame.isChampion && MConstants.CurrentLevelNumber < 6)
        {
            //
            tutorialManager.DisplayTutorialUI();
            //
        }
        else
        {
            //if(RCC_Settings.Instance.controllerType == RCC_Settings.ControllerType.Mobile){

            if (accelerateButton)
                accelerateButton.gameObject.SetActive(true);
            if (steerLeftButton)
                steerLeftButton.gameObject.SetActive(true);
            if (steerRightButton)
                steerRightButton.gameObject.SetActive(true);
            if (brakeButtonUI)
                brakeButtonUI.gameObject.SetActive(false);
            //if (steeringWheel && RCC_Settings.Instance.mobileController == RCC_Settings.MobileController.SteeringWheel)
            //{
            // steeringWheel.gameObject.SetActive(true);
            //}

            //if (handbrakeButton)
            //    handbrakeButton.gameObject.SetActive(true);
            if (nitroButton)
                nitroButton.gameObject.SetActive(true);
            //if(gearButton)
            //gearButton.gameObject.SetActive(true);
            //}

            if (jumpButtonUIGroup)
                jumpButtonUIGroup.SetActive(true);
            //if (RCC_Settings.Instance.mobileController == RCC_Settings.MobileController.Gyro && jumpBtnTiltControls)
            //    jumpBtnTiltControls.SetActive(true);
        }

    }
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TriggerJump();
        }
        //
        if (!Tutorials.isTutorialActive)
        {
            //switch (RCCSettings.mobileController)
            //{

            //    case RCC_Settings.MobileController.TouchScreen:

            gyroscopeInput = 0f;

            //if(steeringWheel && steeringWheel.gameObject.activeInHierarchy)
            //	steeringWheel.gameObject.SetActive(false);

            //if(NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
            //	NOSButton.gameObject.SetActive(canUseNos);

            //if (joystick && joystick.gameObject.activeInHierarchy)
            //    joystick.gameObject.SetActive(false);

            if (!steerLeftButton.gameObject.activeInHierarchy)
            {

                brakeButtonUI.transform.position = initialBrakeBtnPosition;
                steerLeftButton.gameObject.SetActive(true);

            }

            if (!steerRightButton.gameObject.activeInHierarchy)
                steerRightButton.gameObject.SetActive(true);

            if (jumpButtonUIGroup)
                jumpButtonUIGroup.SetActive(true);
            if (jumpTiltUIGroup)
                jumpTiltUIGroup.SetActive(false);
            // break;

            //    case RCC_Settings.MobileController.Gyro:

            //        gyroInput = Input.acceleration.x * RCCSettings.gyroSensitivity;
            //        brakeButton.transform.position = leftButton.transform.position;

            //        //if(steeringWheel.gameObject.activeInHierarchy)
            //        //	steeringWheel.gameObject.SetActive(false);

            //        //if(NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
            //        //	NOSButton.gameObject.SetActive(canUseNos);

            //        if (joystick && joystick.gameObject.activeInHierarchy)
            //            joystick.gameObject.SetActive(false);

            //        if (leftButton.gameObject.activeInHierarchy)
            //            leftButton.gameObject.SetActive(false);

            //        if (rightButton.gameObject.activeInHierarchy)
            //            rightButton.gameObject.SetActive(false);

            //        if (jumpBtnButtonsControls)
            //            jumpBtnButtonsControls.SetActive(false);
            //        if (jumpBtnTiltControls)
            //            jumpBtnTiltControls.SetActive(true);


            //        break;

            //    case RCC_Settings.MobileController.SteeringWheel:

            //        gyroInput = 0f;

            //        //if(!steeringWheel.gameObject.activeInHierarchy){
            //        //	steeringWheel.gameObject.SetActive(true);
            //        //	brakeButton.transform.position = orgBrakeButtonPos;
            //        //}

            //        //if (NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
            //        //    NOSButton.gameObject.SetActive(canUseNos);

            //        //            if (NOSButtonSteeringWheel && NOSButtonSteeringWheel.gameObject.activeInHierarchy != canUseNos)
            //        //NOSButtonSteeringWheel.gameObject.SetActive(canUseNos);

            //        if (joystick && joystick.gameObject.activeInHierarchy)
            //            joystick.gameObject.SetActive(false);

            //        if (leftButton.gameObject.activeInHierarchy)
            //            leftButton.gameObject.SetActive(false);
            //        if (rightButton.gameObject.activeInHierarchy)
            //            rightButton.gameObject.SetActive(false);

            //        break;

            //    case RCC_Settings.MobileController.Joystick:

            //        gyroInput = 0f;

            //        //if (steeringWheel && steeringWheel.gameObject.activeInHierarchy)
            //        //	steeringWheel.gameObject.SetActive (false);

            //        if (NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
            //            NOSButton.gameObject.SetActive(canUseNos);

            //        if (joystick && !joystick.gameObject.activeInHierarchy)
            //        {
            //            joystick.gameObject.SetActive(true);
            //            brakeButton.transform.position = orgBrakeButtonPos;
            //        }

            //        if (leftButton.gameObject.activeInHierarchy)
            //            leftButton.gameObject.SetActive(false);

            //        if (rightButton.gameObject.activeInHierarchy)
            //            rightButton.gameObject.SetActive(false);

            //        break;

            //}
        }

        //


        //       switch (RCCSettings.mobileController) {
        //
        // case RCC_Settings.MobileController.TouchScreen:
        //
        // 	gyroInput = 0f;
        //
        // 	if(steeringWheel && steeringWheel.gameObject.activeInHierarchy)
        // 		steeringWheel.gameObject.SetActive(false);
        //
        // 	//if(NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
        // 	//	NOSButton.gameObject.SetActive(canUseNos);
        //
        // 	if(joystick && joystick.gameObject.activeInHierarchy)
        // 		joystick.gameObject.SetActive(false);
        //
        // 	if(!leftButton.gameObject.activeInHierarchy){
        //
        // 		brakeButton.transform.position = orgBrakeButtonPos;
        // 		leftButton.gameObject.SetActive(true);
        //
        // 	}
        //
        // 	if(!rightButton.gameObject.activeInHierarchy)
        // 		rightButton.gameObject.SetActive(true);
        //
        //               if (jumpBtnButtonsControls)
        //                   jumpBtnButtonsControls.SetActive(true);
        //               if (jumpBtnTiltControls)
        //                   jumpBtnTiltControls.SetActive(false);
        //               break;
        //
        // case RCC_Settings.MobileController.Gyro:
        //
        // 	gyroInput = Input.acceleration.x * RCCSettings.gyroSensitivity;
        // 	brakeButton.transform.position = leftButton.transform.position;
        //
        // 	if(steeringWheel.gameObject.activeInHierarchy)
        // 		steeringWheel.gameObject.SetActive(false);
        //
        // 	//if(NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
        // 	//	NOSButton.gameObject.SetActive(canUseNos);
        //
        // 	if(joystick && joystick.gameObject.activeInHierarchy)
        // 		joystick.gameObject.SetActive(false);
        //
        // 	if(leftButton.gameObject.activeInHierarchy)
        // 		leftButton.gameObject.SetActive(false);
        //
        // 	if(rightButton.gameObject.activeInHierarchy)
        // 		rightButton.gameObject.SetActive(false);
        //
        //               if (jumpBtnButtonsControls)
        //                   jumpBtnButtonsControls.SetActive(false);
        //               if (jumpBtnTiltControls)
        //                   jumpBtnTiltControls.SetActive(true);
        //
        //               break;
        //
        // case RCC_Settings.MobileController.SteeringWheel:
        //
        // 	gyroInput = 0f;
        //
        // 	if(!steeringWheel.gameObject.activeInHierarchy){
        // 		steeringWheel.gameObject.SetActive(true);
        // 		brakeButton.transform.position = orgBrakeButtonPos;
        // 	}
        //
        //               //if (NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
        //               //    NOSButton.gameObject.SetActive(canUseNos);
        //
        //   //            if (NOSButtonSteeringWheel && NOSButtonSteeringWheel.gameObject.activeInHierarchy != canUseNos)
        // 		//NOSButtonSteeringWheel.gameObject.SetActive(canUseNos);
        //
        // 	if(joystick && joystick.gameObject.activeInHierarchy)
        // 		joystick.gameObject.SetActive(false);
        //
        // 	if(leftButton.gameObject.activeInHierarchy)
        // 		leftButton.gameObject.SetActive(false);
        // 	if(rightButton.gameObject.activeInHierarchy)
        // 		rightButton.gameObject.SetActive(false);
        //
        // 	break;
        //
        // case RCC_Settings.MobileController.Joystick:
        //
        // 	gyroInput = 0f;
        //
        // 	if (steeringWheel && steeringWheel.gameObject.activeInHierarchy)
        // 		steeringWheel.gameObject.SetActive (false);
        //
        // 	if (NOSButton && NOSButton.gameObject.activeInHierarchy != canUseNos)
        // 		NOSButton.gameObject.SetActive (canUseNos);
        //
        // 	if (joystick && !joystick.gameObject.activeInHierarchy) {
        // 		joystick.gameObject.SetActive (true);
        // 		brakeButton.transform.position = orgBrakeButtonPos;
        // 	}
        //
        // 	if(leftButton.gameObject.activeInHierarchy)
        // 		leftButton.gameObject.SetActive(false);
        //
        // 	if(rightButton.gameObject.activeInHierarchy)
        // 		rightButton.gameObject.SetActive(false);
        //
        // 	break;
        //
        // }

        accelerationInput = ReadButtonInput(accelerateButton) + ReadButtonInput(smoothAccelerateButton);
        brakeForceInput = ReadButtonInput(brakeButtonUI);
        steeringLeftInput = ReadButtonInput(steerLeftButton);
        steeringRightInput = ReadButtonInput(steerRightButton);
        handbrakeForce = ReadButtonInput(handbrakeButtonUI);





#if UNITY_EDITOR
// Use Unity's built-in input axes in Editor (WASD or Arrow keys)
float vertical = Input.GetAxis("Vertical");
float horizontal = Input.GetAxis("Horizontal");

// If using keyboard, override UI button inputs

#endif

        //     if (steeringWheel)
        //steeringWheelInput = steeringWheel.input;

        //if (joystick)
        //    joystickInput = joystick.inputHorizontal;

#if UNITY_EDITOR
        //   gasInput = Input.GetAxis(RCCSettings.verticalInput);
        //  steeringWheelInput = Input.GetAxis(RCCSettings.horizontalInput);
#endif

        nitroInputStrength = Mathf.Clamp((ReadButtonInput(nitroButton) + ReadButtonInput(steeringNitroButton)) * 2.5f, 1f, 3.5f);

        // While the horse is falling from a hurdle, ignore run / boost / steer so it
        // comes straight down to the ground and only responds again once it has landed.
        bool blockControlForHurdleFall = IsHorseFallingFromHurdle();

        if (!blockControlForHurdleFall && nitroActivated && powerBoostController.NoS > 0)
        {
            accelerationInput = 1;
            nitroInputStrength = 5;
            horseController.Shift = true;
            //RCC_SceneManager.Instance.activePlayerVehicle.maxspeed = vehicleMaxSpeed + 40;
            //RCC_SceneManager.Instance.activePlayerCamera.TPSDistance = Mathf.Lerp(RCC_SceneManager.Instance.activePlayerCamera.TPSDistance, RCC_SceneManager.Instance.activePlayerCamera.BackUpTPSDistance + 2, Time.deltaTime * 5f);
            //RCC_SceneManager.Instance.activePlayerCamera.TPSHeight = RCC_SceneManager.Instance.activePlayerCamera.BackUpTPSHeight + 1;

        }
        else if (!blockControlForHurdleFall && nitroActivated)
        {
            nitroInputStrength = 1;
            accelerationInput = 1;
            horseController.Shift = false;

            //RCC_SceneManager.Instance.activePlayerVehicle.maxspeed = vehicleMaxSpeed;
            //startNitros = false;
            //RCC_SceneManager.Instance.activePlayerCamera.TPSDistance = Mathf.Lerp(RCC_SceneManager.Instance.activePlayerCamera.TPSDistance, RCC_SceneManager.Instance.activePlayerCamera.BackUpTPSDistance, Time.deltaTime * 2.5f);
            //RCC_SceneManager.Instance.activePlayerCamera.TPSHeight = RCC_SceneManager.Instance.activePlayerCamera.BackUpTPSHeight;
        }

        if (horseController)
        {



            //if (NOSInput > 1 && RCC_SceneManager.Instance.activePlayerVehicle.NoS > 5)
            //{
            //    RCC_SceneManager.Instance.activePlayerVehicle.maxspeed = vehicleMaxSpeed + 50;
            //    gasInput = 1;
            //}else
            //{
            //    RCC_SceneManager.Instance.activePlayerVehicle.maxspeed = vehicleMaxSpeed ;

            //}
            //if (MConstants.isRaceOver )
            //{
            //    gasInput = 0;
            //    if (RCC_SceneManager.Instance.activePlayerVehicle.speed > 0)
            //    {
            //        brakeInput = 1;

            //    }
            //    else
            //    {
            //        brakeInput = 0;

            //    }
            //    disableControlles();
            //}\



            Vector3 moveInput = new Vector3(-steeringLeftInput + steeringRightInput + steeringInput + gyroscopeInput + joystickDirectionInput, 0, accelerationInput - brakeForceInput);

            if (blockControlForHurdleFall)
            {
                // Drop straight down and ignore run / boost until the horse lands.
                moveInput = Vector3.zero;
                horseController.Shift = false;
            }

            horseController.Move(moveInput, false);


        }
        else
        {
            //RCC_SceneManager.Instance.activePlayerVehicle.gasInput = gasInput;
            //RCC_SceneManager.Instance.activePlayerVehicle.brakeInput = brakeInput;
            //RCC_SceneManager.Instance.activePlayerVehicle.steerInput = -leftInput + rightInput + steeringWheelInput + gyroInput + joystickInput;
        }

        // for (int i = 0; i < NosData.Length; i++)
        // {
        //     NosData[i].fillAmount = (playerPowerController.NoS / 100f);
        // }

        //
        if (powerBoostController)
        {
            for (int i = 0; i < nitroUIBars.Length; i++)
            {
                nitroUIBars[i].fillAmount = (powerBoostController.NoS / 100f);
            }
        }
        //


    }

    // True while the active horse is falling after hitting a hurdle. Only active child
    // detectors are considered; some horse prefabs also contain inactive attack triggers.
    bool IsHorseFallingFromHurdle()
    {
        if (horseController == null) return false;

        if (hurdleDetector == null)
            hurdleDetector = horseController.GetComponentInChildren<HorseHurdleCollisionDetection>();

        return hurdleDetector != null && hurdleDetector.IsFallingFromHurdle;
    }

    // Gets input from button.
    // float GetInput(HorseUIController button)
    float ReadButtonInput(HorseUIController button)
    {

        if (button == null)
            return 0f;

        return (button.input);

    }

    //  public void OnNosButton()
    public void ActivateNitro()
    {

        Time.timeScale = 1f;

        if (PlayerDataController.instance.playerStats.NitrosCount > 0 && powerBoostController.NoS < 50 && !horseController.Jump)
        {
            // Refill the NoS boost gauge to full when the player spends one of
            // their stored Nitros. Without this the gauge stays low after the
            // first use, so subsequent presses just silently decrement
            // NitrosCount (4x → 3x → 2x → 1x → 0x) without giving any actual
            // boost — which is why "4x" appears but the boost only works once.
           // powerBoostController.NoS = 100;
            nitroActivated = true;
            PlayerDataController.instance.playerStats.NitrosCount--;
            HudMenuManager.instance.updateUIContent();
        }
        else if (powerBoostController.NoS > 20 && !horseController.Jump)
        {
            nitroActivated = true;
        }

    }

    // public void OnNosButtonUP()
    public void DeactivateNitro()
    {

        nitroActivated = false;
        nitroInputStrength = 1;
        horseController.Shift = false;
    }

    // public void AutoNosButton()
    public void TriggerAutoNitro()
    {

        powerBoostController.NoS = 100;
        nitroActivated = true;


    }

    //  public void SetQuality(int index)
    public void ApplyGraphicsQuality(int index)
    {

        QualitySettings.SetQualityLevel(index);

    }

    //   public void SetJump()
    public void TriggerJump()
    {
        Time.timeScale = 1f;

        if (horseController == null) return;

        HorseAnimationSync networkSync = horseController.GetComponent<HorseAnimationSync>();
        if (networkSync != null && networkSync.isOwned)
        {
            networkSync.RequestJump();
        }
        else
        {
            // Keep the existing behaviour for offline/single-player horses.
            horseController.SetJump();
        }

        HudMenuManager.instance.jumpSoundEffect.SetActive(true);
        HudMenuManager.instance.isJumpMessageVisible = true;
        Invoke("ResetJumpState", 5);
    }

    //  void resetJump()
    void ResetJumpState()
    {
        HudMenuManager.instance.isJumpMessageVisible = false;

    }
    //  public void SetSprint()
    public void TriggerSprint()
    {
        horseController.Shift = true;
    }

}

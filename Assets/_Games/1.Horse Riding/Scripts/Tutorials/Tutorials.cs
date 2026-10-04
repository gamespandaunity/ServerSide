using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*using UnityEngine.Experimental.PlayerLoop;*/
using UnityEngine.Experimental;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Tutorials : MonoBehaviour
{
    public static Tutorials instance;

    [Header("Tutorial UI")]
    [Space(10)]
    [FormerlySerializedAs("runButtonObject")] public GameObject runBtnUI ;
    [FormerlySerializedAs("jumpButtonObject")] public GameObject jumpBtnUI;
    [FormerlySerializedAs("rightButtonObject")] public GameObject moveRightBtnUI;
    [FormerlySerializedAs("leftButtonObject")] public GameObject moveLeftBtnUI;
    [FormerlySerializedAs("nostButtonObject")] public GameObject nitroButtonUI;

    [Header("Tutorial Animations")]
    [Space(10)]
    [FormerlySerializedAs("nosTutorial")] public GameObject nitroTutorial;
    [FormerlySerializedAs("runTutorial")] public GameObject runTutorialUI;
    [FormerlySerializedAs("jumpTutorial")] public GameObject jumpTutorialUI;
    [FormerlySerializedAs("rightTutorial")] public GameObject rightMoveTutorialUI;
    [FormerlySerializedAs("leftTutorial")] public GameObject leftMoveTutorialUI;

    [FormerlySerializedAs("AllTutorials")] public List<GameObject> tutorialUIElements ;
    [FormerlySerializedAs("NitroCount")] public Text nitroCountText;


    public static string tutorialType;
    public static bool aiStopped = true;
    public static bool isTutorialActive = false;
    private int currentNitroAmount;
    private Button runUIButton;
    void Awake()
    {
        instance = this;

        if (MConstants.CurrentCHAMPION_MODE != MConstants.CHAMPION_MODES.DUABI_CHAMPION)
        {
            if (MConstants.CurrentLevelNumber > 4)
            {
                isTutorialActive = false;
            }
        }
    }
    

    void Start()
    {
        runUIButton = runBtnUI .GetComponent<Button>();
        // runButton.OnPointerDown(runButton);
        //() => {runTutorial.SetActive(false); }
    }
    
  //  public void ShowUI()
    public void DisplayTutorialUI()
    {
        runBtnUI .SetActive(false);
        jumpBtnUI.SetActive(false);
        moveRightBtnUI.SetActive(false);
        moveLeftBtnUI.SetActive(false);
        nitroButtonUI.SetActive(false);
        
        if (MConstants.CurrentCHAMPION_MODE == MConstants.CHAMPION_MODES.DUABI_CHAMPION)
        {
            if (MConstants.CurrentLevelNumber < 5)
            {
                isTutorialActive = true;
            }
            switch (MConstants.CurrentLevelNumber)
            {
                case 1:
                    //runTutorial
                    runBtnUI .SetActive(true);
                    ToggleTutorialAnimation(runTutorialUI,true);
                    aiStopped = true;
                    break;
                case 2:
                    //jumpTutorial
                    runBtnUI .SetActive(true);
                    jumpBtnUI.SetActive(false);
                    aiStopped = true;
                    break;
                case 3:
                    //nosTutorial
                    runBtnUI .SetActive(true);
                    jumpBtnUI.SetActive(true);
                    // Show nos tutorial with delay
                    //
                    aiStopped = true;
                    break;
                case 4:
                    //turnTutorial
                    runBtnUI .SetActive(true);
                    jumpBtnUI.SetActive(true);
                    nitroButtonUI.SetActive(true);
                    moveRightBtnUI.SetActive(false);
                    moveLeftBtnUI.SetActive(false);
                    // StartCoroutine(StartNosWithDelay());
                    aiStopped = true;
                    break;
                case 5:
                    moveRightBtnUI.SetActive(false);
                    moveLeftBtnUI.SetActive(false);
                    runBtnUI .SetActive(true);
                    jumpBtnUI.SetActive(true);
                    nitroButtonUI.SetActive(true);
                    break;
            }
        }
        else
        {
            aiStopped = false;
            runBtnUI .SetActive(true);
            jumpBtnUI.SetActive(true);
            moveRightBtnUI.SetActive(true);
            moveLeftBtnUI.SetActive(true);
            nitroButtonUI.SetActive(true);
            isTutorialActive = false;
        }
    }

   // public void HideAllTutorials()
    public void HideAllTutorialElements()
    {
        foreach (var tutorial in tutorialUIElements )
        {
            tutorial.SetActive(false);
        }
    }

  //  public void PlayAnimation(GameObject target,bool active)
    public void ToggleTutorialAnimation(GameObject target,bool active)
    {
        target.SetActive(active);
    }

   // public  void JumpOccur()
    public  void OnJumpStarted()
    {
        if (jumpTutorialUI.activeInHierarchy)
            return;
        jumpTutorialUI.SetActive(true);
        ToggleTutorialAnimation(jumpTutorialUI,true);
        jumpBtnUI.SetActive(true);
    }
    
    //public  void JumpCrossed()
    public  void OnJumpCompleted()
    {
        jumpTutorialUI.SetActive(false);
        ToggleTutorialAnimation(jumpTutorialUI,false);
        jumpBtnUI.SetActive(false);
    }
    
   // public  void LeftTurnOccur()
    public  void OnLeftTurnStarted()
    {
        moveRightBtnUI.SetActive(true);
        // PlayAnimation(rightTutorial,true);
        moveLeftBtnUI.SetActive(true);
        ToggleTutorialAnimation(leftMoveTutorialUI,true);
    }
    
   // public  void RightTurnOccur()
    public  void OnRightTurnStarted()
    {
        moveRightBtnUI.SetActive(true);
        ToggleTutorialAnimation(rightMoveTutorialUI,true);
        moveLeftBtnUI.SetActive(true);
        // PlayAnimation(leftTutorial,true);
    }

   // public void TurnCrossed()
    public void OnTurnCompleted()
    {
        ToggleTutorialAnimation(rightMoveTutorialUI,false);
        rightMoveTutorialUI.SetActive(false);
        
        ToggleTutorialAnimation(leftMoveTutorialUI,false);
        leftMoveTutorialUI.SetActive(false);
    }
    

   // public void ShowNitro()
    public void ShowNitroTutorial()
    {
        currentNitroAmount++;
        nitroCountText.text = "x" +currentNitroAmount;

        if (nitroButtonUI.activeInHierarchy)
        {
            return; 
        }
        nitroButtonUI.SetActive(true);
        ToggleTutorialAnimation(nitroTutorial,true);
    }
    
   // public void HideNitro()
    public void HideNitroTutorial()
    {
        if (HorseMobileButton.Instance.powerBoostController.NoS <1 && currentNitroAmount > 0)
        {
            currentNitroAmount--;
            nitroCountText.text = "x" +currentNitroAmount;
            HorseMobileButton.Instance.DeactivateNitro();
            nitroButtonUI.SetActive(false);
            ToggleTutorialAnimation(nitroTutorial,false);
        }
    }
}

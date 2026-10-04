using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class Intro : Singleton<Intro>
{
    [FormerlySerializedAs("cutScene")]
    public Camera CutsceneCamera;

    [FormerlySerializedAs("tempCutScene")]
    public Camera TempCutsceneCamera;

    [FormerlySerializedAs("groundModel")]
    public GameObject GroundModel;

    [FormerlySerializedAs("quad")]
    public GameObject GroundQuad;

    private float CutsceneAnimDuration = 3f;
    private int IntroActionState = -1;
    private float IntroStartTime;
    private bool HasIntroPlayedOnce;
    private float ElapsedIntroTime;
    private Camera IntroCam;
    private Animation CameraAnimation;
    private GameObject GroundCenterMarker;
    private GameObject IntroCameraPivotObject;
    private GameObject StrikerPlayer;
    private GameObject NonStrikerPlayer;
    private GameObject BatsmanExitPoint;
    private int[] WarmUpAnimSequence = new int[9];
    private Transform IntroCameraTransform;
    private Transform GroundCenterTransform;
    private Transform CameraPivotTransform;
    private Transform StrikerTransform;
    private Transform NonStrikerTransform;
    private Transform BatsmanExitTransform;
    private Transform BatsmanReferenceTransform;
    private GroundController GroundLogic;
    private float EntryWalkSpeed = 1.5f;
    private float ExitWalkSpeed = 0.9f;

    protected void Awake()
    {
        CameraAnimation = TempCutsceneCamera.GetComponent<Animation>();
        GroundLogic = GameObject.Find("GroundController").GetComponent<GroundController>();
        GroundCenterMarker = GameObject.Find("GroundCenterPoint");
        GroundCenterTransform = GroundCenterMarker.transform;
        IntroCameraPivotObject = GameObject.Find("IntroCameraPivot");
        CameraPivotTransform = IntroCameraPivotObject.transform;
        IntroCam = GameObject.Find("IntroCamera").GetComponent("Camera") as Camera;
        IntroCameraTransform = GameObject.Find("IntroCamera").transform;
        BatsmanReferenceTransform = GameObject.Find("Batsman/Armature/BatsmanRefPoint").transform;

    }


    public void UpdateIntro()
    {
        ElapsedIntroTime += Time.deltaTime;
        Singleton<GameData>.instance.canPauseGameplay = false;
        if (IntroActionState != 0 && IntroActionState != 1 && IntroActionState != 3)
        {
            if (IntroActionState == 2 || IntroActionState == 6)
            {
                if (StrikerTransform.localScale.x == 1f)
                {
                    IntroCameraTransform.localPosition += new Vector3(Time.deltaTime * 1.8f, 0f, Time.deltaTime * -3.5f);
                }
                else
                {
                    IntroCameraTransform.localPosition += new Vector3(Time.deltaTime * -1.8f, 0f, Time.deltaTime * -3.5f);
                }
            }
            else if (IntroActionState == 4)
            {
                IntroCameraTransform.localPosition += new Vector3(Time.deltaTime * ExitWalkSpeed / 2f, 0f, Time.deltaTime * ExitWalkSpeed);
            }
            else if (IntroActionState == 5)
            {
                IntroCameraTransform.localPosition -= new Vector3(0f, 0f, Time.deltaTime * (0f - ExitWalkSpeed) * 1.7f);
            }
        }
        if (IntroStartTime + CutsceneAnimDuration < Time.time)
        {
            if (IntroActionState == 0)
            {
                zoomCameraToStriker();
            }
            else if (IntroActionState == 1)
            {
                moveCameraToNonStriker();
            }
            else if (IntroActionState == 2)
            {
                focussedOnNonStriker();
            }
            else if (IntroActionState == 3)
            {
                introCompleted();
            }
            else if (IntroActionState == 4)
            {
                batsmanExitStopped();
            }
            else if (IntroActionState == 5)
            {
                batsmanEntryStopped();
            }
        }
    }

    private void WarmUpAnims()
    {
        for (int i = 0; i <= 8; i++)
        {
            WarmUpAnimSequence[i] = Random.Range(1, 6);
        }
        HasIntroPlayedOnce = true;
    }

    public void initGameIntro()
    {
        if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 8)
        {
            Singleton<GameData>.instance.introCompleted();
            return;
        }
        Cutscenes.instance.PlayCutscene("Intro");
        if (GroundLogic != null)
        {
            GroundLogic.initGameIntro();
        }
        IntroStartTime = Time.time;
        CutsceneAnimDuration = 5f;
        IntroActionState = 0;
        Singleton<GroundController>.instance.StartIntroFielderAnimation();
    }

    private void zoomCameraToStriker()
    {
        CutsceneCamera.enabled = false;
        TempCutsceneCamera.enabled = true;
        CameraAnimation.Play("TempIntro");
        GroundModel.transform.eulerAngles = new Vector3(0f, -272.5f, 0f);
        GroundQuad.SetActive(value: false);
        IntroActionState = 1;
        CutsceneAnimDuration = 2f;
        IntroStartTime = Time.time;
        Singleton<GroundController>.instance.StopIntroFielderAnimation();
        StrikerPlayer = Singleton<GroundController>.instance.batsmanObject;
        StrikerTransform = StrikerPlayer.transform;
        StrikerPlayer.transform.position = new Vector3(-60f, 0f, -6f);
        StrikerPlayer.transform.eulerAngles = new Vector3(StrikerTransform.eulerAngles.x, 90f, StrikerTransform.eulerAngles.z);
        int num = Random.Range(1, 4);
        int num2 = 4 - num;
        string animation = "WCCLite_BatsmanIntro01";
        string animation2 = "WCCLite_BatsmanIntro02";
        float length = StrikerPlayer.GetComponent<Animation>()[animation].length;
        int num3 = (int)Random.Range(0f, length);
        StrikerPlayer.GetComponent<Animation>().Play(animation);
        StrikerPlayer.GetComponent<Animation>()[animation].time = 0f;
        NonStrikerPlayer = Singleton<GroundController>.instance.currentRunner;
        NonStrikerTransform = NonStrikerPlayer.transform;
        NonStrikerTransform.position = new Vector3(-67f, 0f, -8f);
        NonStrikerTransform.eulerAngles = new Vector3(NonStrikerTransform.eulerAngles.x, 90f, NonStrikerTransform.eulerAngles.z);
        length = NonStrikerPlayer.GetComponent<Animation>()[animation2].length;
        num3 = (int)Random.Range(0f, length);
        NonStrikerPlayer.GetComponent<Animation>().Play(animation2);
        NonStrikerPlayer.GetComponent<Animation>()[animation2].time = 0f;
        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.Status = "not out";
        //if (CONTROLLER.PlayModeSelected == 7)
        //{
        //	TestMatchBatsman.SetStatus(CONTROLLER.BattingTeamIndex, CONTROLLER.StrikerIndex, "not out");
        //}
        Singleton<BatsmanRecord>.instance.UpdateRecord(CONTROLLER.BattingTeamIndex, CONTROLLER.StrikerIndex);
        Singleton<BatsmanRecord>.instance.Hide(boolean: false);
        IntroCameraTransform.parent = StrikerTransform;
        if (StrikerTransform.localScale.x == 1f)
        {
            IntroCameraTransform.localPosition = new Vector3(7f, 0.8f, -0.05f);
            IntroCameraTransform.localEulerAngles = new Vector3(4f, 190f, 0f);
        }
        else
        {
            IntroCameraTransform.localPosition = new Vector3(-7f, 0.8f, -0.05f);
            IntroCameraTransform.localEulerAngles = new Vector3(4f, 190f, 0f);
        }
    }

    private void moveCameraToNonStriker()
    {
        IntroActionState = 2;
        IntroStartTime = Time.time;
        CutsceneAnimDuration = 1f;
        Singleton<BatsmanRecord>.instance.Hide(boolean: true);
    }

    private void focussedOnNonStriker()
    {
        IntroActionState = 3;
        IntroStartTime = Time.time;
        CutsceneAnimDuration = 2f;
        CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.Status = "not out";

        Singleton<BatsmanRecord>.instance.UpdateRecord(CONTROLLER.BattingTeamIndex, CONTROLLER.NonStrikerIndex);
        Singleton<BatsmanRecord>.instance.Hide(boolean: false);
    }

    private void introCompleted()
    {
        TempCutsceneCamera.enabled = false;
        IntroActionState = 6;
        if (Singleton<GameData>.instance != null)
        {
            Singleton<GameData>.instance.introCompleted();
        }
        GroundModel.transform.eulerAngles = Vector3.zero;
        GroundQuad.SetActive(value: true);
        HasIntroPlayedOnce = false;
    }

    public void initBatsmanExit()
    {
        IntroActionState = 4;
        IntroStartTime = Time.time;
        CutsceneAnimDuration = 2.5f;
        BatsmanExitPoint = GroundLogic.batsmanObject;
        BatsmanExitTransform = BatsmanExitPoint.transform;
        BatsmanExitTransform.position = new Vector3(-50f, 0f, -8f);
        BatsmanExitTransform.eulerAngles = new Vector3(BatsmanExitTransform.eulerAngles.x, 270f, BatsmanExitTransform.eulerAngles.z);
        if (GroundLogic != null)
        {
            GroundLogic.EnableFielders(boolean: false);
            GroundLogic.initBatsmanExit();
        }
        BatsmanExitPoint.GetComponent<Animation>().Play("WCCLite_BatsmanExit");
        BatsmanExitPoint.GetComponent<Animation>()["WCCLite_BatsmanExit"].time = 0f;
        IntroCameraTransform.parent = BatsmanExitTransform;
        IntroCameraTransform.localPosition = new Vector3(-1f, 2f, 4f);
        IntroCameraTransform.localEulerAngles = new Vector3(15f, 180f, 0f);
        IntroCam.fieldOfView = 40f;
        IntroCam.enabled = true;
    }

    public void batsmanExitStopped()
    {
        if (Singleton<GameData>.instance != null)
        {
            Singleton<GameData>.instance.batsmanExitStopped();
        }
    }

    public void initBatsmanEntry()
    {
        IntroActionState = 5;
        IntroStartTime = Time.time;
        CutsceneAnimDuration = 2.5f;
        StrikerPlayer = GroundLogic.batsmanObject;
        StrikerTransform = StrikerPlayer.transform;
        StrikerTransform.position = new Vector3(-67f, 0f, -8f);
        StrikerTransform.eulerAngles = new Vector3(StrikerTransform.eulerAngles.x, 90f, StrikerTransform.eulerAngles.z);
        GroundModel.transform.eulerAngles = new Vector3(0f, -272.5f, 0f);
        GroundQuad.SetActive(value: false);
        int num = Random.Range(2, 3);
        string animation = ((num <= 1) ? "WCCLite_BatsmanIntro01" : ((num > 2) ? "WCCLite_BatsmanIntro03" : "WCCLite_BatsmanIntro02"));
        animation = "WCCLite_BatsmanIntro02";
        //float length = Striker.GetComponent<Animation>()[animation].length;
        //int num2 = (int)Random.Range(0f, length);
        StrikerPlayer.GetComponent<Animation>().Play(animation);
        StrikerPlayer.GetComponent<Animation>()[animation].time = 1f;
        IntroCameraTransform.parent = StrikerTransform;
        IntroCameraTransform.localPosition = new Vector3(-7f, 5.5f, -6f);
        IntroCameraTransform.localEulerAngles = new Vector3(20f, 50f, 0f);
    }

    public void batsmanEntryStopped()
    {
        IntroActionState = -1;
        if (Singleton<GameData>.instance != null)
        {
            Singleton<GameData>.instance.batsmanEntryStopped();
        }
    }

    public void destroyGO()
    {
        IntroActionState = -1;
        IntroCameraTransform.parent = null;
        if (StrikerPlayer != null)
        {
            StrikerPlayer = null;
        }
        if (NonStrikerPlayer != null)
        {
            NonStrikerPlayer = null;
        }
        if (BatsmanExitPoint != null)
        {
            BatsmanExitPoint = null;
        }
    }
}

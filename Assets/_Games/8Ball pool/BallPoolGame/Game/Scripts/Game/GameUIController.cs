using BallPool;
using BallPool.Mechanics;
using NetworkManagement;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityExtensions;

/// <summary>
/// Game user interface controller.
/// </summary>
public class GameUIController : MonoBehaviour
{
    public event Action<bool> OnShotExecuted;
    public event Action TriggerHome;
    [FormerlySerializedAs("physicsManager")] public PhysicsHandeler PhysicsHandler;
    [FormerlySerializedAs("gameManager")] public GameManager GameController;
    [FormerlySerializedAs("replayUI")] public RectTransform ReplayPanel;
    [FormerlySerializedAs("gameUI")] public RectTransform GameInterface;
    [FormerlySerializedAs("cameraToggleText")] public Text ToggleCameraText;
    [FormerlySerializedAs("shotOnUpToggle")] public Toggle ShotOnToggle;
    [FormerlySerializedAs("shotButton")] public Button ShootButton;
    [FormerlySerializedAs("tableCamera")] public Camera TableViewCamera;
    [FormerlySerializedAs("replayNumber")] public Dropdown ReplaySelection;
    [FormerlySerializedAs("camera3DTargetngImage")] public RectTransform Camera3DTargetImage;
    [FormerlySerializedAs("camera2D")] public Camera Camera2DView;
    [FormerlySerializedAs("forceSlider")] public Slider ForcePowerSlider;
    [FormerlySerializedAs("homeScene")] public string MainMenuScene;
    [FormerlySerializedAs("playScene")] public string GameScene;
    [FormerlySerializedAs("cueCamera")] public Camera CueViewCamera;
    [FormerlySerializedAs("cue2dRenderer")] public MeshRenderer CueRenderer2D;
    [FormerlySerializedAs("cameraOrthoSize")] public float CameraZoomLevel = 1.9f;
    [FormerlySerializedAs("cueParent")] public GameObject CueHolder;
    [FormerlySerializedAs("micObject")] public GameObject MicrophoneObject;
    [FormerlySerializedAs("speakerObj")] public GameObject SpeakerObject;

    public static GameUIController instance;
    public int replayNumberValue
    {
        get;
        private set;
    }

    [System.NonSerialized] public bool is3DMode;
    [System.NonSerialized] public bool isShotOnRelease;

    void Awake()
    {

        if (instance == null)
        {
            instance = this;
        }
        if (!EightBallPoolNetworkManager.initialized)
        {
            enabled = false;
            return;
        }

        //camera2D.orthographicSize *= 1.6f / ((float)Screen.width / (float)Screen.height); //RAR




        if (!AightBallPoolNetworkGameAdapter.is3DGraphics)
        {
            is3DMode = false;
        }
        else
        {
            is3DMode = DataManager.GetIntData("Is3D") == 1;
        }
        Camera3DTargetImage.gameObject.SetActive(is3DMode && GameController.shotController.cueControlMode == ShotController.CueViewMode.ThirdPerson);

        isShotOnRelease = DataManager.GetIntData("ShotOnUp") == 0;
        ShootButton.image.enabled = !isShotOnRelease;
        ShootButton.enabled = !isShotOnRelease;
        ShootButton.GetComponentInChildren<Text>().text = isShotOnRelease ? "Auto shot" : "";
        ShotOnToggle.isOn = isShotOnRelease;
        ToggleCameraView();
        UpdateControlState();
        TriggerAutoShot();
    }
    private void OnEnable()
    {
        Camera2DView.orthographicSize *= CameraZoomLevel / ((float)Screen.width / (float)Screen.height); //RAR    un comment to revert
    }
    void Start()
    {


        if (BallPoolGameLogic.playMode == BallPool.PlayMode.Replay)
        {
            ReplaySelection.options = new List<Dropdown.OptionData>(0);
            replayNumberValue = 0;
            int replayCount = PhysicsHandler.ReplayManager.GetReplayDataCount();
            //Debug.Log("replayCount " + replayCount);
            for (int i = 0; i < replayCount; i++)
            {
                ReplaySelection.options.Add(new Dropdown.OptionData("Replay " + i));
            }
            ReplaySelection.value = 1;
            ReplaySelection.value = 0;
        }
        if (!GameModeManager.isAI && staticVariables.gameFeatures.isAgora) // ai mode
        {
            MicrophoneObject.SetActive(true);
            SpeakerObject.SetActive(true);
        }

    }
    public void HandleReplaySelection()
    {
        if (BallPoolGameLogic.playMode == BallPool.PlayMode.Replay)
        {
            replayNumberValue = ReplaySelection.value;
            //Debug.Log("replayNumber " + replayNumberValue);
            GameController.UpdateBallsState(ReplaySelection.value);
        }
    }
    public void ApplyReplaySettings(int index, bool applyNow)
    {
        this.replayNumberValue = replayNumberValue;
        ReplaySelection.value = replayNumberValue;
        if (applyNow)
        {
            GameController.UpdateBallsState(ReplaySelection.value);
        }
    }
    public void HandleControlChange()
    {
        UpdateControlState();
    }
    public void UpdateControlState()
    {
        ReplayPanel.gameObject.SetActive(BallPoolGameLogic.playMode == BallPool.PlayMode.Replay);
        GameInterface.gameObject.SetActive(!ReplayPanel.gameObject.activeSelf);
        HandleReplaySelection();
    }
    public void HandleCameraToggle()
    {
        is3DMode = !is3DMode;
        Camera3DTargetImage.gameObject.SetActive(is3DMode && GameController.shotController.cueControlMode == ShotController.CueViewMode.ThirdPerson);
        DataManager.SetIntData("Is3D", is3DMode ? 1 : 0);
        ToggleCameraView();
    }
    public void TriggerAutoShot()
    {
        isShotOnRelease = ShotOnToggle.isOn;
        ShootButton.GetComponent<Image>().enabled = !isShotOnRelease;
        ShootButton.image.enabled = !isShotOnRelease;
        ShootButton.enabled = !isShotOnRelease;
        ShootButton.GetComponentInChildren<Text>().text = isShotOnRelease ? "Auto shot" : "";
        DataManager.SetIntData("ShotOnUp", isShotOnRelease ? 0 : 1);
        ForcePowerSlider.gameObject.SetActive(!(isShotOnRelease && !InputOutput.isMobilePlatform));
    }
    public void ExecuteShot(bool follow)
    {
        if (OnShotExecuted != null)
        {
            OnShotExecuted(follow);
        }
    }
    public static bool EightballPoolLeaveGame = false;
    public void ReturnToHome()
    {
        GameModeManager.isAI = false; //RAR


        PhysicsHandler.Deactivate();
        if (EightBallPoolNetworkManager.mainPlayer != null)
        {
            EightBallPoolNetworkManager.mainPlayer.state = NetworkManagement.PlayerState.Online;
        }
        if (GameModeManager.instance != null)
        {
            GameModeManager.isAI = false;
        }
        MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
        if (NetworkGameManager.Instance)
        {
            NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
          //  NetworkGameManager.Instance.CmdReloadServer();
            this.Delay(1, () => MirrorNetwork.singleton.StopClient());
        }

        //ScreenNavigotor_Custom.GameScreenRefs.Clear();
        //ScreenNavigotor_Custom.GameScreenStack.Clear();

        SceneLoaderUtility.LoadScene("Home");

        //print("status room list" + RoomsListManager.instance.isbrowseChallenge);
    }
    public void ForceReturnToHome()
    {
        Physics.simulationMode = SimulationMode.FixedUpdate;
        if (GameModeManager.isAI) // ai mode
        {
            ApiAndRoomManager._instance.WinnerLossAIChallenge("ai");

        }
        else
        {
            GameModeManager.isAI = false; //RAR
            staticVariables.playOnceAi = true;
            MyEightBallNetwork.Instance.CmdGameLeave();
            //print("lllllll" + PunNetwork.instance.isPause);
            //PunNetwork.instance.CallPauseStateRPC(true);
            //ConstantsData_M.LogInfo("opponenIsReadToPlay " + GameController.shotController.isOpponentReady);
            //if (GameController.shotController.isOpponentReady)
            //{
            //    if (BallPoolGameLogic.playMode == BallPool.PlayMode.OnLine)
            //    {
            //        if (TriggerHome != null)                                                                                                                     //Photon Removal
            //        {
            //            TriggerHome();
            //            if (PhotonNetwork.NetworkClientState == ClientState.Joined)
            //            {
            //                ApiAndRoomManager._instance.WinnerLossChallenge(NetworkManager.opponentPlayer.userId.ToString());
            //                //print("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);

            //                PhotonNetwork.LeaveRoom();
            //            }
            //        }
            //    }
            //}
        }
        ReturnToHome(); //RAR
    }
    public void ToggleCameraView()
    {
        Camera2DView.enabled = !is3DMode;
        TableViewCamera.enabled = is3DMode;
        ToggleCameraText.text = is3DMode ? "2D" : "3D";
        InputOutput.usedCamera = is3DMode ? CueViewCamera : Camera2DView;
    }
}

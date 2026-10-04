using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
public class HudMenuManager : MonoBehaviour
{
    public static HudMenuManager instance;

    public static float inputSensitivity = 1;
    [FormerlySerializedAs("gameOverMenu")] public GameObject gameOverPanel;
    [FormerlySerializedAs("wrongWay")] public GameObject wrongWayIndicator;
    [FormerlySerializedAs("LapCompleteGo")] public GameObject lapCompletionNotification;
    [FormerlySerializedAs("lapText")] public Text[] lapDisplayText;
    [FormerlySerializedAs("PositionText")] public Text[] positionDisplayText;

    [FormerlySerializedAs("loading")] public GameObject loadingScreen;
    [FormerlySerializedAs("racinUI")] public GameObject racingUI;
    [FormerlySerializedAs("nitrosCount")] public Text nitroCountText;
    [FormerlySerializedAs("CameraTutorial")] public GameObject cameraTutorialPanel;

    [FormerlySerializedAs("backCamImg")] public Image backCameraImage;
    [FormerlySerializedAs("frontCamImg")] public Image frontCameraImage;
    [FormerlySerializedAs("orbitCamImg")] public Image orbitCameraImage;

    [FormerlySerializedAs("backCamSprite")] public Sprite[] backCameraSprites;
    [FormerlySerializedAs("frontCamSprite")] public Sprite[] frontCameraSprites;
    [FormerlySerializedAs("orbitCamSprite")] public Sprite[] orbitCameraSprites;

    [FormerlySerializedAs("jumpSound")] public GameObject jumpSoundEffect;
    [FormerlySerializedAs("jumpImag")] public GameObject jumpImage;
    [FormerlySerializedAs("isJumpMessageEnabled")] public bool isJumpMessageVisible = false;



    void Awake()
    {
        instance = this;
        updateUIContent();
    }

    // public void refreshContent()
    public void updateUIContent()
    {
        if (!Tutorials.isTutorialActive)
        {
            nitroCountText.text = "x" + PlayerDataController.instance.playerStats.NitrosCount;
        }
    }

    // public void setCamera(int camId)
    public void switchCamera(int camId)
    {
        switch (camId)
        {

            case 0://Back
                backCameraImage.sprite = backCameraSprites[1];
                frontCameraImage.sprite = frontCameraSprites[0];
                orbitCameraImage.sprite = orbitCameraSprites[0];

                break;
            case 1://Front

                backCameraImage.sprite = backCameraSprites[0];
                frontCameraImage.sprite = frontCameraSprites[1];
                orbitCameraImage.sprite = orbitCameraSprites[0];

                break;
            case 2://Orbit
                backCameraImage.sprite = backCameraSprites[0];
                frontCameraImage.sprite = frontCameraSprites[0];
                orbitCameraImage.sprite = orbitCameraSprites[1];

                break;

        }

        PlayerCameraManager.Instance.SwitchToCamera(camId);
    }
    //  public void GameOver(){
    public void endGame()
    {
        gameOverPanel.SetActive(true);
    }

    //	public void WrongWay(){
    public void showWrongWayWarning()
    {

        //
        if (MConstants.CurrentLevelNumber != 6)
        {
            wrongWayIndicator.SetActive(true);
        }
        //

        // wrongWay.SetActive (true);
    }

    //	public void RightWay(){
    public void clearWrongWayWarning()
    {
        wrongWayIndicator.SetActive(false);
    }

    //	public void LapComplete(){
    public void notifyLapCompletion()
    {
        lapCompletionNotification.SetActive(true);
        Invoke("LapCompleteRemove", 2);
    }

    //public void LapCompleteRemove(){
    public void removeLapCompletionNotification()
    {
        lapCompletionNotification.SetActive(false);
    }

    // public void setLaps(int currentLap,int TotalLaps)
    public void updateLapProgress(int currentLap, int TotalLaps)
    {
        if (currentLap > TotalLaps)
        {
            currentLap = TotalLaps;
        }
        for (int i = 0; i < lapDisplayText.Length; i++)
            lapDisplayText[i].text = "LAP: " + currentLap + "/" + TotalLaps;

    }

    // public void setPos(int currentPOs, int TotalPlayer)
    public void updatePosition(int currentPOs, int TotalPlayer)
    {
        for (int i = 0; i < positionDisplayText.Length; i++)
            positionDisplayText[i].text = "POS: " + currentPOs + "/" + TotalPlayer;

    }

}

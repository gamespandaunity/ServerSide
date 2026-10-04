namespace CarRace 
{
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class CarController : MonoBehaviour
{

    protected RCC_CarControllerV3 carController;
    [HideInInspector] protected int lapCount = 0;
    [HideInInspector] protected int totalLaps = 0;
    [HideInInspector] protected int totalPlayers = 0;
    [HideInInspector] protected bool hasCrossedMiddleCheckPoint = false;
    [SerializeField] public byte playerID;
    [HideInInspector] protected string playerName;
    protected GameObject playerPositionUI;
    TextMeshProUGUI positionUIText;
    protected TextMeshProUGUI positionNameText;
    protected virtual void Start()
    {
        carController = GetComponent<RCC_CarControllerV3>();
        SpawnPositionUI();
        lapCount = 0;
        totalLaps = 2;
    }
    private void Update()
    {
        if (RCC_SceneManager.Instance.activeMainCamera)
        {
            playerPositionUI.transform.LookAt(RCC_SceneManager.Instance.activeMainCamera.transform);
            playerPositionUI.transform.rotation = Quaternion.Euler(playerPositionUI.transform.eulerAngles.x, playerPositionUI.transform.eulerAngles.y + 180f, playerPositionUI.transform.eulerAngles.z);
        }
    }
    protected void OnEnable()
    {
        EventManager.OnGameStartEvent += OnGameStart;
        EventManager.OnGameFinishEvent += OnGameFinish;
    }
    protected void OnDisable()
    {
        EventManager.OnGameStartEvent -= OnGameStart;
        EventManager.OnGameFinishEvent -= OnGameFinish;
    }
    
    void SpawnPositionUI()
    {
        playerPositionUI = Instantiate(Resources.Load("PlayerPositionUI", typeof(GameObject))) as GameObject;
        playerPositionUI.transform.SetParent(transform, false);
        playerPositionUI.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        playerPositionUI.transform.localScale = Vector3.one;
        positionUIText = playerPositionUI.transform.Find("playerPositionText").transform.GetComponent<TextMeshProUGUI>();
        positionNameText = playerPositionUI.transform.Find("playerNameText").transform.GetComponent<TextMeshProUGUI>();


        GameObject positionIndicator;
        if(carController.externalController)
            positionIndicator = Instantiate(Resources.Load("MMAI", typeof(GameObject))) as GameObject;
        else
            positionIndicator = Instantiate(Resources.Load("MMP", typeof(GameObject))) as GameObject;

        positionIndicator.transform.SetParent(transform, false);
        positionIndicator.transform.localPosition = new Vector3(0f, 10f, 0f);
        positionIndicator.transform.localScale = Vector3.one;
        positionIndicator.transform.localEulerAngles = new Vector3(-90, 0 , 0);
    }
    public virtual void UpdatePositions(int pos)
    {
       positionUIText.text = pos.ToString();
    }
    protected virtual void OnGameStart(int totalLaps, int totalPlayers)
    {
        carController.SetCanControl(true);
        hasCrossedMiddleCheckPoint = false;
        this.totalLaps = totalLaps;
        this.totalPlayers = totalPlayers;
    }

    protected void OnGameFinish()
    {
        //PlayerPositionSystem._Instance.DeregisterPlayer(playerID);
        carController.SetCanControl(false);
    }
    protected virtual void LapFinished()
    {
        lapCount += 1;
        PlayerPositionSystem._Instance.PlayerLapComplete(playerID, lapCount);
        //if (lapCount == totalLaps)
        //    carController.SetCanControl(false);
    }
}

}
namespace CarRace 
{
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(CheckWrongDirection))]
[RequireComponent(typeof(CarModification))]
public class UserCarController : CarController
{
    CheckWrongDirection checkWrongDirection;
    [HideInInspector] public TextMeshProUGUI lapText;
    [HideInInspector] public TextMeshProUGUI posText;
    protected override void Start()
    {
        base.Start();
        playerPositionUI.SetActive(false);
        checkWrongDirection = GetComponent<CheckWrongDirection>();
        GetComponent<CarModification>().ChangeColor(UserData.GetCarColor(PlayerPrefs.GetInt(PPConst.SavedCar)));
        (staticVariables.UserProfiledata.user.first_name+" "+staticVariables.UserProfiledata.user.last_name).Show("Full Name");
        GetComponentInChildren<TextMeshProUGUI>().text = staticVariables.UserProfiledata.user.first_name+" "+staticVariables.UserProfiledata.user.last_name;
    }
    private void OnTriggerExit(Collider collider)
    {
        if (collider.CompareTag("DirectionPoints"))
        {
            if (!collider.GetComponent<WayPoint>().IsCarPassed()) // if the check point was already passed, then do not increment the WayPoint Index
            {
                checkWrongDirection.OnCheckpointPassed();
                PlayerPositionSystem._Instance.PlayerCrossedWaypoint(playerID);
            }
        }
        else if (collider.CompareTag("Middle"))
        {
            hasCrossedMiddleCheckPoint = true;
            EventManager.OnHalfLapCompleteEvent.Invoke();
        }
        else if (collider.CompareTag("Finish"))
        {
            if (hasCrossedMiddleCheckPoint)
            {
                hasCrossedMiddleCheckPoint = false;
                LapFinished();
            }
        }
    }

    protected override void OnGameStart(int totalLaps, int totalPlayers)
    {
        PlayerPositionSystem._Instance.RegisterPlayer(new PlayerDataForPositionSystem(playerID, transform, PlayerPrefs.GetString(PPConst.UserName)), this);
        base.OnGameStart(totalLaps, totalPlayers);
        lapText.text = "Laps " + (lapCount + 1) + "/" + totalLaps;
    }
    public override void UpdatePositions(int pos)
    {
        posText.text = "Pos" + pos + "/" + totalPlayers;
    }
    protected override void LapFinished()
    {
        base.LapFinished();
        EventManager.OnLapFinishEvent.Invoke();
        lapText.text = "Laps " + (lapCount + 1) + "/" + totalLaps;
        if (lapCount == totalLaps)
        {
            GetComponent<RCC_CarControllerV3>().SetCanControl(false);
            EventManager.OnGameFinishEvent.Invoke();
            EventManager.OnShowFinishCinematics.Invoke();
        }

    }
}

}
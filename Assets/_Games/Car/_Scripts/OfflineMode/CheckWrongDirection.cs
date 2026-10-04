namespace CarRace 
{
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckWrongDirection : MonoBehaviour
{
    private float wrongDirectionThreshold = -0.75f; // Adjust this value to control the sensitivity of the wrong direction detection
    [HideInInspector] public List<Transform> wrongDirectionCheckpoints; // A list of GameObjects representing checkpoints along the track
    [HideInInspector] public GameObject wrongWayPopUp;
    private int currentCheckpointIndex = 0;
    bool isRaceFinished;
    private void OnEnable()
    {
        EventManager.OnGameStartEvent += OnGameStart;
        EventManager.OnGameFinishEvent += OnGameFinish;
    }

    private void OnDisable()
    {
        EventManager.OnGameStartEvent -= OnGameStart;
        EventManager.OnGameFinishEvent -= OnGameFinish;
    }

    private void OnGameFinish()
    {
        isRaceFinished = true;
        StopCoroutine(CheckDirection());
    }

    private void OnGameStart(int totalLaps, int totalPlayers)
    {
        isRaceFinished = false;
        StartCoroutine(CheckDirection());
    }

    IEnumerator CheckDirection()
    {
        while (!isRaceFinished)
        {
            yield return new WaitForSeconds(0.07f);
            if (currentCheckpointIndex < wrongDirectionCheckpoints.Count - 1)
            {
                Vector3 currentCheckpointToNext = wrongDirectionCheckpoints[currentCheckpointIndex + 1].position - wrongDirectionCheckpoints[currentCheckpointIndex].position;
                Vector3 playerForward = transform.forward;

                float directionDotProduct = Vector3.Dot(playerForward.normalized, currentCheckpointToNext.normalized);

                wrongWayPopUp.SetActive(directionDotProduct < wrongDirectionThreshold);
                EventManager.OnPositionUpdateEvent.Invoke();
            }
        }
    }

    // Call this method when the player passes a checkpoint
    public void OnCheckpointPassed()
    {

        currentCheckpointIndex = (currentCheckpointIndex + 1) % wrongDirectionCheckpoints.Count;

    }
}

}
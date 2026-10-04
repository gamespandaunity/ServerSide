namespace CarRace 
{
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 

public class CheckWrongDirection_M : MonoBehaviour
{
    private float wrongDirectionThreshold = -0.75f; // Adjust this value to control the sensitivity of the wrong direction detection
    [HideInInspector] public List<Transform> wrongDirectionCheckpoints; // A list of GameObjects representing checkpoints along the track
    [HideInInspector] public GameObject wrongWayPopUp;
    public int currentCheckpointIndex = 0;
    bool isRaceFinished = false;

    private void Start()
    {
        StartCoroutine(CheckDirection());
    }
    IEnumerator CheckDirection()
    {
        while (!isRaceFinished)
        {
            yield return new WaitForSeconds(0.1f);
            if (currentCheckpointIndex < wrongDirectionCheckpoints.Count - 1)
            {
                Vector3 currentCheckpointToNext = wrongDirectionCheckpoints[currentCheckpointIndex + 1].position - wrongDirectionCheckpoints[currentCheckpointIndex].position;
                Vector3 playerForward = transform.forward;

                float directionDotProduct = Vector3.Dot(playerForward.normalized, currentCheckpointToNext.normalized);

                wrongWayPopUp.SetActive(directionDotProduct < wrongDirectionThreshold);
                //EventManager.OnPositionUpdateEvent.Invoke();
            }
        }
    }

    public void GetReferences(List<Transform> checkpoints, GameObject popUp)
    {
        wrongDirectionCheckpoints = checkpoints;
        wrongWayPopUp = popUp;
    }

    // Call this method when the player passes a checkpoint
    public void OnCheckpointPassed()
    {

        currentCheckpointIndex = (currentCheckpointIndex + 1) % wrongDirectionCheckpoints.Count;

    }
    public Transform GetPlayersLastCheckPoint()
    {
        //if (GetComponent<PhotonView>().IsMine)
        return wrongDirectionCheckpoints[currentCheckpointIndex - 1];
        //else
        //    return null;
    }
}

}
using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

public class CheckPoint : MonoBehaviour {
    [FormerlySerializedAs("Id")] public int checkpointId;
    [FormerlySerializedAs("isStartingPoint")] public bool isStartPoint;
    [FormerlySerializedAs("ResetPoint")] public GameObject resetLocation;

    bool isGoingWrongWay = false;
	void Start(){
		//CheckPointController.instance.LastId = Id;
		//ResetPoint = gameObject.transform.GetChild (0).gameObject;
		//gameObject.GetComponent<MeshRenderer>().enabled = false;
	}

	void OnTriggerEnter(Collider other){
		if(other.gameObject.CompareTag("PlayerCollider") && MConstants.CurrentGameMode != MConstants.GAME_MODES.MULTI_PLAYER){
			CheckPointController.instance.lastCheckpointReached = resetLocation;
			//Debug.Log ("Current Id "+ Id+ "Last ID "+CheckPointController.instance.LastId  );
			if (!isStartPoint && CheckPointController.instance.lastCheckpointId > checkpointId && !isGoingWrongWay) {
				CheckPointController.instance.lastCheckpointId = checkpointId;
				//Debug.Log("CheckPoint.CS: Wrong Way Detected");
				HudMenuManager.instance.showWrongWayWarning ();
				isGoingWrongWay = true;
				Invoke ("StopWrongWayWarning", 1.5f);
			} else if(!isGoingWrongWay) {
				HudMenuManager.instance.clearWrongWayWarning ();
				//Debug.Log("Right Way in : CheckPoints Script");
				CheckPointController.instance.lastCheckpointId = checkpointId;
			}
		}
	}

	void StopWrongWayWarning(){
		isGoingWrongWay = false;

	}
}

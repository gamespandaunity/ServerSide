using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

public class CheckPointController : MonoBehaviour {
	public static CheckPointController instance;
    [FormerlySerializedAs("lastKnownCheckPoint")] public GameObject lastCheckpointReached;
    [FormerlySerializedAs("LastId")] public int lastCheckpointId;


    // Use this for initialization
    void Awake () {
		instance = this;
	}

}

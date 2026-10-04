using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class ObjectRepositioner : MonoBehaviour {
    [FormerlySerializedAs("enemy")] public GameObject currentEnemy;

    // Use this for initialization
    void Start () {
		//gameObject.SetActive (false);
	}

}

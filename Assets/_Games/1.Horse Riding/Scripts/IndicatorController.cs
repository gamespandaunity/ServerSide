using UnityEngine;
using System.Collections;

public class IndicatorController : MonoBehaviour {

	// Use this for initialization
	void Start () {
		if (PlayerDataController.instance != null && PlayerDataController.instance.playerStats.CurrentMode == 1) {
			gameObject.SetActive (false);
		}
	}

}

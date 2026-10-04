using UnityEngine;
using System.Collections;

public class OnBack : MonoBehaviour {
	public GameObject back;
	bool isStartCalled = false;
	void Start(){
		isStartCalled = true;
	}
	void OnEnable(){
		back.SetActive (false);
		//if(isStartCalled){
		//	AdsManager.instance.showChartBoostInterstial ();
		//}
	}

	void OnDisable(){
		back.SetActive (true);
	}
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown (KeyCode.Escape)) {
			Application.Quit();
		}
	}
}

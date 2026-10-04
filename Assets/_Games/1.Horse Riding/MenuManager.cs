using UnityEngine;
using System.Collections;
public class MenuManager : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape)) 
			Application.Quit(); 
	}

	public void playCarScene(){
		Application.LoadLevel("Race_Track_01");

	}

	public void playBikeScene(){
		Application.LoadLevel("Race_Track_02-2");

	}
}

using UnityEngine;
using System.Collections;

public class FieldOfViewWithTime : MonoBehaviour {
	public float StartFielOfView=60;
	public Camera camera;
	 float currentFieldOfView=60;
    public int mutiFactor = 1;
	// Use this for initialization
	void OnEnable () {
		currentFieldOfView = StartFielOfView;
		camera.fieldOfView = currentFieldOfView;

	}
	
	// Update is called once per frame
	void FixedUpdate () {
		currentFieldOfView += Time.deltaTime * mutiFactor*10;
		if(currentFieldOfView>60){
			currentFieldOfView = 60;
            //mutiFactor = -mutiFactor;

        }

        if (currentFieldOfView < 20)
        {
            currentFieldOfView = 20;
            //mutiFactor = -mutiFactor;

        }
        camera.fieldOfView = currentFieldOfView;

	}
}

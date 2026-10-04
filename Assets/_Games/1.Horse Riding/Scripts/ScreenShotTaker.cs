using UnityEngine;
using System.Collections;

public class ScreenShotTaker : MonoBehaviour {


	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown (KeyCode.P)) {
			Time.timeScale = 0.0001f;
			//Application.CaptureScreenshot(Application.persistentDataPath + "Screenshot.png",1);

		}else if (Input.GetKeyDown (KeyCode.R)) {
			Time.timeScale = 1;

			//Application.CaptureScreenshot(Application.persistentDataPath + "Screenshot.png",1);

		}else if (Input.GetKeyDown (KeyCode.S)) {
            //Debug.Log(ScreenShotName());
			ScreenCapture.CaptureScreenshot(ScreenShotName(),1);

		}

	}

	public static string ScreenShotName() {
		return Application.persistentDataPath+
			System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")+".png";
	}


}

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class BlinkText : MonoBehaviour {
	public Text flashingText;
	void OnEnable(){
		StartCoroutine (BlinkTextE());
	}
	public IEnumerator BlinkTextE(){
		//blink it forever. You can set a terminating condition depending upon your requirement
		while(true){
			//set the Text's text to blank
			flashingText.enabled = true;
			//display blank text for 0.5 seconds
			yield return new WaitForSeconds(.3f);
			//display “I AM FLASHING TEXT” for the next 0.5 seconds
			flashingText.enabled = false;
			yield return new WaitForSeconds(.3f);
		}
	}
}

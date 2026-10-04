using UnityEngine;
using System.Collections;

public class ScaleSample : MonoBehaviour {
	void OnEnable(){
		iTween.ScaleFrom(gameObject, iTween.Hash("x", 0.5f, "y", 0.5f,"easeType", "easeInOutExpo","time",1.5f));
	}
}

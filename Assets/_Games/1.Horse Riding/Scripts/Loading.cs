using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Loading : MonoBehaviour {
    public List<string> instructions;
    public UITextTypeWriter textTypeWriter;
   
	// Use this for initialization
	void OnEnable () {
        textTypeWriter.txt.text = instructions[Random.Range(0, instructions.Count)];
        textTypeWriter.StartTypeWriter();
    }
	
	
}

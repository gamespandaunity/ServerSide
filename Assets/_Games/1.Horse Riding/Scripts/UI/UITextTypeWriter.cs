using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// attach to UI Text component (with the full text already there)

public class UITextTypeWriter : MonoBehaviour
{
    public Text txt;

	string story;
	public AudioSource typeWriterSound;
    public bool isAutoPlay;
	void OnEnable () 
	{
		//txt = GetComponent<Text> ();
        if (isAutoPlay)
        {
            StartTypeWriter();
        }
		
	}
    public void StartTypeWriter()
    {
        story = txt.text;
        txt.text = "";
        typeWriterSound.Play();
        // TODO: add optional delay when to start
        StartCoroutine("PlayText");
    }

	IEnumerator PlayText()
	{
		foreach (char c in story) 
		{
			txt.text += c;
			yield return new WaitForSeconds (0.03f);
		}

		typeWriterSound.Stop ();
	}

}

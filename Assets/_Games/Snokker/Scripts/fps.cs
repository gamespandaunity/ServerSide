using UnityEngine;
using UnityEngine.UI;

public class fps : MonoBehaviour
{
	private float updateInterval = 0.5f;

	private float accum;

	private float frames;

	private float timeleft;

	private Text textComp;

	public static string prefixForDebug = string.Empty;

	private void Start()
	{
		timeleft = updateInterval;
		textComp = GetComponent<Text>();
	}

	private void Update()
	{
		timeleft -= Time.deltaTime;
		accum += Time.timeScale / Time.deltaTime;
		frames += 1f;
		if (timeleft <= 0f)
		{
			textComp.text = prefixForDebug + "  " + (accum / frames).ToString("f2");
			timeleft = updateInterval;
			accum = 0f;
			frames = 0f;
		}
	}
}

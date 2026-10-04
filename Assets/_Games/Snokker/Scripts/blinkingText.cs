using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class blinkingText : MonoBehaviour
{
	private CanvasGroup canvasGroupComponent;

	private void Awake()
	{
		canvasGroupComponent = GetComponent<CanvasGroup>();
	}

	private void Update()
	{
		canvasGroupComponent.alpha = Mathf.PingPong(Time.realtimeSinceStartup * 2f, 0.5f) + 0.5f;
	}
}

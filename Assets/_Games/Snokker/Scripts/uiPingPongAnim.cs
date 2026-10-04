using UnityEngine;

public class uiPingPongAnim : MonoBehaviour
{
	private RectTransform rectTransform;

	private Vector2 startPos;

	public bool isFlipped;

	public float speedVal = 30f;

	public float distanceVal = 10f;

	public bool dirX;

	public bool dirY;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
		startPos = rectTransform.anchoredPosition;
	}

	private void Update()
	{
		if (dirX)
		{
			if (isFlipped)
			{
				rectTransform.SetAnchoredPositionX(startPos.x + (Mathf.PingPong(Time.realtimeSinceStartup * speedVal, distanceVal) - distanceVal / 2f));
			}
			else
			{
				rectTransform.SetAnchoredPositionX(startPos.x - (Mathf.PingPong(Time.realtimeSinceStartup * speedVal, distanceVal) - distanceVal / 2f));
			}
		}
		if (dirY)
		{
			if (isFlipped)
			{
				rectTransform.SetAnchoredPositionY(startPos.y + (Mathf.PingPong(Time.realtimeSinceStartup * speedVal, distanceVal) - distanceVal / 2f));
			}
			else
			{
				rectTransform.SetAnchoredPositionY(startPos.y - (Mathf.PingPong(Time.realtimeSinceStartup * speedVal, distanceVal) - distanceVal / 2f));
			}
		}
	}
}

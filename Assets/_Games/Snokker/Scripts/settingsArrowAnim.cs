using UnityEngine;

public class settingsArrowAnim : MonoBehaviour
{
	private RectTransform rectTransform;

	public bool isRight;

	private void Awake()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	private void Update()
	{
		Vector3 vector = rectTransform.anchoredPosition;
		if (isRight)
		{
			vector.x = Mathf.PingPong(Time.realtimeSinceStartup * 30f, 10f);
			rectTransform.anchoredPosition = vector;
		}
		else
		{
			vector.x = 50f - Mathf.PingPong(Time.realtimeSinceStartup * 30f, 10f);
			rectTransform.anchoredPosition = vector;
		}
	}
}

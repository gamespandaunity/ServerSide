using UnityEngine;

public class uiAnimatorPowerMeter : MonoBehaviour
{
	private RectTransform rectTransform;

	private CanvasGroup thisCanvasGroup;

	public static float targetPosX;

	private float animValue = 1f;

	private float animVel;

	public static float animTarget = 1f;

	private void Start()
	{
		rectTransform = GetComponent<RectTransform>();
		thisCanvasGroup = GetComponent<CanvasGroup>();
		targetPosX = rectTransform.anchoredPosition.x;
		rectTransform.anchoredPosition = new Vector2(targetPosX - animValue * targetPosX, rectTransform.anchoredPosition.y);
		thisCanvasGroup.alpha = 1f - animValue;
	}

	private void Update()
	{
		animValue = Mathf.SmoothDamp(animValue, animTarget, ref animVel, 0.15f, float.PositiveInfinity, mainScript.deltaTimeCustom);
		rectTransform.anchoredPosition = new Vector2(targetPosX - animValue * (targetPosX * 1.1f), rectTransform.anchoredPosition.y);
		thisCanvasGroup.alpha = 1f - animValue;
	}
}

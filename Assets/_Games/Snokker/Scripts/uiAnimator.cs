using UnityEngine;

public class uiAnimator : MonoBehaviour
{
	private RectTransform rectTransform;

	private float targetPosX;

	private float targetPosY;

	public bool animX;

	public bool animY;

	public bool animScaleX;

	public bool animScaleY;

	public bool isGameName;

	public bool animAlpha;

	private CanvasGroup canvasGroupComponent;

	private Vector3 tempAnimScaleVal;

	private Vector2 tempAnimPosVal;

	private void Start()
	{
		rectTransform = GetComponent<RectTransform>();
		if (animAlpha)
		{
			canvasGroupComponent = GetComponent<CanvasGroup>();
			canvasGroupComponent.alpha = 1f - mainScript.btnAnimValue;
		}
		if (animScaleX || animScaleY)
		{
			tempAnimScaleVal = rectTransform.localScale;
			if (animScaleX)
			{
				tempAnimScaleVal.x = 1f - mainScript.btnAnimValue;
			}
			if (animScaleY)
			{
				tempAnimScaleVal.y = 1f - mainScript.btnAnimValue;
			}
			rectTransform.localScale = tempAnimScaleVal;
		}
		if (isGameName)
		{
			targetPosX = rectTransform.anchoredPosition.x;
			tempAnimPosVal = rectTransform.anchoredPosition;
			tempAnimPosVal.x = targetPosX - mainScript.gameNamePos * targetPosX;
			rectTransform.anchoredPosition = tempAnimPosVal;
			return;
		}
		if (animX)
		{
			targetPosX = rectTransform.anchoredPosition.x;
			tempAnimPosVal = rectTransform.anchoredPosition;
			tempAnimPosVal.x = targetPosX - mainScript.btnAnimValue * targetPosX;
			rectTransform.anchoredPosition = tempAnimPosVal;
		}
		if (animY)
		{
			targetPosY = rectTransform.anchoredPosition.y;
			tempAnimPosVal = rectTransform.anchoredPosition;
			tempAnimPosVal.y = targetPosY - mainScript.btnAnimValue * targetPosY;
			rectTransform.anchoredPosition = tempAnimPosVal;
		}
	}

	private void Update()
	{
		if (!mainScript.bAnimateGui)
		{
			return;
		}
		if (animAlpha)
		{
			canvasGroupComponent.alpha = 1f - mainScript.btnAnimValue;
		}
		if (animScaleX || animScaleY)
		{
			tempAnimScaleVal = rectTransform.localScale;
			if (animScaleX)
			{
				tempAnimScaleVal.x = 1f - mainScript.btnAnimValue;
			}
			if (animScaleY)
			{
				tempAnimScaleVal.y = 1f - mainScript.btnAnimValue;
			}
			rectTransform.localScale = tempAnimScaleVal;
		}
		if (isGameName)
		{
			tempAnimPosVal = rectTransform.anchoredPosition;
			tempAnimPosVal.x = targetPosX - mainScript.gameNamePos * (targetPosX * 1.1f);
			rectTransform.anchoredPosition = tempAnimPosVal;
			return;
		}
		if (animX)
		{
			tempAnimPosVal = rectTransform.anchoredPosition;
			tempAnimPosVal.x = targetPosX - mainScript.btnAnimValue * (targetPosX * 1.1f);
			rectTransform.anchoredPosition = tempAnimPosVal;
		}
		if (animY)
		{
			tempAnimPosVal = rectTransform.anchoredPosition;
			tempAnimPosVal.y = targetPosY - mainScript.btnAnimValue * (targetPosY * 1.1f);
			rectTransform.anchoredPosition = tempAnimPosVal;
		}
	}
}

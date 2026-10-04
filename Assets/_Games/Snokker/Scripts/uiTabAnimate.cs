using UnityEngine;

public class uiTabAnimate : MonoBehaviour
{
	public bool forGroup;

	private Transform thisTransform;

	private CanvasGroup canvasGroupComponent;

	private float comingAnimValue;

	private float comingAnimVel;

	private float comingAnimTarget = 1f;

	private float comingAnimTime = 0.1f;

	private void Awake()
	{
		thisTransform = base.transform;
		canvasGroupComponent = GetComponent<CanvasGroup>();
	}

	private void OnEnable()
	{
		comingAnimValue = 0f;
		comingAnimTarget = 1f;
		if (forGroup)
		{
			canvasGroupComponent.alpha = comingAnimValue;
			comingAnimTime = 0.3f;
			return;
		}
		Vector3 localScale = thisTransform.localScale;
		localScale.x = comingAnimValue;
		thisTransform.localScale = localScale;
		comingAnimTime = 0.1f;
	}

	private void Update()
	{
		comingAnimValue = Mathf.SmoothDamp(comingAnimValue, comingAnimTarget, ref comingAnimVel, comingAnimTime, float.PositiveInfinity, mainScript.deltaTimeCustom);
		if (forGroup)
		{
			canvasGroupComponent.alpha = comingAnimValue;
			return;
		}
		Vector3 localScale = thisTransform.localScale;
		localScale.x = comingAnimValue;
		thisTransform.localScale = localScale;
	}
}

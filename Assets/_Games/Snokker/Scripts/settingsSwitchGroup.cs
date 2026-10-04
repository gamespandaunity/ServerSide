using UnityEngine;

public class settingsSwitchGroup : MonoBehaviour
{
	private RectTransform groupRectTrans1;

	private RectTransform groupRectTrans2;

	private GameObject leftArrowObj;

	private GameObject rightArrowObj;

	private float animValue;

	private float animVel;

	private float animTarget;

	private void Awake()
	{
		groupRectTrans1 = base.transform.Find("Group1").gameObject.GetComponent<RectTransform>();
		groupRectTrans2 = base.transform.Find("Group2").gameObject.GetComponent<RectTransform>();
		leftArrowObj = base.transform.Find("ArrowLeft").gameObject;
		rightArrowObj = base.transform.Find("ArrowRight").gameObject;
		leftArrowObj.SetActive(false);
		groupRectTrans2.anchoredPosition = new Vector2(1000f, groupRectTrans2.anchoredPosition.y);
	}

	private void Update()
	{
		animValue = Mathf.SmoothDamp(animValue, animTarget, ref animVel, 0.15f, float.PositiveInfinity, mainScript.deltaTimeCustom);
		groupRectTrans1.anchoredPosition = new Vector2(animValue * -1000f, groupRectTrans1.anchoredPosition.y);
		groupRectTrans2.anchoredPosition = new Vector2((1f - animValue) * 1000f, groupRectTrans2.anchoredPosition.y);
	}

	private void showGroup1()
	{
		animTarget = 0f;
		leftArrowObj.SetActive(false);
		rightArrowObj.SetActive(true);
	}

	private void showGroup2()
	{
		animTarget = 1f;
		leftArrowObj.SetActive(true);
		rightArrowObj.SetActive(false);
	}
}

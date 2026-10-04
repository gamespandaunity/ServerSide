using UnityEngine;
using UnityEngine.UI;

public class rateGameMsgScript : MonoBehaviour
{
	private GameObject bgGameObj;

	private RectTransform msgRectTransform;

	private CanvasGroup bgCanvasGroupComponent;

	private Vector3 tempAnimScaleVal;

	private float animValue;

	private float animTarget;

	private float animVel;

	private float animTime = 0.15f;

	private bool alreadyAskedToRate;

	private WWW wwwRateGameUrlData;

	private void Awake()
	{
		bgGameObj = base.transform.Find("BG").gameObject;
		msgRectTransform = base.transform.Find("BG/Box").GetComponent<RectTransform>();
		bgCanvasGroupComponent = base.transform.Find("BG").GetComponent<CanvasGroup>();
	}

	private void showMessage()
	{
		animValue = 0f;
		animTarget = 1f;
		tempAnimScaleVal = msgRectTransform.localScale;
		tempAnimScaleVal.x = animValue;
		tempAnimScaleVal.y = animValue;
		msgRectTransform.localScale = tempAnimScaleVal;
		bgCanvasGroupComponent.alpha = animValue;
		bgGameObj.SetActive(true);
		base.transform.Find("BG/Box/Message").GetComponent<Text>().text = "LOVE REAL POOL 3D?\nPLEASE RATE IT IN THE APP STORE!";
	}

	public void hideMessage()
	{
		animTarget = 0f;
	}

	public void disableMessage()
	{
		bgGameObj.SetActive(false);
	}

	private void Update()
	{
		if (bgGameObj.activeSelf)
		{
			animValue = Mathf.SmoothDamp(animValue, animTarget, ref animVel, animTime);
			tempAnimScaleVal = msgRectTransform.localScale;
			tempAnimScaleVal.x = animValue;
			tempAnimScaleVal.y = animValue;
			msgRectTransform.localScale = tempAnimScaleVal;
			bgCanvasGroupComponent.alpha = animValue;
			if (animVel < 0f && animValue < 0.05f)
			{
				bgGameObj.SetActive(false);
			}
		}
	}



	public void goToRateThisGame()
	{
		Application.OpenURL("market://details?id=com.eivaagames.RealPool3DFree");
	}
}

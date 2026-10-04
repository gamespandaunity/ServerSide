using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class logo : MonoBehaviour
{
	private bool loading;

	private Transform loadingRoundObjTransform;

	private Image loadingProgressImageComponenet;

	private AsyncOperation asyncLoading;

	private float loadingSmoothVel;

	private float loadingSmoothValue;

	private RectTransform logoRectTransform;

	private CanvasGroup blackFadeCanvasGroupComp;

	private float logoAnimValue;

	private float logoAnimVel;

	private float logoAnimTarget = 1f;

	private float logoAnimTime = 0.6f;

	private bool logoAnimScale = true;

	private float logoScaleExtraVal;

	private void Start()
	{
		Time.timeScale = 1f;
		Application.targetFrameRate = 60;
		loadingRoundObjTransform = GameObject.Find("Canvas/Parent/LoadingGroup/LoadingRound").transform;
		loadingProgressImageComponenet = GameObject.Find("Canvas/Parent/LoadingGroup/LoadingBack/LoadingProgress").GetComponent<Image>();
		logoRectTransform = GameObject.Find("Canvas/Parent/Logo/").GetComponent<RectTransform>();
		blackFadeCanvasGroupComp = GameObject.Find("Canvas/Parent/BlackFade").GetComponent<CanvasGroup>();
		Invoke("startLoading", 2.5f);
	}

	private void Update()
	{
		if (logoAnimScale)
		{
			logoRectTransform.localScale = new Vector3(0.75f + logoAnimValue / 4f, 0.75f + logoAnimValue / 4f, logoRectTransform.localScale.z);
		}
		if (logoAnimValue > 0.97f)
		{
			logoScaleExtraVal += 0.02f * Time.deltaTime;
			logoRectTransform.localScale = new Vector3(0.75f + logoAnimValue / 4f + logoScaleExtraVal, 0.75f + logoAnimValue / 4f + logoScaleExtraVal, logoRectTransform.localScale.z);
		}
		blackFadeCanvasGroupComp.alpha = 1f - logoAnimValue;
		logoAnimValue = Mathf.SmoothDamp(logoAnimValue, logoAnimTarget, ref logoAnimVel, logoAnimTime);
		Vector3 eulerAngles = loadingRoundObjTransform.eulerAngles;
		eulerAngles.z -= 350f * Time.deltaTime;
		loadingRoundObjTransform.eulerAngles = eulerAngles;
		if (loading)
		{
			loadingSmoothValue = Mathf.SmoothDamp(loadingSmoothValue, asyncLoading.progress * 100f / 100f, ref loadingSmoothVel, 0.6f);
			loadingProgressImageComponenet.fillAmount = loadingSmoothValue * 1.11f;
		}
	}

	private void startLoading()
	{
		loading = true;
		loadLevelActivate();
	}

	private void loadLevelActivate()
	{
		asyncLoading = SceneManager.LoadSceneAsync("1");
		asyncLoading.allowSceneActivation = false;
		InvokeRepeating("loadLevelInvoke", 0.1f, 0.05f);
	}

	private void loadLevelInvoke()
	{
		if ((double)(loadingSmoothValue * 1.11f) >= 0.998)
		{
			logoAnimTarget = 0f;
			logoAnimTime = 0.2f;
			logoAnimScale = false;
			loading = false;
			GameObject.Find("Canvas/Parent/LoadingGroup").SetActive(false);
			CancelInvoke("loadLevelInvoke");
			Invoke("activateLoadedScene", 0.7f);
		}
	}

	private void activateLoadedScene()
	{
		asyncLoading.allowSceneActivation = true;
	}
}

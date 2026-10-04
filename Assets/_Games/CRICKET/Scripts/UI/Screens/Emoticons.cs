using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Emoticons : MonoBehaviour
{
    [FormerlySerializedAs("smileyBtn")]
    public GameObject smileyButton;

    [FormerlySerializedAs("closeBtn")]
    public GameObject closeButton;

    [FormerlySerializedAs("posi")]
    public GameObject transitionPosition;

    private bool isInTransition;

	private float currentTransitionTime;

	private float totalTransitionDuration;

	private float transitionStartValue;

	private float transitionEndValue;

	private void Awake()
	{
		transitionPosition.SetActive(value: false);
		smileyButton.SetActive(value: true);
		closeButton.SetActive(value: false);
		isInTransition = false;
		totalTransitionDuration = 0.5f;
	}

	public void OpenEmoticons()
	{
		if (!isInTransition)
		{
			transitionPosition.SetActive(value: true);
			currentTransitionTime = 0f;
			isInTransition = true;
		}
	}

	public void CloseEmoticons()
	{
		if (!isInTransition)
		{
			transitionPosition.GetComponent<Animator>().SetTrigger("close");
			isInTransition = true;
			currentTransitionTime = 0f;
			StartCoroutine(StopAnimation());
		}
	}

	private IEnumerator StopAnimation()
	{
		yield return new WaitForSeconds(0.5f);
		transitionPosition.SetActive(value: false);
	}

	public void Update()
	{
		if (!isInTransition)
		{
			return;
		}
		currentTransitionTime += Time.deltaTime;
		if (currentTransitionTime > totalTransitionDuration)
		{
			isInTransition = false;
			if (smileyButton.activeSelf)
			{
				smileyButton.SetActive(value: false);
				closeButton.SetActive(value: true);
			}
			else
			{
				smileyButton.SetActive(value: true);
				closeButton.SetActive(value: false);
			}
		}
	}

	private float Linear(float t, float source, float destination, float duration)
	{
		return (destination - source) * t / duration + source;
	}
}

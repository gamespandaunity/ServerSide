using UnityEngine;

public class uiBobAnim : MonoBehaviour
{
	private RectTransform rectTransform;

	public bool animScale;

	public bool animRotate;

	public float scaleAmount = 0.2f;

	public float scaleSpeed = 5f;

	public float rotateAmount = 10f;

	public float rotateSpeed = 5f;

	private void Start()
	{
		rectTransform = GetComponent<RectTransform>();
	}

	private void Update()
	{
		if (animScale)
		{
			float num = Mathf.Sin(Time.time * scaleSpeed) * scaleAmount;
			rectTransform.localScale = new Vector2(1f + num, 1f + num);
		}
		if (animRotate)
		{
			rectTransform.SetLocalEulerAnglesZ(Mathf.Sin(Time.time * rotateSpeed) * rotateAmount);
		}
	}
}

using UnityEngine;

public class rotateObject : MonoBehaviour
{
	private Transform thisTransform;

	public Vector3 rotationDir;

	private void Start()
	{
		thisTransform = base.transform;
	}

	private void Update()
	{
		thisTransform.Rotate(rotationDir * 7f * Time.deltaTime);
		float num = 0.8f + Mathf.PingPong(Time.realtimeSinceStartup / 3f, 0.4f);
		thisTransform.localScale = new Vector3(num, num, thisTransform.localScale.z);
	}
}

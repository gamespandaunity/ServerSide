using UnityEngine;
using Cricket;

public class LeftCameraLookAt : Singleton<LeftCameraLookAt>
{
	public bool canLookAt;

	public Transform target;

	public float damping = 6f;

	public float _lerpTime;

	public bool canChangeFov;

	private void LateUpdate()
	{
		if (CONTROLLER.cameraType == 0 && (bool)target && canLookAt)
		{
            //Quaternion b = Quaternion.LookRotation(target.position - base.transform.position);
            Quaternion b = Quaternion.LookRotation(Singleton<GroundController>.instance.GetTempPos() - base.transform.position);
            base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, Time.deltaTime * damping);
		}
	}

	private void Start()
	{
		canLookAt = true;
		if ((bool)GetComponent<Rigidbody>())
		{
			GetComponent<Rigidbody>().freezeRotation = true;
		}
	}

}

using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class RightCameraFocus : Singleton<RightCameraFocus>
{
	[FormerlySerializedAs("canLookAt")]
	public bool IsLookAtEnabled;

	[FormerlySerializedAs("target")]
	public Transform TargetTransform;

	[FormerlySerializedAs("damping")]
	public float MovementDamping;

	[FormerlySerializedAs("_lerpTime")]
	public float LerpDuration;


	private void LateUpdate()
	{
        if (CONTROLLER.cameraType == 0 && (bool)TargetTransform && IsLookAtEnabled)
        {
            //Quaternion b = Quaternion.LookRotation(target.position - base.transform.position);
            Quaternion b = Quaternion.LookRotation(Singleton<GroundController>.instance.GetTempPos() - base.transform.position);
			base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, Time.deltaTime * MovementDamping);
			if (Singleton<GroundController>.instance.rightFieldCamera.enabled)
			{
				ChangeFOV();
			}
		}
	}

	private void Start()
	{
		IsLookAtEnabled = true;
		if ((bool)GetComponent<Rigidbody>())
		{
			GetComponent<Rigidbody>().freezeRotation = true;
		}
	}

	private void ChangeFOV()
	{
		if (CONTROLLER.cameraType == 0)
		{
			LerpDuration += Time.deltaTime;
			float t = LerpDuration / 6f;
			t = Mathf.SmoothStep(0f, 1f, t);
			t = SmootherStep(t);
			t *= t;
			t = t * t * t;
			GetComponent<Camera>().fieldOfView = Mathf.Lerp(60f, 20f, t);
		}
	}

	private float SmootherStep(float t)
	{
		return t * t * t * (t * (6f * t - 15f) + 10f);
	}
}

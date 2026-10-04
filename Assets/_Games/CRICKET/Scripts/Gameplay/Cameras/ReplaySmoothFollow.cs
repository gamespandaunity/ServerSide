using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class ReplaySmoothFollow : MonoBehaviour
{
	[FormerlySerializedAs("target")] public Transform FollowTarget;
	[FormerlySerializedAs("distance")] public float FollowDistance = 10f;
	[FormerlySerializedAs("height")] public float FollowHeight = 5f;
	[FormerlySerializedAs("heightDamping")] public float VerticalSmoothness = 2f;
	[FormerlySerializedAs("rotationDamping")] public float RotationSmoothness = 3f;

	private float desiredYaw;
	private float desiredPitch;
	private float currentYaw;
	private float currentPitch;
	private Quaternion currentRotation;
	private GroundController _groundControllerScript;

	protected void Awake()
	{
		_groundControllerScript = GameObject.Find("GroundController").GetComponent<GroundController>();
	}

	private void LateUpdate()
	{
		if ((bool)FollowTarget)
		{
			desiredYaw = FollowTarget.transform.eulerAngles.y;
            //wantedHeight = target.transform.position.y + height;
            desiredPitch = Singleton<GroundController>.instance.GetTempPos().y + FollowHeight;

            if (_groundControllerScript.restrictReplayCameraHeight && desiredPitch > 6f)
			{
				desiredPitch = 6f;
			}
			currentYaw = base.transform.eulerAngles.y;
			currentPitch = base.transform.position.y;
			currentYaw = Mathf.LerpAngle(currentYaw, desiredYaw, RotationSmoothness * Time.deltaTime);
			currentPitch = Mathf.Lerp(currentPitch, desiredPitch, VerticalSmoothness * Time.deltaTime);
			currentRotation = Quaternion.Euler(0f, currentYaw, 0f);
            //base.transform.position = target.transform.position;
            base.transform.position = Singleton<GroundController>.instance.GetTempPos();

            base.transform.position -= currentRotation * Vector3.forward * FollowDistance;
			base.transform.position = new Vector3(base.transform.position.x, currentPitch, base.transform.position.z);
			base.transform.LookAt(FollowTarget);
		}
	}
}

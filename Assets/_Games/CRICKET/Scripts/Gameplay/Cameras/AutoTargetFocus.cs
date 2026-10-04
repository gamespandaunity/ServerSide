using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class AutoTargetFocus : MonoBehaviour
{
	[FormerlySerializedAs("centerPoint")]
	public GameObject CameraCenterPoint;

	[FormerlySerializedAs("ball")]
	public GameObject TargetBall;

	[FormerlySerializedAs("cam")]
	public Camera MainCamera;

	[FormerlySerializedAs("maxDistance")]
	[SerializeField] 
	private float MaxCameraDistance = 60f;

	[FormerlySerializedAs("startFov")]
	[SerializeField]
	private float InitialFov = 48f;

	[FormerlySerializedAs("dampingFactor")]
	[SerializeField]
	private float CameraDampingFactor = 1.5f;


	private void Update()
	{
		if (CONTROLLER.cameraType == 1 && Singleton<GroundController>.instance.currentActionState > 2 && (Vector3.Distance(Singleton<GroundController>.instance.GetTempPos(), CameraCenterPoint.transform.position) < MaxCameraDistance))
        {
            MainCamera.fieldOfView = InitialFov - (Vector3.Distance(Singleton<GroundController>.instance.GetTempPos(), CameraCenterPoint.transform.position)/CameraDampingFactor);
        }
    }

	private float DistanceBetweenTwoGameObjects(GameObject go1, GameObject go2)
	{
		return Vector3.Distance(go1.transform.position, go2.transform.position);
	}
}

using UnityEngine;
using Cricket;
public class CameraBoundary : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (other.name == "Ball" && (Singleton<GroundController>.instance.leftFieldCamera.enabled || Singleton<GroundController>.instance.rightFieldCamera.enabled))
		{
			Singleton<RightSmoothFov>.instance.CanMove = false;
		}
	}
}

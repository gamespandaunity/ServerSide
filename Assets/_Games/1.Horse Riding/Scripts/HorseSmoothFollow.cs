using UnityEngine;
using UnityEngine.Serialization;

namespace UnityStandardAssets.Utility
{
	public class HorseSmoothFollow : MonoBehaviour
	{

        // The target we are following
        [FormerlySerializedAs("target")][SerializeField] public Transform followTarget;
        // The distance in the x-z plane to the target
        [FormerlySerializedAs("distance")][SerializeField] private float horizontalDistance = 10.0f;
        // the height we want the camera to be above the target
        [FormerlySerializedAs("height")][SerializeField] private float verticalOffset = 5.0f;

        [FormerlySerializedAs("rotationDamping")][SerializeField] private float rotationSmoothness;
        [FormerlySerializedAs("heightDamping")][SerializeField] private float heightSmoothness;


        // Use this for initialization
        void Start() { }

		// Update is called once per frame
		void LateUpdate()
		{
			// Early out if we don't have a target
			if (!followTarget)
				return;

			// Calculate the current rotation angles
			var wantedRotationAngle = followTarget.eulerAngles.y;
			var wantedHeight = followTarget.position.y + verticalOffset;

			var currentRotationAngle = transform.eulerAngles.y;
			var currentHeight = transform.position.y;

			// Damp the rotation around the y-axis
			currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationSmoothness * Time.deltaTime);

			// Damp the height
			currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightSmoothness * Time.deltaTime);

			// Convert the angle into a rotation
			var currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

			// Set the position of the camera on the x-z plane to:
			// distance meters behind the target
			transform.position = followTarget.position;
			transform.position -= currentRotation * Vector3.forward * horizontalDistance;

			// Set the height of the camera
			transform.position = new Vector3(transform.position.x ,currentHeight , transform.position.z);

			// Always look at the target
			transform.LookAt(followTarget);
		}
	}
}
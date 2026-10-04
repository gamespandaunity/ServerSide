namespace Cricket 
{
using UnityEngine;
    using UnityEngine.Serialization;

    [AddComponentMenu("Camera-Control/Smooth Look At")]
    public class GradualLookAt : MonoBehaviour
{
        [FormerlySerializedAs("canLookAt")]
        public static bool CanLookAtTarget;

        [FormerlySerializedAs("target")]
        public Transform LookTarget;

        [FormerlySerializedAs("damping")]
        public float LookDamping  = 6f;

        [FormerlySerializedAs("smooth")]
        public bool UseSmoothMovement  = true;


        private void LateUpdate()
	{
		if (!Singleton<GroundController>.instance.isBallOnBoundaryLine)
		{
			Quaternion b = Quaternion.LookRotation(LookTarget.position - base.transform.position);
			base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, Time.deltaTime * LookDamping );
		}
	}

	private void Start()
	{
		CanLookAtTarget = true;
		if ((bool)GetComponent<Rigidbody>())
		{
			GetComponent<Rigidbody>().freezeRotation = true;
		}
	}
}

}
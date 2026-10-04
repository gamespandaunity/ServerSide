using DG.Tweening;
using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class SmoothLookAtTwo : Singleton<SmoothLookAtTwo>
{
	[FormerlySerializedAs("target")]	public Transform Target;

	[FormerlySerializedAs("damping")]public float Damping = 1f;

	[FormerlySerializedAs("smooth")]public bool UseSmoothMovement = true;

	[FormerlySerializedAs("canMove")]public bool CanMove;

	private GroundController groundController;

	private Vector3 offsetFromTarget;

	private Vector3 lastTargetPosition;

	private Tweener cameraMoveTween;

	protected void Start()
	{
		groundController = GameObject.Find("GroundController").GetComponent<GroundController>();
		if ((bool)GetComponent<Rigidbody>())
		{
			GetComponent<Rigidbody>().freezeRotation = true;
		}
	}

	protected void LateUpdate()
	{
		if (!groundController.isBallOnBoundaryLine)
		{
			//Vector3 forward = target.position - base.transform.position;
			Target.position = Singleton<GroundController>.instance.GetTempPos();
            Vector3 forward = Target.position - base.transform.position;
            Quaternion b = Quaternion.LookRotation(forward);
			base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, Time.deltaTime * Damping);
			if (CanMove && !(lastTargetPosition == Singleton<GroundController>.instance.GetTempPos() + offsetFromTarget))
			{
                //tween.ChangeEndValue(target.position + offset, snapStartValue: true).Restart();
				cameraMoveTween.ChangeEndValue(Singleton<GroundController>.instance.GetTempPos() + offsetFromTarget, snapStartValue: true).Restart();
                lastTargetPosition = Target.position + offsetFromTarget;
                //targetLastPos = GroundController.tempPos + offset;
            }
		}
	}

	public void StartFollowTarget()
	{
		Target.transform.position = Singleton<GroundController>.instance.GetTempPos();
		offsetFromTarget = base.transform.position - Target.transform.position;
		////Debug.Log("OOOFSET : " + offset);
        //offset = base.transform.position - GroundController.tempPos;
		////Debug.Log("@@@@" + base.transform.position + " @ " + GroundController.tempPos);
        //tween = base.transform.DOMove(target.position + offset, 1f).SetAutoKill(autoKillOnCompletion: false);
        cameraMoveTween = base.transform.DOMove(Singleton<GroundController>.instance.GetTempPos() + offsetFromTarget, 1f).SetAutoKill(autoKillOnCompletion: false);
        CanMove = true;
	}

	public void Reset()
	{
		cameraMoveTween.Kill();
		CanMove = false;
	}
}

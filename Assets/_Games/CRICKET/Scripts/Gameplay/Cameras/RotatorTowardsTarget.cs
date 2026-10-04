using DG.Tweening;
using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class RotatorTowardsTarget : Singleton<RotatorTowardsTarget>
{
	[FormerlySerializedAs("target")] public Transform FollowTarget;
	[FormerlySerializedAs("damping")] public float MoveSmoothness = 1f;
	[FormerlySerializedAs("smooth")] public bool UseSmoothMovement = true;
	[FormerlySerializedAs("canMove")] public bool IsMovementEnabled;

	private Vector3 _positionOffset;
	private Vector3 _lastTargetPosition;
	private GroundController _groundController;
	private Tweener _positionTween;

	protected void Start()
	{
		_groundController = GameObject.Find("GroundController").GetComponent<GroundController>();
		if ((bool)GetComponent<Rigidbody>())
		{
			GetComponent<Rigidbody>().freezeRotation = true;
		}
	}

	protected void Update()
	{
		if (!_groundController.isBallOnBoundaryLine)
		{
			//Vector3 forward = target.position - base.transform.position;
			FollowTarget.position = Singleton<GroundController>.instance.GetTempPos();
            Vector3 forward = FollowTarget.position - base.transform.position;
            Quaternion b = Quaternion.LookRotation(forward);
			base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, Time.deltaTime * MoveSmoothness);
            //if (canMove && !(targetLastPos == target.position + offset))
            if (IsMovementEnabled && !(_lastTargetPosition == Singleton<GroundController>.instance.GetTempPos() + _positionOffset))
            {
                //tween.ChangeEndValue(target.position + offset, snapStartValue: true).Restart();
                _positionTween.ChangeEndValue(Singleton<GroundController>.instance.GetTempPos() + _positionOffset, snapStartValue: true).Restart();
                //targetLastPos = target.position + offset;
                _lastTargetPosition = Singleton<GroundController>.instance.GetTempPos() + _positionOffset;
            }
        }
	}

	public void StartFollowTarget()
	{
        //offset = base.transform.position - target.transform.position;
        _positionOffset = base.transform.position - Singleton<GroundController>.instance.GetTempPos();
        //tween = base.transform.DOMove(target.position + offset, 1f).SetAutoKill(autoKillOnCompletion: false);
        _positionTween = base.transform.DOMove(Singleton<GroundController>.instance.GetTempPos() + _positionOffset, 1f).SetAutoKill(autoKillOnCompletion: false);
        IsMovementEnabled = true;
	}

	public void Reset()
	{
		_positionTween.Kill();
		IsMovementEnabled = false;
	}
}

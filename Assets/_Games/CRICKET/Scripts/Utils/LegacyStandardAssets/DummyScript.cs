using DG.Tweening;
using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class DummyScript : Singleton<DummyScript>
{
    [FormerlySerializedAs("target")] public Transform TargetTransform;

    [FormerlySerializedAs("damping")] public float CameraDamping = 6f;

    [FormerlySerializedAs("smooth")] public bool IsSmoothMovement = true;

    private GroundController groundController;

    [FormerlySerializedAs("canMove")] public bool CanMove;

    private Vector3 CameraOffset;

    private Vector3 LastTargetPosition;

    private Tweener MovementTweener;

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
            Vector3 forward = TargetTransform.position - base.transform.position;
            Quaternion b = Quaternion.LookRotation(forward);
            base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, Time.deltaTime * CameraDamping);
            if (CanMove && !(LastTargetPosition == TargetTransform.position + CameraOffset))
            {
                MovementTweener.ChangeEndValue(TargetTransform.position + CameraOffset, snapStartValue: true).Restart();
                LastTargetPosition = TargetTransform.position + CameraOffset;
            }
        }
    }

    public void StartFollowTarget()
    {
        CameraOffset = base.transform.position - TargetTransform.transform.position;
        MovementTweener = base.transform.DOMove(TargetTransform.position + CameraOffset, 1f).SetAutoKill(autoKillOnCompletion: false);
        CanMove = true;
    }

    public void Reset()
    {
        MovementTweener.Kill();
        CanMove = false;
    }
}
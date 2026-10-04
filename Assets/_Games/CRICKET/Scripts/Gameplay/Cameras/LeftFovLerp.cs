using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class LeftFovLerp : Singleton<LeftFovLerp>
{
    [FormerlySerializedAs("FovStart")] public float StartingFOV  = 60f;
    [FormerlySerializedAs("BLC")] public GameObject BoundaryLookCamera;
    [FormerlySerializedAs("FovEnd")] public float EndingFOV  = 30f;
    [FormerlySerializedAs("TransitionTime")] public float FOVTransitionDuration  = 3.5f;
    [FormerlySerializedAs("canMove")] public bool CanMove;
    [FormerlySerializedAs("target")] public Transform Target;
    [FormerlySerializedAs("damping")] public float Damping  = 6f;
    [FormerlySerializedAs("lineCamera")] public RotatorTowardsTarget LineCamera;
    [FormerlySerializedAs("mainCam")] public Camera MainCamera;
    [FormerlySerializedAs("canRotate")] public bool CanRotate;
    [FormerlySerializedAs("fovOffset")] public float FOVOffset  = 5f;
    [FormerlySerializedAs("smoothSpeed")] public float SmoothMoveSpeed  = 0.125f;
    [FormerlySerializedAs("zoomSpeed")] public float ZoomSpeed  = 3f;
    [FormerlySerializedAs("offset")] public Vector3 Offset;
    [FormerlySerializedAs("rOffset")] public Vector3 RotationOffset;
    [FormerlySerializedAs("start")] public Vector3 StartPosition;
    [FormerlySerializedAs("end")] public Vector3 EndPosition;
    [FormerlySerializedAs("boundaryCam")] public Camera BoundaryCamera;
    [FormerlySerializedAs("canLookAt")] public bool CanLookAt;
    [FormerlySerializedAs("canMoveDown")] public bool CanMoveDown;
    [FormerlySerializedAs("boundaryCamEnable")] public bool IsBoundaryCameraEnabled;
    [FormerlySerializedAs("RotateAngle")] public float RotateAngle  = 270f;


    private float initialAngle  = 90f;
    private float currentFOV;
    private float lerpTimer;
    private float lerpDuration;
    private Camera internalCamera;
    private float targetFOV;
    private Camera secondaryCamera;
    private Vector3 targetPosition;
    private Vector3 smoothedPosition;
    private float positionLimit;
    private float yOffset;
    private float zOffset;
    private Vector3 cameraEndPosition;
    private Vector3 rawPosition;
    private float bounceFrame;
    private Vector3 lastTargetPosition;
    private Vector3 moveVelocity;
    private Vector3 moveDirection;
    private Vector3 intermediatePos1;
    private Vector3 intermediatePos2;
    private Tweener mainTween;
    private Tweener secondaryTween;

    private void Start()
    {
        if (CONTROLLER.cameraType == 0)
        {
            BoundaryCamera.enabled = false;
            //tween = base.transform.DOMove(target.position, 1f).SetAutoKill(autoKillOnCompletion: false);
            mainTween = base.transform.DOMove(Singleton<GroundController>.instance.GetTempPos(), 1f).SetAutoKill(autoKillOnCompletion: false);
            //targetLastPos = target.position;
            lastTargetPosition = Singleton<GroundController>.instance.GetTempPos();
            CanMoveDown = false;
            RotationOffset = new Vector3(90f, 0f, 0f);
            CanMove = true;
            CanLookAt = true;
            internalCamera = GetComponent<Camera>();
            secondaryCamera = GetComponent<Camera>();
            targetFOV = GetComponent<Camera>().fieldOfView;
            if ((bool)GetComponent<Rigidbody>())
            {
                GetComponent<Rigidbody>().freezeRotation = true;
            }
            positionLimit = 70f;
        }
    }

    private void Update()
    {
        if ((bool)Target && IsBoundaryCameraEnabled && bounceFrame < 70f && DistanceBetweenTwoVector2(Singleton<GroundController>.instance.groundCenterMarker, Target.gameObject) > 40f)
        {
            BoundaryLookCamera.GetComponent<Camera>().enabled = true;
            MainCamera.enabled = false;
        }
    }

   

    public float DistanceBetweenTwoVector2(GameObject go1, GameObject go2)
    {
        float num = go1.transform.position.x - go2.transform.position.x;
        float num2 = go1.transform.position.z - go2.transform.position.z;
        return Mathf.Sqrt(num * num + num2 * num2);
    }

    public void setBallAngle(float ballAngle)
    {
    }

    public void setCameraPosition(Vector3 pos, GameObject target)
    {
        if (!Singleton<GroundController>.instance.isReplayModeActive)
        {
            Offset = LineCamera.transform.position - LineCamera.FollowTarget.transform.position;
            LineCamera.transform.position = LineCamera.FollowTarget.position + Offset;
            LineCamera.StartFollowTarget();
            pos = new Vector3(pos.x + Mathf.Cos((RotateAngle  + initialAngle ) * ((float)Math.PI / 180f)) * 8f, 4f, pos.z + Mathf.Sin((RotateAngle  + initialAngle ) * ((float)Math.PI / 180f)) * 8f);
            BoundaryLookCamera.transform.position = pos;
            Invoke("EnableBoundaryCam", 0.5f);
        }
    }

    public void setCameraPositionWicket(Vector3 pos, GameObject target)
    {
        if (CONTROLLER.cameraType == 0)
        {
            pos = new Vector3(pos.x - Mathf.Cos((135f + initialAngle ) * ((float)Math.PI / 180f)) * 8f, 4f, pos.z - Mathf.Sin((135f + initialAngle ) * ((float)Math.PI / 180f)) * 8f);
            BoundaryLookCamera.transform.position = pos;
            Invoke("EnableBoundaryCam", 0.3f);
            return;
        }
        Offset = LineCamera.transform.position - LineCamera.FollowTarget.transform.position;
        LineCamera.transform.position = LineCamera.FollowTarget.position + Offset;
        LineCamera.StartFollowTarget();
        pos = new Vector3(pos.x + Mathf.Cos((RotateAngle  + initialAngle ) * ((float)Math.PI / 180f)) * 8f, 4f, pos.z + Mathf.Sin((RotateAngle  + initialAngle ) * ((float)Math.PI / 180f)) * 8f);
        BoundaryLookCamera.transform.position = pos;
        Invoke("EnableBoundaryCam", 0.5f);
    }

    public void EnableBoundaryCam()
    {
        if (CONTROLLER.cameraType == 0)
        {
            Singleton<MainCameraController>.instance.CameraTween.Pause();
            Singleton<MainCameraController>.instance.ReverseCameraTween.Pause();
            IsBoundaryCameraEnabled = true;
        }
        else
        {
            IsBoundaryCameraEnabled = true;
        }
    }

   
    public void setBallFirstBounce(float bounceDistance)
    {
        bounceFrame = bounceDistance;
    }

    public void Reset()
    {
        if (CONTROLLER.cameraType == 0)
        {
            mainTween.Kill();
            IsBoundaryCameraEnabled = false;
            mainTween = base.transform.DOMove(new Vector3(0f, 0f, 8.8f), 1f).SetAutoKill(autoKillOnCompletion: false);
            cameraEndPosition = new Vector3(30f, 10f, 30f);
            StartingFOV  = 50f;
            BoundaryCamera.enabled = false;
            Singleton<LeftCameraLookAt>.instance.canChangeFov = false;
            EndingFOV  = 25f;
            lerpTimer = 0f;
            Singleton<RotatorTowardsTarget>.instance.Reset();
            Singleton<LeftCameraLookAt>.instance._lerpTime = 0f;
            CanMove = true;
            CanMoveDown = false;
            Singleton<LeftCameraLookAt>.instance.canLookAt = true;
        }
        else
        {
            BoundaryLookCamera.GetComponent<Camera>().enabled = false;
            IsBoundaryCameraEnabled = false;
        }
    }

}

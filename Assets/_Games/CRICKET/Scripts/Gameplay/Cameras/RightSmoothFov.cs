using System;
using DG.Tweening;
using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class RightSmoothFov : Singleton<RightSmoothFov>
{
    [FormerlySerializedAs("BLC")]
    public GameObject BoundaryCollider;

    [FormerlySerializedAs("FovStart")]
    public float InitialFov = 60f;

    [FormerlySerializedAs("FovEnd")]
    public float FinalFov = 30f;

    [FormerlySerializedAs("TransitionTime")]
    public float FovTransitionDuration = 3.5f;

    [FormerlySerializedAs("canMove")]
    public bool CanMove;

    [FormerlySerializedAs("target")]
    public Transform TargetTransform;

    [FormerlySerializedAs("damping")]
    public float MovementDamping = 6f;

    [FormerlySerializedAs("mainCam")]
    public Camera MainCamera;

    [FormerlySerializedAs("canRotate")]
    public bool CanRotate;

    [FormerlySerializedAs("canLookAt")]
    public bool CanLookAtTarget;

    [FormerlySerializedAs("fovOffset")]
    public float FovOffset = 5f;

    [FormerlySerializedAs("smoothSpeed")]
    public float SmoothMovementSpeed = 0.125f;

    [FormerlySerializedAs("zoomSpeed")]
    public float ZoomSpeed = 3f;

    [FormerlySerializedAs("offset")]
    public Vector3 CameraOffset;

    [FormerlySerializedAs("start")]
    public Vector3 StartPosition;

    [FormerlySerializedAs("end")]
    public Vector3 EndPosition;

    [FormerlySerializedAs("boundaryCamEnable")]
    public bool IsBoundaryCameraEnabled;

    [FormerlySerializedAs("RotateAngle")]
    public float CameraRotationAngle = 90f;


    private float CurrentAngle = 90f;
    private float Velocity;
    private Vector3 PreviousPosition;
    private float CurrentFov;
    private float LerpTime;
    private Camera CameraComponent;
    private float FieldOfView;
    private float CameraLimit;
    private Camera CurrentCamera;
    private Vector3 DesiredPosition;
    private Vector3 SmoothedPosition;
    private float YPosition;
    private Vector3 FinalPosition;
    private Vector3 Position;
    private float BouncesAt;
    private Vector3 TargetLastPosition;
    private Tweener CameraTween;

    
    private void Start()
    {
        if (CONTROLLER.cameraType == 0)
        {
            BoundaryCollider.GetComponent<Camera>().enabled = false;
            //tween = base.transform.DOMove(target.position, 1f).SetAutoKill(autoKillOnCompletion: false);
            CameraTween = base.transform.DOMove(Singleton<GroundController>.instance.GetTempPos(), 1f).SetAutoKill(autoKillOnCompletion: false);
            //targetLastPos = target.position;
            TargetLastPosition = Singleton<GroundController>.instance.GetTempPos();
            CanMove = true;
            CanLookAtTarget = true;
            CameraComponent = GetComponent<Camera>();
            CurrentCamera = GetComponent<Camera>();
            FieldOfView = GetComponent<Camera>().fieldOfView;
            if ((bool)GetComponent<Rigidbody>())
            {
                GetComponent<Rigidbody>().freezeRotation = true;
            }
            CameraLimit = 70f;
        }
    }

    private void Update()
    {
        if ((bool)TargetTransform && IsBoundaryCameraEnabled && BouncesAt < 70f && DistanceBetweenTwoVector2(Singleton<GroundController>.instance.groundCenterMarker, TargetTransform.gameObject) > 40f)
        {
            BoundaryCollider.GetComponent<Camera>().enabled = true;
            MainCamera.enabled = false;
        }
    }

    private void resetTween()
    {
        if (CONTROLLER.cameraType == 0 && !CanMove)
        {
            //tween = base.transform.DOMove(target.position + offset, 3f).SetAutoKill(autoKillOnCompletion: false);
            CameraTween = base.transform.DOMove(Singleton<GroundController>.instance.GetTempPos() + CameraOffset, 3f).SetAutoKill(autoKillOnCompletion: false);
        }
    }

    public void setCameraPosition(Vector3 pos, GameObject target)
    {
        if (CONTROLLER.cameraType == 0)
        {
            //offset = Singleton<SmoothLookAtTwo>.instance.transform.position - Singleton<SmoothLookAtTwo>.instance.target.transform.position;
            CameraOffset = Singleton<SmoothLookAtTwo>.instance.transform.position - Singleton<GroundController>.instance.GetTempPos();
            Singleton<SmoothLookAtTwo>.instance.transform.position = Singleton<SmoothLookAtTwo>.instance.Target.position + CameraOffset;
            Singleton<SmoothLookAtTwo>.instance.StartFollowTarget();
            pos = new Vector3(pos.x + Mathf.Cos(CurrentAngle * ((float)Math.PI / 180f)) * 8f, 4f, pos.z + Mathf.Sin(CurrentAngle * ((float)Math.PI / 180f)) * 8f);
            BoundaryCollider.transform.position = pos;
            Invoke("SetBoundaryCam", 0.5f);
        }
        else
        {
            //offset = Singleton<SmoothLookAtTwo>.instance.transform.position - Singleton<SmoothLookAtTwo>.instance.target.transform.position;
            //Singleton<SmoothLookAtTwo>.instance.transform.position = Singleton<SmoothLookAtTwo>.instance.target.position + offset;

            //offset = Singleton<SmoothLookAtTwo>.instance.transform.position - Singleton<SmoothLookAtTwo>.instance.target.transform.position;
            CameraOffset = Singleton<SmoothLookAtTwo>.instance.transform.position - Singleton<GroundController>.instance.GetTempPos();
            Singleton<SmoothLookAtTwo>.instance.transform.position = Singleton<SmoothLookAtTwo>.instance.Target.position + CameraOffset;
            Singleton<SmoothLookAtTwo>.instance.StartFollowTarget();
            pos = new Vector3(pos.x + Mathf.Cos((CameraRotationAngle + CurrentAngle) * ((float)Math.PI / 180f)) * 8f, 4f, pos.z + Mathf.Sin((CameraRotationAngle + CurrentAngle) * ((float)Math.PI / 180f)) * 8f);
            BoundaryCollider.transform.position = pos;
            Invoke("SetBoundaryCam", 0.5f);
        }
    }

    public void setCameraPositionWicket(Vector3 pos, GameObject target)
    {
        if (CONTROLLER.cameraType == 0)
        {
            pos = new Vector3(pos.x - Mathf.Cos((135f + CurrentAngle) * ((float)Math.PI / 180f)) * 8f, 4f, pos.z - Mathf.Sin((135f + CurrentAngle) * ((float)Math.PI / 180f)) * 8f);
            BoundaryCollider.transform.position = pos;
            Invoke("SetBoundaryCam", 0.3f);
            return;
        }
        //offset = Singleton<SmoothLookAtTwo>.instance.transform.position - Singleton<SmoothLookAtTwo>.instance.target.transform.position;
        //Singleton<SmoothLookAtTwo>.instance.transform.position = Singleton<SmoothLookAtTwo>.instance.target.position + offset;
        //offset = Singleton<SmoothLookAtTwo>.instance.transform.position - Singleton<SmoothLookAtTwo>.instance.target.transform.position;
        CameraOffset = Singleton<SmoothLookAtTwo>.instance.transform.position - Singleton<GroundController>.instance.GetTempPos();
        Singleton<SmoothLookAtTwo>.instance.transform.position = Singleton<SmoothLookAtTwo>.instance.Target.position + CameraOffset;
        Singleton<SmoothLookAtTwo>.instance.StartFollowTarget();
        pos = new Vector3(pos.x + Mathf.Cos((CameraRotationAngle + CurrentAngle) * ((float)Math.PI / 180f)) * 8f, 4f, pos.z + Mathf.Sin((CameraRotationAngle + CurrentAngle) * ((float)Math.PI / 180f)) * 8f);
        BoundaryCollider.transform.position = pos;
        Invoke("SetBoundaryCam", 0.5f);
    }

    public void SetBoundaryCam()
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
   

    public float DistanceBetweenTwoVector2(GameObject go1, GameObject go2)
    {
        float num = go1.transform.position.x - go2.transform.position.x;
        float num2 = go1.transform.position.z - go2.transform.position.z;
        return Mathf.Sqrt(num * num + num2 * num2);
    }

    public void setBallAngle(float ballAngle)
    {
    }

    public void setBallFirstBounce(float bounceDistance)
    {
        BouncesAt = bounceDistance;
    }

    private void ChangeFOV()
    {
        if (CONTROLLER.cameraType == 0)
        {
            LerpTime += Time.deltaTime;
            float t = LerpTime / 5f;
            t = Mathf.SmoothStep(0f, 1f, t);
            t = SmootherStep(t);
            t *= t;
            t = t * t * t;
            CurrentFov = Mathf.Lerp(50f, 30f, t);
            GetComponent<Camera>().fieldOfView = CurrentFov;
        }
    }

    public void Reset()
    {
        if (CONTROLLER.cameraType == 0)
        {
            CameraTween.Kill();
            CameraTween = base.transform.DOMove(new Vector3(0f, 0f, 8.8f), 1f).SetAutoKill(autoKillOnCompletion: false);
            FinalPosition = new Vector3(30f, 10f, 30f);
            BoundaryCollider.GetComponent<Camera>().enabled = false;
            IsBoundaryCameraEnabled = false;
            InitialFov = 50f;
            FinalFov = 25f;
            LerpTime = 0f;
            Singleton<SmoothLookAtTwo>.instance.Reset();
            Singleton<RightCameraFocus>.instance.LerpDuration = 0f;
            CanMove = true;
            Singleton<RightCameraFocus>.instance.IsLookAtEnabled = true;
        }
        else
        {
            BoundaryCollider.GetComponent<Camera>().enabled = false;
            IsBoundaryCameraEnabled = false;
        }
    }

    private float SmootherStep(float t)
    {
        return t * t * t * (t * (6f * t - 15f) + 10f);
    }
}

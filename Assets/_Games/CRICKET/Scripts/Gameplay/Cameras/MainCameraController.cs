using System;
using DG.Tweening;
using UnityEngine;
using Cricket;

public class MainCameraController : Singleton<MainCameraController>
{
    public GameObject MainCamera;
    public GameObject DummyCamera;
    public float InitialFov = 60f;
    public float FinalFov  = 30f;
    public float FovTransitionTime  = 3.5f;
    public bool CanMoveCamera;
    public Transform CameraTarget;
    public Transform CameraPositionTarget;
    public float CameraDamping  = 6f;
    public bool CanRotateCamera;
    public bool CanLookAtTarget;
    public float FovOffset  = 5f;
    public float SmoothMoveSpeed  = 0.125f;
    public float ZoomSpeed  = 3f;
    public Vector3 CameraOffset;
    public Vector3 CameraStartPosition;
    public Vector3 CameraEndPosition;
    public Vector3 BLCStartPosition;
    public Tweener CameraTween;
    public Tweener ReverseCameraTween;
    public bool IsBoundaryCameraEnabled;

    private float CameraAngle  = 90f;
    private float ShotAngle;
    private float Velocity;
    private Vector3 PreviousPosition;
    private bool IsFirstTimeCalled;
    private float TweenDuration  = 5f;
    private float CurrentFov;
    private float LerpTime;
    private Camera MainCameraComponent;
    private float FovValue;
    private float CameraLimit;
    private Camera CameraComponent;
    private float YPosition;
    private float OffsetAngle;
    private Vector3 DesiredCameraPosition;
    private Vector3 SmoothedCameraPosition;
    private Vector3 EndCameraPosition;
    private Vector3 CurrentPosition;
    private float BounceFactor;
    private Vector3 LastTargetPosition;

    private void Start()
	{
		if (CONTROLLER.cameraType == 0)
		{
			IsFirstTimeCalled = true;
			MainCamera.GetComponent<Camera>().enabled = false;
			CameraStartPosition = base.transform.position;
			BLCStartPosition = new Vector3(0f, 6.8f, 8f);
			LastTargetPosition = CameraTarget.position;
			CanLookAtTarget = true;
			MainCameraComponent = GetComponent<Camera>();
			CameraComponent = GetComponent<Camera>();
			FovValue = GetComponent<Camera>().fieldOfView;
			if ((bool)GetComponent<Rigidbody>())
			{
				GetComponent<Rigidbody>().freezeRotation = true;
			}
			CameraLimit = 70f;
		}
	}

	private void Update()
	{
		if (CONTROLLER.cameraType != 0)
		{
			return;
		}
		Vector3 vector = (base.transform.position - PreviousPosition) / Time.deltaTime;
		PreviousPosition = base.transform.position;
		Velocity = vector.magnitude;
		if (!CameraTarget)
		{
			return;
		}
		if (BounceFactor > 40f)
		{
			TweenDuration  = 5f;
		}
		else
		{
			TweenDuration  = 5f;
		}
		if (Singleton<GroundController>.instance.currentActionState <= 2 || !CanMoveCamera)
		{
			return;
		}
		if (!Singleton<GroundController>.instance.isBallOnBoundaryLine)
		{
			if (ShotAngle > 220f && ShotAngle <= 320f)
			{
				GetComponent<Camera>().enabled = false;
				MainCamera.GetComponent<Camera>().enabled = true;
			}
			if (BounceFactor > 45f && GetComponent<Camera>().fieldOfView > 20f)
			{
				GetComponent<Camera>().fieldOfView -= 0.05f;
			}
			if (DistanceBetweenTwoVector2(Singleton<GroundController>.instance.groundCenterMarker, CameraTarget.gameObject) > 20f && MainCamera.GetComponent<Camera>().fieldOfView > 20f)
			{
				MainCamera.GetComponent<Camera>().fieldOfView -= 0.05f;
			}
			Vector3 forward = CameraTarget.position - base.transform.position;
			Quaternion b = Quaternion.LookRotation(forward);
			base.transform.rotation = Quaternion.Slerp(base.transform.rotation, b, Time.deltaTime * CameraDamping );
		}
		else
		{
			CameraTween.Pause();
			ReverseCameraTween.Pause();
		}
	}

	public void StartFollowTween()
	{
		if (CONTROLLER.cameraType == 0 && !(Singleton<GroundController>.instance.currentShotPlayed == "bt6Defense") && !(Singleton<GroundController>.instance.currentShotPlayed == "backFootDefenseHighBall") && IsFirstTimeCalled)
		{
			if ((ShotAngle < 270f && ShotAngle > 90f && CONTROLLER.StrikerHand == "right") || ((ShotAngle < 90f || ShotAngle > 270f) && CONTROLLER.StrikerHand == "left"))
			{
				OffsetAngle = UnityEngine.Random.Range(-30f, -15f);
			}
			else
			{
				OffsetAngle = UnityEngine.Random.Range(15f, 30f);
			}
			CameraTween = base.transform.DOMove(new Vector3(Mathf.Cos((CameraAngle  + OffsetAngle) * ((float)Math.PI / 180f)) * 40f, 4f, Mathf.Sin((CameraAngle  + OffsetAngle) * ((float)Math.PI / 180f)) * 40f), TweenDuration ).SetAutoKill(autoKillOnCompletion: false);
			ReverseCameraTween = MainCamera.transform.DOMove(new Vector3(Mathf.Cos(CameraAngle  * ((float)Math.PI / 180f)) * 30f, 4f, Mathf.Sin(CameraAngle  * ((float)Math.PI / 180f)) * 30f), 6f).SetAutoKill(autoKillOnCompletion: false);
			setDummyCameraPosition();
			CanMoveCamera = true;
			IsFirstTimeCalled = false;
		}
	}

	private void resetTween()
	{
		if (CONTROLLER.cameraType == 0 && !CanMoveCamera)
		{
			CameraTween = base.transform.DOMove(CameraTarget.position + CameraOffset, 3f).SetAutoKill(autoKillOnCompletion: false);
		}
	}

	public void setCameraPosition(Vector3 pos, GameObject target)
	{
		if (CONTROLLER.cameraType == 0)
		{
			Singleton<SmoothLookAtTwo>.instance.Target = this.CameraTarget;
			pos = new Vector3(pos.x + Mathf.Cos(CameraAngle  * ((float)Math.PI / 180f)) * 8f, 4f, pos.z + Mathf.Sin(CameraAngle  * ((float)Math.PI / 180f)) * 8f);
			MainCamera.transform.position = pos;
			IsBoundaryCameraEnabled = true;
			ReverseTween();
		}
	}

	public void ReverseTween()
	{
		if (CONTROLLER.cameraType == 0)
		{
			CameraTween.SmoothRewind();
			CameraTween.Play();
		}
	}

	private void StopCameraLookAt()
	{
		if (CONTROLLER.cameraType == 0)
		{
			Singleton<RightCameraFocus>.instance.IsLookAtEnabled = false;
		}
	}

	private void PauseTween()
	{
		if (CONTROLLER.cameraType == 0)
		{
			CameraTween = base.transform.DOMove(CameraTarget.position, 2f);
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
		if (CONTROLLER.cameraType == 0)
		{
			ShotAngle = ballAngle;
			if (Singleton<GroundController>.instance.batsmanHand == "right")
			{
				CameraAngle  = ballAngle;
			}
			else
			{
				CameraAngle  = 180f - ballAngle;
			}
		}
	}

	public void setDummyCameraPosition()
	{
		if (CONTROLLER.cameraType == 0)
		{
			Singleton<DummyScript>.instance.StartFollowTarget();
			DummyCamera.transform.position = new Vector3(CameraTarget.position.x + Mathf.Cos(CameraAngle  * ((float)Math.PI / 180f)) * 8f, 4f, CameraTarget.position.z + Mathf.Sin(CameraAngle  * ((float)Math.PI / 180f)) * 8f);
		}
	}

	public void MoveCamForward()
	{
		if (CONTROLLER.cameraType == 0)
		{
			CameraTween = base.transform.DOMove(CameraPositionTarget.position, 7f).SetAutoKill(autoKillOnCompletion: false);
		}
	}

	public void setBallFirstBounce(float bounceDistance)
	{
		if (CONTROLLER.cameraType == 0)
		{
			BounceFactor = bounceDistance;
		}
	}

	private void ChangeFOV()
	{
		if (CONTROLLER.cameraType == 0)
		{
			LerpTime += Time.deltaTime;
			float t = LerpTime / FovTransitionTime ;
			t = Mathf.SmoothStep(0f, 1f, t);
			t = SmootherStep(t);
			CurrentFov = Mathf.Lerp(GetComponent<Camera>().fieldOfView, FinalFov , t);
			GetComponent<Camera>().fieldOfView = CurrentFov;
		}
	}

	public void Reset()
	{
		if (CONTROLLER.cameraType == 0)
		{
			IsFirstTimeCalled = true;
			CameraTween.Complete();
			CameraTween.Kill();
			ReverseCameraTween.Complete();
			ReverseCameraTween.Kill();
			Singleton<DummyScript>.instance.Reset();
			base.transform.position = CameraStartPosition;
			MainCamera.transform.position = BLCStartPosition;
			MainCamera.GetComponent<Camera>().fieldOfView = 40f;
			LerpTime = 0f;
		}
	}

	private float SmootherStep(float t)
	{
		return t * t * t * (t * (6f * t - 15f) + 10f);
	}
}

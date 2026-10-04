using DG.Tweening;
using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class DRSCameraScript : Singleton<DRSCameraScript>
{
	private Camera drsCamera;

	private Vector3 targetPosition = Vector3.zero;

	private bool isCameraFixedInStumpCrease;

	private bool isCameraMovedToSideOfStump;

	private bool areAllRenderersDisabled;

	private bool isCameraMovedToTopOfPitch;

	private bool hasHitStump;

	[FormerlySerializedAs("timeInterval")] public float timeIntervalInSeconds = 4f;

	private TweenCallback callbackReference;

	 public GameObject lbwLineIndicator;

	private void Start()
	{
		drsCamera = GetComponent<Camera>();
		EnableDRSCamera(state: false);
		lbwLineIndicator.SetActive(value: false);
	}

	public void FixAtStump2Crease()
	{
		if (!isCameraFixedInStumpCrease)
		{
			isCameraFixedInStumpCrease = true;
			lbwLineIndicator.SetActive(value: true);
			Singleton<GroundController>.instance.SetDRSTrailRenderer();
			SetTimeScale(0.2f);
			targetPosition = new Vector3(Singleton<GroundController>.instance.groundCenterMarker.transform.position.x, 1.5f, Singleton<GroundController>.instance.groundCenterMarker.transform.position.z - 2f);
			drsCamera.transform.eulerAngles = new Vector3(0f, -0.4f, 0f);
			drsCamera.fieldOfView = 40f;
			drsCamera.transform.DOMove(targetPosition, 0f).SetUpdate(isIndependentUpdate: true);
			EnableDRSCamera(state: true);
			Singleton<DRS>.instance.ResetPanelTransition();
		}
	}

	public void DisableAllRenderers()
	{
		if (!areAllRenderersDisabled)
		{
			areAllRenderersDisabled = true;
			Singleton<GroundController>.instance.ProcessOnImpact();
			SetTimeScale(0f);
			Sequence sequence = DOTween.Sequence();
			callbackReference = delegate
			{
				Singleton<GroundController>.instance.EnableAllSkinRenderers(state: false);
			};
			sequence.InsertCallback(2.5f, callbackReference);
			callbackReference = delegate
			{
				SetTimeScale(0.1f);
			};
			sequence.InsertCallback(4f, callbackReference);
			sequence.SetUpdate(isIndependentUpdate: true);
		}
	}

	private void SetTimeScale(float timescale)
	{
		Time.timeScale = timescale;
	}

	public void HitStump(float state)
	{
		if (!hasHitStump)
		{
			hasHitStump = true;
			if (state == 1f)
			{
				state = 0.001f;
			}
			Sequence sequence = DOTween.Sequence();
			callbackReference = delegate
			{
				SetTimeScale(0f);
			};
			sequence.InsertCallback(state, callbackReference);
			sequence.InsertCallback(1f, ShowStumpImpacts);
			sequence.SetUpdate(isIndependentUpdate: true);
		}
	}

	public void ShowStumpImpacts()
	{
		if (!isCameraMovedToSideOfStump)
		{
			isCameraMovedToSideOfStump = true;
			targetPosition = new Vector3(Singleton<GroundController>.instance.groundCenterMarker.transform.position.x + 2.68f, 0.58f, Singleton<GroundController>.instance.groundCenterMarker.transform.position.z + 9.5f);
			drsCamera.transform.DORotate(new Vector3(0.1f, -97f, 0f), 1f).SetUpdate(isIndependentUpdate: true);
			drsCamera.fieldOfView = 30f;
			Sequence sequence = DOTween.Sequence();
			sequence.Append(drsCamera.transform.DOMove(targetPosition, 1f));
			sequence.InsertCallback(timeIntervalInSeconds, MoveCameraToBackOfStump);
			sequence.InsertCallback(timeIntervalInSeconds * 2f, MoveCameraToTopOfStump);
			sequence.InsertCallback(timeIntervalInSeconds * 3f, DRSCompleted);
			sequence.SetUpdate(isIndependentUpdate: true);
		}
	}

	public void MoveCameraToBackOfStump()
	{
		targetPosition = new Vector3(Singleton<GroundController>.instance.groundCenterMarker.transform.position.x, 0.55f, Singleton<GroundController>.instance.groundCenterMarker.transform.position.z + 12.51f);
		drsCamera.transform.DORotate(new Vector3(0.1f, -184.6f, 0f), 2f).SetUpdate(isIndependentUpdate: true);
		drsCamera.fieldOfView = 30f;
		drsCamera.transform.DOMove(targetPosition, 2f).SetUpdate(isIndependentUpdate: true);
	}

	public void MoveCameraToTopOfStump()
	{
		targetPosition = new Vector3(Singleton<GroundController>.instance.groundCenterMarker.transform.position.x + 0.02f, 3.02f, Singleton<GroundController>.instance.groundCenterMarker.transform.position.z + 9.97f);
		drsCamera.transform.DORotate(new Vector3(90f, -180f, 0f), 2f).SetUpdate(isIndependentUpdate: true);
		drsCamera.fieldOfView = 30f;
		drsCamera.transform.DOMove(targetPosition, 2f).SetUpdate(isIndependentUpdate: true);
	}

	public void MoveCameraToTopOfPitch()
	{
		if (!isCameraMovedToTopOfPitch)
		{
			isCameraMovedToTopOfPitch = true;
			SetTimeScale(0f);
			Singleton<GroundController>.instance.EnableAllSkinRenderers(state: false);
			targetPosition = new Vector3(Singleton<GroundController>.instance.groundCenterMarker.transform.position.x - 0.29f, 6.79f, Singleton<GroundController>.instance.groundCenterMarker.transform.position.z + 5.56f);
			drsCamera.transform.DORotate(new Vector3(90f, 0f, 0f), 2f).SetUpdate(isIndependentUpdate: true);
			drsCamera.fieldOfView = 60f;
			Sequence sequence = DOTween.Sequence();
			sequence.Append(drsCamera.transform.DOMove(targetPosition, 2f));
			sequence.InsertCallback(timeIntervalInSeconds, DRSCompleted);
			sequence.SetUpdate(isIndependentUpdate: true);
		}
	}

	public void EnableDRSCamera(bool state)
	{
		drsCamera.enabled = state;
	}

	private void ResetAllVariables()
	{
		isCameraFixedInStumpCrease = false;
		isCameraMovedToSideOfStump = false;
		areAllRenderersDisabled = false;
		isCameraMovedToTopOfPitch = false;
		hasHitStump = false;
	}

	public void DRSCompleted()
	{
		CONTROLLER.cameraType = 1;
		EnableDRSCamera(state: false);
		ResetAllVariables();
		lbwLineIndicator.SetActive(value: false);
		Singleton<DRS>.instance.Hide();
		Singleton<GroundController>.instance.EnableAllSkinRenderers(state: true);
		Singleton<GroundController>.instance.ShowBowler(showStatus: false);
		Singleton<GroundController>.instance.showUmpireAfterDrs();
		Singleton<GroundController>.instance.isPitchOutsideLegForDRS = false;
	}
}

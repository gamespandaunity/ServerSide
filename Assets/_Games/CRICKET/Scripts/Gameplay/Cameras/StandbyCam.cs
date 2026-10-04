using DG.Tweening;
using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class StandbyCam : Singleton<StandbyCam>
{
	 public Camera standbyCamera;

	 public Camera arcadeModeStandbyCamera;

	public Transform standbyCameraPivot;

	private Tweener cameraTween;

	public void RotateStandbyCam()
	{
		cameraTween = null;
		standbyCamera.enabled = true;
		standbyCameraPivot.eulerAngles = Vector3.zero;
		cameraTween = standbyCameraPivot.DOLocalRotate(new Vector3(0f, 360f, 0f), 480f, RotateMode.WorldAxisAdd).SetLoops(-1, LoopType.Yoyo).SetUpdate(isIndependentUpdate: true);
		cameraTween.Play();
	}

	public void RotateStandbyCamArcadeMode()
	{
		cameraTween = null;
		arcadeModeStandbyCamera.enabled = true;
		float duration = 30f;
		if (standbyCameraPivot.eulerAngles.y < 355f)
		{
			duration = 60f;
		}
		else if (standbyCameraPivot.eulerAngles.y > 355f || standbyCameraPivot.eulerAngles.y == 0f)
		{
			duration = 30f;
		}
		cameraTween = standbyCameraPivot.DOLocalRotate(new Vector3(0f, 368f, 0f), duration, RotateMode.FastBeyond360).SetUpdate(isIndependentUpdate: true).OnComplete(RotateBack);
		cameraTween.Play();
	}

	private void RotateBack()
	{
		cameraTween = null;
		cameraTween = standbyCameraPivot.DOLocalRotate(new Vector3(0f, -8f, 0f), 60f, RotateMode.FastBeyond360).SetUpdate(isIndependentUpdate: true).OnComplete(RotateStandbyCamArcadeMode);
		cameraTween.Play();
	}

	public void PauseTween()
	{
		standbyCamera.enabled = false;
		if (cameraTween != null && cameraTween.IsPlaying())
		{
			cameraTween.Pause();
		}
	}

	public void PauseTweenArcadeModes()
	{
		arcadeModeStandbyCamera.enabled = false;
		cameraTween = null;
	}

	public void DisableAllCams()
	{
		standbyCamera.enabled = false;
		arcadeModeStandbyCamera.enabled = false;
	}
}

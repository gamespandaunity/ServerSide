using System.Collections.Generic;
using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class RewindTime : Singleton<RewindTime>
{
	[FormerlySerializedAs("isRewinding")] public bool IsRollingBack;
	[FormerlySerializedAs("canRecord")] public bool CanCapture;
	[FormerlySerializedAs("canRecordActualPath")] public bool CanCaptureActualRoute;
	[FormerlySerializedAs("positions")] public List<Vector3> CapturePoints;
	[FormerlySerializedAs("tempPositions")] public List<Vector3> TempCapturePoints;
	[FormerlySerializedAs("forwardPositions")] public List<Vector3> ForwardCapturePoints;

	public static int CaptureCount;
	private void Start()
	{
		CapturePoints = new List<Vector3>();
	}

	private void Update()
	{
		if (IsRollingBack && CanCapture)
		{
			Rewind();
		}
		else if (!IsRollingBack && CanCapture)
		{
			Record();
		}
	}

	private void Rewind()
	{
		if (CapturePoints.Count > 0)
		{
			base.transform.position = CapturePoints[0];
			RecordTemp();
			CapturePoints.RemoveAt(0);
			return;
		}
		CaptureCount++;
		if (CaptureCount % 2 != 0)
		{
			Singleton<GroundController>.instance.batsmanAnim[Singleton<GroundController>.instance.currentBatsmanAnimation].speed = 1f;
		}
		if (CaptureCount % 2 == 0 && CaptureCount > 0)
		{
			Singleton<GroundController>.instance.batsmanAnim[Singleton<GroundController>.instance.currentBatsmanAnimation].speed = -1f;
		}
		TransferPositions();
	}

	private void TransferPositions()
	{
		for (int num = TempCapturePoints.Count - 1; num >= 0; num--)
		{
			CapturePoints.Insert(0, TempCapturePoints[num]);
		}
		TempCapturePoints.Clear();
		base.transform.position = CapturePoints[0];
	}

	private void Record()
	{
		////Debug.Log("POsitions : ");
		CapturePoints.Insert(0, base.transform.position);
	}

	private void RecordTemp()
	{
		TempCapturePoints.Insert(0, base.transform.position);
	}

	public void StartRewind()
	{
		IsRollingBack = true;
	}

	public void StopRewind()
	{
		IsRollingBack = false;
		TempCapturePoints.Clear();
		CapturePoints.Clear();
	}
}

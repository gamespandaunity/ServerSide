using System;
using UnityEngine;
using Cricket;
using UnityEngine.Serialization;

public class BallSimulationManager : Singleton<BallSimulationManager>
{
	private bool isEnabled = true;

	[FormerlySerializedAs("simulatedBallHolder")] public Transform ballHolderAnchor;

	[FormerlySerializedAs("ballTextures")] public Texture[] ballSkins;

	private Transform[] simulatedBalls;

	private TrailRenderer[] ballTrails;

	private float[] ballAngles;

	private float[] ballHorizontalSpeeds;

	private float[] ballHeights;

	private float[] ballArcAngles;

	private float[] ballTravelDistances;

	private int[] ballBounceCounts;

	private float[] ballAnglePerSecond;

	private float[] swingAnglePerSecond;

	private float[] swingAngles;

	private float[] ballSwingValues;

	private float[] ballSpinValues;

	private string bowlerType;

	private string bowlerHand;

	private string[] bowlerSides;

	private string[] scoreDataPerBall;

	[FormerlySerializedAs("startSimulation")] public bool beginSimulation;

	private bool skipSimulation;

	[FormerlySerializedAs("retriveSavedData")] public bool loadSavedData;

	[FormerlySerializedAs("settedSimulationData")] public bool hasSetSimulationData;

	[FormerlySerializedAs("noOfBallDataSetted")] public int totalSetBallData;

	[FormerlySerializedAs("noOfSimulatedBalls")] public int totalSimulatedBalls;

	[FormerlySerializedAs("currentSimulationBallIndex")] public int simBallIndex;

	private Vector3 ballStartPos = Vector3.zero;

	private float[] tempStats;

	[FormerlySerializedAs("showingBallSimulation")] public bool isShowingSimulation;

	private float spinSpeedX;

	private float spinSpeedZ;

	private float degToRad = (float)Math.PI / 180f;

	private float ballSize = 0.05f;

	private int index;

	private void Start()
	{
		InitializeVariables();
		Singleton<BallSimulationCameraScripts>.instance.InitializeCamera();
		ShowAllBall(state: false);
	}

	private void SetTestingData()
	{
		for (index = 0; index < totalSimulatedBalls; index++)
		{
			if (index == 1)
			{
				SetTempData(93.4f, 16.6f, 2.15f, 14f, 2f, 2f);
				SetBallSimulationData("six", "spin", "left", "left");
			}
			else if (index == 2)
			{
				SetTempData(93.4f, 16.6f, 2.15f, 14f, 2f, 2f);
				SetBallSimulationData("six", "spin", "left", "right");
			}
			else if (index == 0)
			{
				SetTempData(93.4f, 16.6f, 2.15f, 14f, 2f, 2f);
				SetBallSimulationData("six", "spin", "right", "left");
			}
			else
			{
				SetTempData(93.4f, 16.6f, 2.15f, 14f, 2f, 2f);
				SetBallSimulationData("six", "spin", "right", "right");
			}
		}
	}

	public void InitializeVariables()
	{
		totalSimulatedBalls = ballHolderAnchor.childCount;
		simulatedBalls = new Transform[totalSimulatedBalls];
		for (index = 0; index < totalSimulatedBalls; index++)
		{
			simulatedBalls[index] = ballHolderAnchor.GetChild(index);
		}
		ballAngles = new float[totalSimulatedBalls];
		ballHorizontalSpeeds = new float[totalSimulatedBalls];
		ballHeights = new float[totalSimulatedBalls];
		ballTravelDistances = new float[totalSimulatedBalls];
		ballBounceCounts = new int[totalSimulatedBalls];
		ballArcAngles = new float[totalSimulatedBalls];
		ballAnglePerSecond = new float[totalSimulatedBalls];
		swingAnglePerSecond = new float[totalSimulatedBalls];
		swingAngles = new float[totalSimulatedBalls];
		ballSwingValues = new float[totalSimulatedBalls];
		ballSpinValues = new float[totalSimulatedBalls];
		scoreDataPerBall = new string[totalSimulatedBalls];
		bowlerSides = new string[totalSimulatedBalls];
		ballTrails = new TrailRenderer[totalSimulatedBalls];
		for (index = 0; index < ballTrails.Length; index++)
		{
			ballTrails[index] = simulatedBalls[index].GetComponent<TrailRenderer>();
			ballTrails[index].time = 300f;
			if (CONTROLLER.PlayModeSelected == 7)
			{
				//simulatedBall[i].GetComponent<Renderer>().material.mainTexture = ballTextures[0];
			}
			else
			{
				simulatedBalls[index].GetComponent<Renderer>().material.mainTexture = ballSkins[1];
			}
		}
		ballStartPos = simulatedBalls[0].position;
		simBallIndex = 0;
		tempStats = new float[7];
	}

	public void StartBallSimulation()
	{
		simBallIndex = 0;
		ShowAllBall(state: true);
		isShowingSimulation = true;
		ClearAllSaveData();
		Singleton<GroundController>.instance.stump1Anim.Play("idle");
		Singleton<GroundController>.instance.stump2Anim.Play("idle");
		Singleton<GameData>.instance.skipPanel.SetActive(value: true);
		for (index = 0; index < totalSetBallData; index++)
		{
			ballArcAngles[index] = 270f;
			SetBallOrigin(index);
			SetProjectileAnglePerSecond(index);
		}
		SetTrailColors();
		Time.timeScale = 0.7f;
		Singleton<BallSimulationCameraScripts>.instance.StartBallSimulationCamera();
	}

	public void StartBallMovement()
	{
		beginSimulation = true;
	}

	private void ShowAllBall(bool state)
	{
		for (index = 0; index < totalSimulatedBalls; index++)
		{
			simulatedBalls[index].gameObject.SetActive(state);
			ballBounceCounts[index] = 0;
			if (!state)
			{
				ballTrails[index].Clear();
			}
		}
	}

	public void BallSimulationCompleted()
	{
		beginSimulation = false;
		hasSetSimulationData = false;
		simBallIndex = 0;
		totalSetBallData = 0;
		ShowAllBall(state: false);
		Singleton<BallSimulationCameraScripts>.instance.EnableCamera(state: false);
		skipSimulation = false;
		isShowingSimulation = false;
		Singleton<GameData>.instance.skipPanel.SetActive(value: false);
		Singleton<GameData>.instance.ShowScoreCard();
	}

	public void BallSimulationSkipped()
	{
		skipSimulation = true;
		Singleton<BallSimulationCameraScripts>.instance.skip();
		BallSimulationCompleted();
	}

	public bool CanShowBallSimulation()
	{
		if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && isEnabled)
		{
			return true;
		}
		return false;
	}

	public void SetTempData(float ballAngleL, float horizontalSpeedL, float projectileHeightL, float spotLengthL, float swingValueL, float spinValueL)
	{
		tempStats[0] = ballAngleL;
		tempStats[1] = horizontalSpeedL;
		tempStats[2] = projectileHeightL;
		tempStats[3] = spotLengthL;
		tempStats[4] = spinValueL;
		tempStats[5] = swingValueL;
	}

	public void SetBallSimulationData(string data, string currentBowlerTypeL, string bowlerSideL, string currentBowlerHandL)
	{
		if (totalSetBallData == 0)
		{
			simBallIndex = 0;
		}
		if (loadSavedData)
		{
			loadSavedData = false;
			RetriveBallSimulationData();
		}
		////Debug.Log("ERRRORRR :: " + currentSimulationBallIndex);
		// Bounds guard: a DRS / extra re-entry could call this after simBallIndex already reached the
		// over's array size, throwing IndexOutOfRange and ABORTING the DRS replay coroutine (the stuck
		// "action replay" text + bowler auto-bowling bug). Skip safely when the index is out of range.
		if (ballAngles == null || simBallIndex < 0 || simBallIndex >= ballAngles.Length)
		{
			Debug.LogWarning("[BallSimulationManager] SetBallSimulationData skipped — simBallIndex " + simBallIndex + " out of bounds (len " + (ballAngles == null ? 0 : ballAngles.Length) + ").");
			return;
		}
		ballAngles[simBallIndex] = tempStats[0];
		ballHorizontalSpeeds[simBallIndex] = tempStats[1];
		ballHeights[simBallIndex] = tempStats[2];
		ballTravelDistances[simBallIndex] = tempStats[3];
		ballSpinValues[simBallIndex] = tempStats[4];
		ballSwingValues[simBallIndex] = tempStats[5];
		bowlerType = currentBowlerTypeL;
		scoreDataPerBall[simBallIndex] = data;
		bowlerHand = currentBowlerHandL;
		bowlerSides[simBallIndex] = bowlerSideL;
		if (totalSetBallData > 4)
		{
			hasSetSimulationData = true;
		}
		totalSetBallData++;
		SaveBallSimulationData();
		simBallIndex++;
	}

	private void SaveBallSimulationData()
	{
		AutoSave.simulatedBallAngle = AutoSave.simulatedBallAngle + ballAngles[simBallIndex] + "|";
		AutoSave.simulatedBallHorizontalSpeed = AutoSave.simulatedBallHorizontalSpeed + ballHorizontalSpeeds[simBallIndex] + "|";
		AutoSave.simulatedBallProjectileHeight = AutoSave.simulatedBallProjectileHeight + ballHeights[simBallIndex] + "|";
		AutoSave.simulatedBallSpotLength = AutoSave.simulatedBallSpotLength + ballTravelDistances[simBallIndex] + "|";
		AutoSave.simulatedBallSpinValue = AutoSave.simulatedBallSpinValue + ballSpinValues[simBallIndex] + "|";
		AutoSave.simulatedBallSwingValue = AutoSave.simulatedBallSwingValue + ballSwingValues[simBallIndex] + "|";
		AutoSave.currentBowlerType = bowlerType;
		AutoSave.ballScoreData = AutoSave.ballScoreData + scoreDataPerBall[simBallIndex] + "|";
		AutoSave.currentBowlerHand = bowlerHand;
		AutoSave.bowlerSide = AutoSave.bowlerSide + bowlerSides[simBallIndex] + "|";
		if (totalSetBallData < 6)
		{
			AutoSave.noOfBallDataSetted = totalSetBallData.ToString();
		}
		else
		{
			AutoSave.noOfBallDataSetted = "0";
		}
	}

	public void RetriveBallSimulationData()
	{
		totalSetBallData = int.Parse(AutoSave.noOfBallDataSetted);
		if (totalSetBallData != 0)
		{
			string[] array = new string[6];
			array = AutoSave.simulatedBallAngle.Split('|');
			for (index = 0; index < array.Length - 1; index++)
			{
				ballAngles[index] = float.Parse(array[index]);
			}
			array = AutoSave.simulatedBallHorizontalSpeed.Split('|');
			for (index = 0; index < array.Length - 1; index++)
			{
				ballHorizontalSpeeds[index] = float.Parse(array[index]);
			}
			array = AutoSave.simulatedBallProjectileHeight.Split('|');
			for (index = 0; index < array.Length - 1; index++)
			{
				ballHeights[index] = float.Parse(array[index]);
			}
			array = AutoSave.simulatedBallSpotLength.Split('|');
			for (index = 0; index < array.Length - 1; index++)
			{
				ballTravelDistances[index] = float.Parse(array[index]);
			}
			array = AutoSave.simulatedBallSpinValue.Split('|');
			for (index = 0; index < array.Length - 1; index++)
			{
				ballSpinValues[index] = float.Parse(array[index]);
			}
			array = AutoSave.simulatedBallSwingValue.Split('|');
			for (index = 0; index < array.Length - 1; index++)
			{
				ballSwingValues[index] = float.Parse(array[index]);
			}
			array = AutoSave.ballScoreData.Split('|');
			for (index = 0; index < array.Length - 1; index++)
			{
				scoreDataPerBall[index] = array[index];
			}
			array = AutoSave.bowlerSide.Split('|');
			for (index = 0; index < array.Length - 1; index++)
			{
				bowlerSides[index] = array[index];
			}
			bowlerHand = AutoSave.currentBowlerHand;
			bowlerType = AutoSave.currentBowlerType;
			simBallIndex = totalSetBallData;
		}
	}

	public void ClearAllSaveData()
	{
		AutoSave.simulatedBallAngle = string.Empty;
		AutoSave.simulatedBallHorizontalSpeed = string.Empty;
		AutoSave.simulatedBallProjectileHeight = string.Empty;
		AutoSave.simulatedBallSpotLength = string.Empty;
		AutoSave.simulatedBallSpinValue = string.Empty;
		AutoSave.simulatedBallSwingValue = string.Empty;
		AutoSave.currentBowlerType = string.Empty;
		AutoSave.ballScoreData = string.Empty;
		AutoSave.currentBowlerHand = string.Empty;
		AutoSave.bowlerSide = string.Empty;
		AutoSave.noOfBallDataSetted = "0";
		AutoSave.SaveInGameMatch();
	}

	private void SetTrailColors()
	{
		for (index = 0; index < scoreDataPerBall.Length; index++)
		{
			switch (scoreDataPerBall[index])
			{
			case "dot":
				ballTrails[index].material.color = Color.green;
				break;
			case "run":
				ballTrails[index].material.color = Color.yellow;
				break;
			case "four":
				ballTrails[index].material.color = Color.magenta;
				break;
			case "six":
				ballTrails[index].material.color = Color.red;
				break;
			case "wicket":
				ballTrails[index].material.color = Color.white;
				break;
			}
		}
	}

	private void SetBallOrigin(int index)
	{
		if (bowlerSides[index] == "left")
		{
			if (bowlerHand == "left")
			{
				simulatedBalls[index].position = new Vector3(-0.89f, ballStartPos.y, ballStartPos.z);
			}
			else
			{
				simulatedBalls[index].position = new Vector3(-0.7f, ballStartPos.y, ballStartPos.z);
			}
		}
		else if (bowlerHand == "left")
		{
			simulatedBalls[index].position = new Vector3(0.7f, ballStartPos.y, ballStartPos.z);
		}
		else
		{
			simulatedBalls[index].position = new Vector3(0.91f, ballStartPos.y, ballStartPos.z);
		}
	}

	private void Update()
	{
		if (!beginSimulation)
		{
			return;
		}
		if (Input.GetMouseButtonDown(0))
		{
			BallSimulationSkipped();
		}
		if (simulatedBalls[simBallIndex].transform.position.z <= 9.95f)
		{
			BowlingBallMovement(simBallIndex);
		}
		else
		{
			simBallIndex++;
			if (simBallIndex > totalSetBallData - 1)
			{
				beginSimulation = false;
			}
		}
		Singleton<GameData>.instance.BlinkActionReplay();
	}

	public void BowlingBallMovement(int ballIndex)
	{
		BallMovement(ballIndex);
		BallSwingMovement(ballIndex);
		if (!(ballArcAngles[ballIndex] >= 360f))
		{
			return;
		}
		ballBounceCounts[ballIndex]++;
		ballArcAngles[ballIndex] = 180f;
		ballAnglePerSecond[ballIndex] *= 1.1f;
		ballHeights[ballIndex] *= 0.6f;
		spinSpeedX = UnityEngine.Random.Range(-3600, -1800);
		spinSpeedZ = UnityEngine.Random.Range(-3600, -1800);
		if (ballBounceCounts[ballIndex] == 1)
		{
			if (bowlerType == "spin")
			{
				ballAngles[ballIndex] += ballSpinValues[ballIndex];
			}
			else if (bowlerType == "fast" && ballSwingValues[ballIndex] != 0f)
			{
				ballAngles[ballIndex] += ballSpinValues[ballIndex];
				ballSwingValues[ballIndex] = 0f;
			}
			else if (bowlerType == "medium" && ballSwingValues[ballIndex] != 0f)
			{
				ballAngles[ballIndex] += ballSpinValues[ballIndex];
				ballSwingValues[ballIndex] = 0f;
			}
		}
	}

	public void SetProjectileAnglePerSecond(int ballIndex)
	{
		ballAnglePerSecond[ballIndex] = 90f / ballTravelDistances[ballIndex] * ballHorizontalSpeeds[ballIndex];
		swingAnglePerSecond[ballIndex] = 180f / ballTravelDistances[ballIndex] * ballHorizontalSpeeds[ballIndex];
	}

	public void BallMovement(int ballIndex)
	{
		float x = Mathf.Cos(ballAngles[ballIndex] * degToRad) * ballHorizontalSpeeds[ballIndex] * Time.deltaTime;
		float z = Mathf.Sin(ballAngles[ballIndex] * degToRad) * ballHorizontalSpeeds[ballIndex] * Time.deltaTime;
		float num = Mathf.Sin(ballArcAngles[ballIndex] * degToRad) * ballHeights[ballIndex] - ballSize;
		if (float.IsNaN(num))
		{
			num = 0f;
		}
		simulatedBalls[ballIndex].position = new Vector3(simulatedBalls[ballIndex].position.x, 0f - num, simulatedBalls[ballIndex].position.z);
		simulatedBalls[ballIndex].position += new Vector3(x, 0f, z);
		ballArcAngles[ballIndex] += ballAnglePerSecond[ballIndex] * Time.deltaTime;
		simulatedBalls[ballIndex].Rotate(Vector3.right * Time.deltaTime * spinSpeedX, Space.World);
		simulatedBalls[ballIndex].Rotate(Vector3.forward * Time.deltaTime * spinSpeedZ, Space.World);
	}

	private void BallSwingMovement(int ballIndex)
	{
		if (ballSwingValues[ballIndex] != 0f)
		{
			float x = Mathf.Cos(swingAngles[ballIndex] * degToRad) * ballSwingValues[ballIndex] * Time.deltaTime;
			swingAngles[ballIndex] += swingAnglePerSecond[ballIndex] * Time.deltaTime;
			simulatedBalls[ballIndex].position -= new Vector3(x, 0f, 0f);
		}
	}
}

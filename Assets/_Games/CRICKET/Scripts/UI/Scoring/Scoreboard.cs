using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Cricket;
using DG.Tweening;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityExtensions;

public class Scoreboard : Singleton<Scoreboard>
{
	private GroundController groundControllerScript;

	public GameObject scoreBoard;

	public GameObject SOInfoTab;

	public GameObject toHide;

	public GameObject testMatchInfo;

	public Image LogoFlag;

	public Text LogoText;

	public Text SOInfoText;

	public Text PowerplayText;

	public Button pauseBtn;

	public Button SOPauseBtn;

	public Text ScoreTxt;

	public Text BowlerToBatsmanText;

	public Text OverTxt;

	public GameObject TargetBG;

	public Text TargetTxt;

	public Text StrikerName;

	private string[] LevelDescriptionArray = new string[18]
	{
			LocalizationData.localizationInstance.getText(278),
		LocalizationData.localizationInstance.getText(279),
		LocalizationData.localizationInstance.getText(281),
		LocalizationData.localizationInstance.getText(280),
		LocalizationData.localizationInstance.getText(279),
		LocalizationData.localizationInstance.getText(278),
		LocalizationData.localizationInstance.getText(282),
		LocalizationData.localizationInstance.getText(279),
		LocalizationData.localizationInstance.getText(280),
		LocalizationData.localizationInstance.getText(527),
		LocalizationData.localizationInstance.getText(527),
		LocalizationData.localizationInstance.getText(527),
		LocalizationData.localizationInstance.getText(527),
		LocalizationData.localizationInstance.getText(527),
		LocalizationData.localizationInstance.getText(527),
		LocalizationData.localizationInstance.getText(527),
		LocalizationData.localizationInstance.getText(527),
		LocalizationData.localizationInstance.getText(527)
	};

	public Text NonStrikerName;

	public string str = string.Empty;

	public GameObject StripBG;

	public Text StripTxt;

	public GameObject BallInfo;

	public List<Text> BallInfoList;

	public List<Text> BallExtras;

	public GameObject freeHitGO;

	public ScrollRect ballScroll;

	private Vector3 pausePos;

	private Vector2 pauseSize;

	private Transform _transform;

	private bool canShowTargetBg;

	public Transform tutorialBtn;

	public Transform[] tutorialBG;

	private bool theRingIsFired;

	//Photon Removal	public PhotonView photonView;

	int MaxRetries = 2;

	float RetryDelay = 0.1f;

	public GameObject multiplayerPausePanel;
	protected void Awake()
	{
		if (CONTROLLER.PlayModeSelected == 4)
		{
		}
		//else if (CONTROLLER.PlayModeSelected == 5)
		//{
		//	CTTargetToWin();
		//}
		else
		{
			UpdateSOTabText(string.Empty);
		}
		groundControllerScript = GameObject.Find("GroundController").GetComponent<GroundController>();
		Hide(boolean: true);
		showFreeHitBg(canShow: false);
	}

	public void HideScoreBoard()
	{
		toHide.transform.DOLocalMoveY(-250f, 1f).SetUpdate(isIndependentUpdate: true);
	}

	public void ShowScoreBoard()
	{
		toHide.transform.DOLocalMoveY(0f, 0.5f).SetUpdate(isIndependentUpdate: true);
	}

	public void UpdateSOTabText(string str)
	{
		SOInfoText.text = str;
		if (str != string.Empty)
		{
			SOInfoTab.SetActive(value: true);
		}
		else
		{
			SOInfoTab.SetActive(value: false);
		}
	}

	public void ShowChallengeTitle()
	{
		if (CONTROLLER.SuperOverMode == "bat")
		{
			UpdateSOTabText(LevelDescriptionArray[(int)Mathf.Floor(CONTROLLER.LevelId / 2)]);
		}
		else
		{
			UpdateSOTabText(LevelDescriptionArray[CONTROLLER.LevelId + 9]);
		}
	}

	public void pauseGame()
	{
		if (pauseBtn.gameObject.activeInHierarchy && pauseBtn.enabled)
		{
			Singleton<PauseGameScreen>.instance.Hide(boolean: false);
			Singleton<GameData>.instance.GamePaused(boolean: true);
			GameConstants.isWithAI.Show();

			if (GameConstants.isWithAI == false)
			{
				multiplayerPausePanel.SetActive(true);
				if (GameConstants.isWithAI == false)
				{                                                                                                                                                                      //Photon Removal
																																													   //photonView.RPC("RPC_PauseGame", RpcTarget.OthersBuffered);
					CricketNetworkManager.instance.CmdPauseGame(staticVariables.UserProfiledata.user._id);
				}


			}
		}
	}

	public void multiplayerLeaved()
	{
		MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
		if (NetworkGameManager.Instance)
			NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
		this.Delay(1, () => NetworkManager.singleton.StopClient());
		SceneManager.LoadScene("Home");
		//Photon Removal	PhotonNetwork.LeaveRoom();
	}
	IEnumerator SendRPCWithRetry()
	{
		int retries = 0;
		while (retries < MaxRetries)
		{
			if (GameConstants.isWithAI == false)
			{
				//Photon Removal       photonView.RPC("RPC_PauseGame", RpcTarget.OthersBuffered);
				CricketNetworkManager.instance.CmdPauseGame(staticVariables.UserProfiledata.user._id);
			}


			yield return new WaitForSeconds(RetryDelay);
			retries++;
		}

		ConstantsData_M.Log("Failed to send acknowledgement after all retries.");
	}

	//Photon Removal   [PunRPC]
	public void RPC_PauseGame()
	{
		if (pauseBtn.gameObject.activeInHierarchy && pauseBtn.enabled)
		{
			Singleton<PauseGameScreen>.instance.Hide(boolean: false);
			Singleton<GameData>.instance.GamePaused(boolean: true);
		}
	}

	public void CTTargetToWin()
	{
		int num = CONTROLLER.TargetToChase - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores;
		int num2 = CONTROLLER.totalOvers * 6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls;
		int num3 = CONTROLLER.totalWickets - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchWickets;
		string text = LocalizationData.localizationInstance.getText(528);
		UpdateSOTabText(text);
	}

	public void showFreeHitBg(bool canShow)
	{
		if (canShow)
		{
			TargetBG.SetActive(value: false);
			freeHitGO.SetActive(value: true);
			Singleton<GroundController>.instance.freeHit = true;
			return;
		}
		if (canShowTargetBg)
		{
			TargetBG.SetActive(value: true);
		}
		else
		{
			TargetBG.SetActive(value: false);
		}
		freeHitGO.SetActive(value: false);
		Singleton<GroundController>.instance.freeHit = false;
	}

	public string ReplaceText(string original, string replace1, string replace2)
	{
		string text = string.Empty;
		//Debug.Log(original + " " + replace1 + " " + replace2 + " " + text);
		if (original.Contains("#"))
		{
			text = original.Replace("#", replace1);
		}
		//Debug.Log(original + " " + replace1 + " " + replace2 + " " + text);
		if (text.Contains("$"))
		{
			text = text.Replace("$", replace2);
		}
		//Debug.Log(original + " " + replace1 + " " + replace2 + " " + text);
		return text;
	}

	public void UpdateScoreCard()
	{
		if (CONTROLLER.PlayModeSelected == 7)
		{
			//testMatchInfo.SetActive(value: true);
			//if (CONTROLLER.currentInnings < 2)
			//{
			//	testMatchInfo.GetComponentInChildren<Text>().text = LocalizationData.instance.getText(465);
			//	testMatchInfo.GetComponentInChildren<Text>().text = ReplaceText(testMatchInfo.GetComponentInChildren<Text>().text, CONTROLLER.currentDay.ToString(), "1");
			//}
			//else
			//{
			//	testMatchInfo.GetComponentInChildren<Text>().text = LocalizationData.instance.getText(465);
			//	testMatchInfo.GetComponentInChildren<Text>().text = ReplaceText(testMatchInfo.GetComponentInChildren<Text>().text, CONTROLLER.currentDay.ToString(), "2");
			//}
			//UpdateBowlerToBatsmanLabel();
			//int num;
			//int num2;
			//if (CONTROLLER.currentInnings < 2)
			//{
			//	ScoreTxt.text = CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchScores1 + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchWickets1;
			//	num = CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchBalls1 / 6;
			//	num2 = CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchBalls1 % 6;
			//}
			//else
			//{
			//	ScoreTxt.text = CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchScores2 + "/" + CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchWickets2;
			//	num = CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchBalls2 / 6;
			//	num2 = CONTROLLER.TeamList[CONTROLLER.BattingTeam[CONTROLLER.currentInnings]].TMcurrentMatchBalls2 % 6;
			//}
			//string text = num + "." + num2 + " (" + CONTROLLER.totalOvers.ToString() + ")";
			//OverTxt.text = text;
		}
		else
		{
			testMatchInfo.SetActive(value: false);
			BowlerToBatsmanText.text = string.Empty;
			ScoreTxt.text = Singleton<GameData>.instance.scoreDisplayString;
			OverTxt.text = Singleton<GameData>.instance.oversDisplayString;
		}
		string abbrevation = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation;
		Sprite[] flags = Singleton<FlagHolderGround>.instance.flags;
		foreach (Sprite sprite in flags)
		{
			if (sprite.name.ToUpper() == abbrevation)
			{
				LogoFlag.sprite = sprite;
			}
		}
		LogoText.text = abbrevation;
		string text2;
		int num3;
		int num4;
		if (CONTROLLER.StrikerIndex >= 0 && CONTROLLER.StrikerIndex < CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList.Length)
		{
			text2 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].ScoreboardName;

			{
				num3 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.RunsScored;
				num4 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].BatsmanList.BallsPlayed;
			}
		}
		else
		{
			text2 = string.Empty;
			num3 = 0;
			num4 = 0;
		}
		if (text2.Length > 10)
		{
			text2 = text2.Substring(0, 10);
		}
		StrikerName.text = text2 + "* " + num3 + "(" + num4 + ")";
		if (CONTROLLER.NonStrikerIndex >= 0 && CONTROLLER.NonStrikerIndex < CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList.Length)
		{
			text2 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].ScoreboardName;

			{
				num3 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.RunsScored;
				num4 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.NonStrikerIndex].BatsmanList.BallsPlayed;
			}
		}
		else
		{
			text2 = string.Empty;
			num3 = 0;
			num4 = 0;
		}
		if (text2.Length > 10)
		{
			text2 = text2.Substring(0, 10);
		}
		Text strikerName = StrikerName;
		string text3 = strikerName.text;
		strikerName.text = text3 + "\t\t" + text2 + " " + num3 + "(" + num4 + ")";
		NonStrikerName.text = string.Empty;
		string[] array = Singleton<ScoreBoardBallList>.instance.ballList.ToArray();
		string[] array2 = Singleton<ScoreBoardBallList>.instance.extras.ToArray();
		if (array.Length > 6)
		{
			ballScroll.content.pivot = new Vector2(1f, 0.5f);
			if (array.Length > 14)
			{
				for (int j = 0; j < array.Length - BallInfoList.Count; j++)
				{
					GameObject gameObject = Object.Instantiate(BallInfo);
					// SetParent(..., worldPositionStays:false), NOT `transform.parent =`. The assignment form
					// KEEPS THE WORLD POSITION, so a chip instantiated from the prefab stayed wherever the
					// prefab sat and was never pulled into the strip's layout — it rendered loose on the
					// canvas, on top of the player-name rows, carrying its "nb"/"wd" extra label with it.
					// This branch only runs once the over's history passes 14 chips, which is exactly the
					// tester's "history m MAXIMUM balls a jati hain PHIR players k name k upr show hoti hai".
					// With worldPositionStays:false the chip keeps the prefab's LOCAL transform and the
					// strip's layout places it like every pre-built chip.
					Transform _strip = BallInfoList[0].transform.parent.transform.parent;
					gameObject.transform.SetParent(_strip, false);
					gameObject.transform.localScale = Vector3.one;
					gameObject.transform.localRotation = Quaternion.identity;
					BallInfoList.Add(gameObject.transform.GetChild(1).GetComponent<Text>());
					BallExtras.Add(gameObject.transform.GetChild(0).GetComponent<Text>());
					ConstantsData_M.MpLog($"[Scoreboard] Ball-chip {BallInfoList.Count - 1} created for a {array.Length}-ball over and parented into the strip layout.");
				}
			}
			for (int j = 0; j < BallInfoList.Count; j++)
			{
				if (j < array.Length)
				{
					BallInfoList[j].transform.parent.gameObject.SetActive(value: true);
					BallInfoList[j].gameObject.SetActive(value: true);
					BallInfoList[j].text = array[j];
				}
				else
				{
					BallInfoList[j].transform.parent.gameObject.SetActive(value: false);
					BallInfoList[j].gameObject.SetActive(value: false);
				}
			}
		}
		else
		{
			ballScroll.content.pivot = new Vector2(0f, 0.5f);
			for (int j = BallInfoList.Count - 1; j >= 0; j--)
			{
				if (j < 15)
				{
					if (j < array.Length)
					{
						BallInfoList[j].transform.parent.gameObject.SetActive(value: true);
						BallInfoList[j].gameObject.SetActive(value: true);
						BallInfoList[j].text = array[j];
					}
					else
					{
						BallInfoList[j].transform.parent.gameObject.SetActive(value: false);
					}
				}
				else
				{
					Object.Destroy(BallInfoList[j].transform.parent.gameObject);
					BallInfoList.RemoveAt(j);
					// BallExtras is the PARALLEL list (same chip, the little nb/wd label). It was grown
					// alongside BallInfoList when chips are created but never shrunk here, so once this
					// ran the two lists were permanently misaligned by however many chips were destroyed
					// — and the extras write below indexes BY CHIP NUMBER, so a no-ball's "nb" landed on
					// whatever Text now sat at that index. That is the tester's "no balls were showing on
					// the names of players".
					if (j < BallExtras.Count) BallExtras.RemoveAt(j);
				}
			}
		}
		BallInfoList.TrimExcess();
		array = null;
		// Iterate BallExtras' OWN count — using BallInfoList.Count here threw IndexOutOfRange whenever the
		// two lists disagreed (see the RemoveAt above), and a throw in the middle of UpdateScoreCard leaves
		// the scorecard half-painted.
		for (int j = 0; j < BallExtras.Count; j++)
		{
			BallExtras[j].text = " ";
		}
		for (int j = 0; j < array2.Length; j += 2)
		{
			// extras stores the CHIP INDEX (ballCount - 1) as text. Two no-balls in one over push the chip
			// count past the six pre-built slots, so this index can legitimately exceed BallExtras — and it
			// was indexed blind, taking the whole scorecard update down with it. Skip + log instead.
			if (!int.TryParse(array2[j], out int _chipIdx) || _chipIdx < 0 || _chipIdx >= BallExtras.Count)
			{
				ConstantsData_M.MpLog($"[Scoreboard] Extra label '{array2[j + 1]}' skipped — chip index '{array2[j]}' is outside BallExtras (count={BallExtras.Count}, chips={Singleton<ScoreBoardBallList>.instance.ballList.Count}).");
				continue;
			}
			BallExtras[_chipIdx].text = array2[j + 1];
		}
		BallExtras.TrimExcess();
		array2 = null;
		if (CONTROLLER.PowerPlay)
		{
			PowerplayText.gameObject.SetActive(value: true);
		}
		else
		{
			PowerplayText.gameObject.SetActive(value: false);
		}
		//if (CONTROLLER.PlayModeSelected == 4 || CONTROLLER.PlayModeSelected == 5)
		//{
		//	Singleton<SuperOverScoreboard>.instance.UpdateScoreboard();
		//}
	}

	public void ShowTargetScreen(bool boolean)
	{
		if (boolean)
		{
			canShowTargetBg = true;
			TargetBG.SetActive(value: true);
			TargetTxt.text = string.Empty + CONTROLLER.TargetToChase;
		}
		else
		{
			canShowTargetBg = false;
			TargetTxt.text = string.Empty;
			TargetBG.SetActive(value: false);
		}
	}

	public void UpdateStripText(string str)
	{
		if (CONTROLLER.PlayModeSelected == 7)
		{
			//StripTxt.text = string.Empty;
		}
		else
		{
			StripTxt.text = str;
		}
	}

	public void HideStrip(bool boolean)
	{
		UpdateStripText(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation + " Vs " + CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].abbrevation);
		if (!boolean)
		{
			StripBG.SetActive(value: true);
		}
	}

	public void BowlerToBatsman()
	{
		string playerName = CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].PlayerList[CONTROLLER.CurrentBowlerIndex].PlayerName;
		string playerName2 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].PlayerName;
		string text = playerName + " to " + playerName2;
		UpdateStripText(text);
	}

	public void TargetToWin()
	{
		//if (CONTROLLER.PlayModeSelected == 7)
		//{
		//	UpdateStripText(string.Empty);
		//	return;
		//}
		int num = CONTROLLER.TargetToChase - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores;
		int num2 = CONTROLLER.Overs[CONTROLLER.oversSelectedIndex] * 6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls;
		string text = "Need " + num + " from " + num2 + " balls";
		CONTROLLER.RequiredRun = num;
		UpdateStripText(text);
	}

	public void NewOver()
	{
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[0] = string.Empty;
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[1] = string.Empty;
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[2] = string.Empty;
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[3] = string.Empty;
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[4] = string.Empty;
		CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].ballUpdate[5] = string.Empty;
		Singleton<ScoreBoardBallList>.instance.ResetBallList();
	}

	public void Hide(bool boolean)
	{
		if (boolean)
		{
			Singleton<BattingControls>.instance.battingMeter.SetActive(value: false);
			Singleton<BattingControls>.instance.GreedyAdsImage.SetActive(value: true);
			if (CONTROLLER.PlayModeSelected != 6)
			{
				Screen.sleepTimeout = -2;
			}
			//if (CONTROLLER.PlayModeSelected == 4 || CONTROLLER.PlayModeSelected == 5)
			//{
			//	Singleton<SuperOverScoreboard>.instance.holder.SetActive(value: false);
			//}
			scoreBoard.SetActive(value: false);
			HidePause(boolean: true);
		}
		else
		{
			Singleton<NavigationBack>.instance.deviceBack = pauseGame;
			CONTROLLER.pageName = "Ground";
			Screen.sleepTimeout = -1;
			//if (CONTROLLER.receivedAdEvent)
			//{
			//	Singleton<AdIntegrate>.instance.HideAd();
			//}
			RealignUIObjects();
			scoreBoard.SetActive(value: true);
			HidePause(boolean: false);
		}
	}

	private void RealignUIObjects()
	{
		if (CONTROLLER.PlayModeSelected == 4 || CONTROLLER.PlayModeSelected == 5)
		{
			//Singleton<SuperOverScoreboard>.instance.holder.SetActive(value: true);
			//toHide.SetActive(value: false);
			//tutorialBtn.localPosition = new Vector3(tutorialBtn.localPosition.x, -269f, tutorialBtn.localPosition.z);
		}
		else
		{
			tutorialBtn.localPosition = new Vector3(tutorialBtn.localPosition.x, -314f, tutorialBtn.localPosition.z);
		}
		//if (CONTROLLER.PlayModeSelected == 7)
		//{
		//	Transform[] array = tutorialBG;
		//	foreach (Transform transform in array)
		//	{
		//		transform.localPosition = new Vector3(transform.localPosition.x, -196f, transform.localPosition.z);
		//	}
		//}
	}

	private void OpenPause()
	{
		Singleton<PauseGameScreen>.instance.Hide(boolean: false);
	}

	public void HidePause(bool boolean)
	{
		if (boolean)
		{
			pauseBtn.gameObject.SetActive(value: false);
		}
		else
		{

			pauseBtn.gameObject.SetActive(value: true);
		}
	}

	public void UpdateBowlerToBatsmanLabel()
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i <= CONTROLLER.currentInnings; i++)
		{
			if (i < 2)
			{
				if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
				{
					if (CONTROLLER.BattingTeam[i] == CONTROLLER.myTeamIndex)
					{
						num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores1;
					}
					else
					{
						num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores1;
					}
				}
				else if (CONTROLLER.BattingTeam[i] == CONTROLLER.myTeamIndex)
				{
					num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores1;
				}
				else
				{
					num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores1;
				}
			}
			else if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
			{
				if (CONTROLLER.BattingTeam[i] == CONTROLLER.myTeamIndex)
				{
					num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores2;
				}
				else
				{
					num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores2;
				}
			}
			else if (CONTROLLER.BattingTeam[i] == CONTROLLER.myTeamIndex)
			{
				num2 += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores2;
			}
			else
			{
				num += CONTROLLER.TeamList[CONTROLLER.BattingTeam[i]].TMcurrentMatchScores2;
			}
		}
		int num3 = num - num2;
		string abbrevation = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].abbrevation;
		if (num3 > 0)
		{
			if (CONTROLLER.currentInnings != 3)
			{
				str = abbrevation + " lead by " + Mathf.Abs(num3) + " runs.";
			}
			else if (CONTROLLER.TargetToChase + 1 > 1)
			{
				str = abbrevation + " need " + (Mathf.Abs(num3) + 1) + " runs to win.";
			}
			else
			{
				str = abbrevation + " need " + (Mathf.Abs(num3) + 1) + " run to win.";
			}
		}
		else if (num3 < 0)
		{
			if (CONTROLLER.currentInnings != 3)
			{
				str = abbrevation + " trail by " + Mathf.Abs(num3) + " runs.";
			}
			else if (CONTROLLER.TargetToChase + 1 > 1)
			{
				str = abbrevation + " need " + (Mathf.Abs(num3) + 1) + " runs to win.";
			}
			else
			{
				str = abbrevation + " need " + (Mathf.Abs(num3) + 1) + " run to win.";
			}
		}
		else if (num3 == 0)
		{
			if (CONTROLLER.currentInnings != 3)
			{
				str = "SCORES LEVEL";
			}
			else
			{
				str = abbrevation + " need " + (Mathf.Abs(num3) + 1) + " runs to win.";
			}
		}
		int num4 = 0;
		int num5 = 0;
		string empty = string.Empty;
		string empty2 = string.Empty;
		int num6 = 0;
		int num7 = 0;
		if (CONTROLLER.currentInnings == 0)
		{
			string empty3 = string.Empty;
			string empty4 = string.Empty;
			num4 = Random.Range(0, 10);
			empty3 = CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation;
			empty4 = CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation;
			if (num4 > 2 && num4 < 7)
			{
				num5 = CONTROLLER.BowlingTeamIndex;
				empty = CONTROLLER.TeamList[num5].PlayerList[CONTROLLER.CurrentBowlerIndex].PlayerName;
				empty2 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].PlayerName;
				BowlerToBatsmanText.text = empty + " to " + empty2;
			}
			else if (CONTROLLER.currentSession > 1)
			{
				if (num4 < 3)
				{
					num6 = CONTROLLER.totalOvers - 1 - CONTROLLER.ballsBowledPerDay / 6;
					num7 = ((CONTROLLER.currentInnings >= 2) ? (6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls2 % 6) : (6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls1 % 6));
					if (num6 > 0)
					{
						if (num7 == 6)
						{
							BowlerToBatsmanText.text = LocalizationData.localizationInstance.getText(529) + " " + (num6 + 1);
							return;
						}
						BowlerToBatsmanText.text = LocalizationData.localizationInstance.getText(529) + " " + num6 + "." + num7;
					}
					else
					{
						BowlerToBatsmanText.text = LocalizationData.localizationInstance.getText(530) + " " + num7;
					}
				}
				else
				{
					BowlerToBatsmanText.text = empty3 + " vs " + empty4;
				}
			}
			else
			{
				BowlerToBatsmanText.text = empty3 + " vs " + empty4;
			}
		}
		else if (CONTROLLER.currentInnings == 1 || CONTROLLER.currentInnings == 2)
		{
			num4 = Random.Range(0, 10);
			if (num4 < 2)
			{
				num5 = CONTROLLER.BowlingTeamIndex;
				empty = CONTROLLER.TeamList[num5].PlayerList[CONTROLLER.CurrentBowlerIndex].PlayerName;
				empty2 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].PlayerName;
				BowlerToBatsmanText.text = empty + " to " + empty2;
			}
			else if (CONTROLLER.currentSession > 1)
			{
				if (num4 > 1 && num4 < 5)
				{
					num6 = CONTROLLER.totalOvers - 1 - CONTROLLER.ballsBowledPerDay / 6;
					num7 = ((CONTROLLER.currentInnings >= 2) ? (6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls2 % 6) : (6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls1 % 6));
					if (num6 > 0)
					{
						if (num7 == 6)
						{
							BowlerToBatsmanText.text = LocalizationData.localizationInstance.getText(529) + " " + (num6 + 1);
							return;
						}
						BowlerToBatsmanText.text = LocalizationData.localizationInstance.getText(529) + " " + num6 + "." + num7;
					}
					else
					{
						BowlerToBatsmanText.text = LocalizationData.localizationInstance.getText(530) + " " + num7;
					}
				}
				else
				{
					BowlerToBatsmanText.text = str;
				}
			}
			else
			{
				BowlerToBatsmanText.text = str;
			}
		}
		else
		{
			if (CONTROLLER.currentInnings != 3)
			{
				return;
			}
			num4 = Random.Range(0, 10);
			if (num4 == 0)
			{
				num5 = CONTROLLER.BowlingTeamIndex;
				empty = CONTROLLER.TeamList[num5].PlayerList[CONTROLLER.CurrentBowlerIndex].PlayerName;
				empty2 = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].PlayerList[CONTROLLER.StrikerIndex].PlayerName;
				BowlerToBatsmanText.text = empty + " to " + empty2;
			}
			else if (CONTROLLER.currentSession > 1)
			{
				if (num4 > 0 && num4 < 4)
				{
					num6 = CONTROLLER.totalOvers - 1 - CONTROLLER.ballsBowledPerDay / 6;
					num7 = ((CONTROLLER.currentInnings >= 2) ? (6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls2 % 6) : (6 - CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].TMcurrentMatchBalls1 % 6));
					if (num6 > 0)
					{
						if (num7 == 6)
						{
							BowlerToBatsmanText.text = LocalizationData.localizationInstance.getText(529) + " " + (num6 + 1);
							return;
						}
						BowlerToBatsmanText.text = LocalizationData.localizationInstance.getText(529) + " " + num6 + "." + num7;
					}
					else
					{
						BowlerToBatsmanText.text = LocalizationData.localizationInstance.getText(530) + " " + num7;
					}
				}
				else
				{
					BowlerToBatsmanText.text = str;
				}
			}
			else
			{
				BowlerToBatsmanText.text = str;
			}
		}
	}

	public Vector3 GetPausePos()
	{
		return pausePos;
	}

	public Vector2 GetPauseSize()
	{
		return pauseSize;
	}

	// The rebowl / review-overturn undo paths in GameData all hid the last ball chip by indexing
	// BallInfoList[Count - 1] blind. The list is rebuilt from ballList, which ResetBallList() empties at every
	// over rollover and on the scene reload a reconnect performs — so a rebowl on the FIRST ball of an over
	// threw ArgumentOutOfRange before the caller could reach CheckForOverComplete(), and the over never
	// completed. Same freeze as the ScoreBoardBallList undo helpers, same fix, in one place.
	public void HideLastBallInfo()
	{
		if (BallInfoList == null || BallInfoList.Count == 0)
		{
			ConstantsData_M.MpLog("[Scoreboard] HideLastBallInfo skipped — no ball chips to hide (list was reset by an over rollover or a reconnect).");
			return;
		}
		BallInfoList[BallInfoList.Count - 1].gameObject.SetActive(value: false);
	}

}

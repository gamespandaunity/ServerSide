
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cricket;

public class ScoreBoardBallList : Singleton<ScoreBoardBallList>
{
	public List<string> ballList;

	public List<string> extras;

	public int ballCount;

	//Photon Removal	public PhotonView photonView;

	private string Score;

	private string ExtraInfo;

	private void Awake()
	{
		ResetBallList();
		ballCount = 0;
		Score = string.Empty;
		ExtraInfo = string.Empty;
	}

	public void ResetBallList()
	{
		ballList.Clear();
		extras.Clear();
		ballList.TrimExcess();
		extras.TrimExcess();
		ballCount = 0;
	}

	public void AddBall(string score, string extraInfo)
	{
		//print("&**ADDEDDD");
		if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
		{
			//photonView.RPC("RPC_ChangeBallData", RpcTarget.OthersBuffered, score, extraInfo);
		}
		Score = score;
		ExtraInfo = extraInfo;
		ballCount++;
		ballList.Add(score);
		if (extraInfo != " ")
		{
			// Index the extra by its position in ballList, NOT by ballCount. Scoreboard renders the strip from
			// ballList.ToArray() and writes each extra label into BallExtras[thatIndex], so the two must agree.
			// They do not on the BOWLING client: RpcCorrectBallState force-assigns
			// `ScoreBoardBallList.ballCount = <authority value>` without touching ballList, so from the next
			// delivery onward ballCount runs ahead of the list by however far they had drifted, and every
			// "nb"/"wd" label lands that many rows away from its own chip — onto the player-name rows.
			// The 03-08 pair log shows the drift plainly: "ballCount=7 ballNum=4", "ballCount=8 ballNum=5".
			// ballList.Count is the only index the renderer will ever agree with.
			extras.Add((ballList.Count - 1).ToString());
			extras.Add(extraInfo);
		}
		// The over strip's nb/wd label is only ever written when AddBall is handed a real extraInfo, and
		// nothing logged what it actually received — so "WD/NB strip pe nahi likha" could not be told apart
		// from "the label was written and is invisible". The scene wiring is confirmed good (every chip has
		// an active 'extra' Text, correctly paired with its own chip), so this line is the remaining unknown.
		// One line per delivery.
		if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
			ConstantsData_M.MpLog($"[ScoreBoardBallList] Chip {ballList.Count - 1} added: score='{score}' extra='{extraInfo}' (extras entries now {(extras != null ? extras.Count : -1)}).");

		if (ballCount != ballList.Count)
			ConstantsData_M.MpLog($"[ScoreBoardBallList] ballCount={ballCount} has drifted from ballList={ballList.Count} — an authoritative correction set the count without the chips.");

		if (CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
		{
			//CheckSyncForMultiplayer(score, extraInfo);
		}
	}

	public void MultiplayerSendData()
	{
		if (GameConstants.isWithAI == false)
		{
			//print("CHECK CALLED");
			//Photon Removal      photonView.RPC("RPC_ChangeBallData", RpcTarget.OthersBuffered, Score, ExtraInfo);
			CricketNetworkManager.instance.CmdChangeBallData(staticVariables.UserProfiledata.user._id, Score, ExtraInfo);
		}


	}

	//Photon Removal  [PunRPC]
	public void RPC_ChangeBallData(string SCORE, string EXTRAINFO)
	{
		//print("CHECK CALLED");
		CONTROLLER.SCORE = SCORE;
		CONTROLLER.EXTRAINFO = EXTRAINFO;
		CheckSyncForMultiplayer(Score, ExtraInfo);
	}

	private void CheckSyncForMultiplayer(string SCORE, string EXTRAINFO)
	{
		//if (!(SCORE.Equals(CONTROLLER.SCORE) && EXTRAINFO.Equals(CONTROLLER.EXTRAINFO) ))
		if (false)
		{
			ConstantsData_M.Log("Score Not Equal ---->");
			StartCoroutine(RebowlWithRetries());
			CONTROLLER.OnWaitScreen = 0;
			Singleton<GameData>.instance.RebowlLastBallMultiplayer();
		}
		else
		{
			print("NOT REBOWL");
			if (!Launcher.Instance.IsReConnecting())
			{
				//Photon Removal   photonView.RPC("RPC_CallCheckForOverComplete", RpcTarget.OthersBuffered);
				CricketNetworkManager.instance.CmdCallCheckForOverComplete(staticVariables.UserProfiledata.user._id);

				Invoke("CallCheckOverComplete", 1f);
			}
		}
	}

	private void CallCheckOverComplete()
	{
		if (Launcher.Instance.IsReConnecting())
		{
			return;
		}
		if (CONTROLLER.OnWaitScreen == 0)
		{
			return; // Already processed by RPC_SendOppAck
		}
		//print("CALLL YESS");
		CONTROLLER.OnWaitScreen = 0;
		if (Singleton<GameData>.instance.currentBallNumber != 5)
		{
		}

		Singleton<GameData>.instance.CheckForOverComplete();
	}

	//Photon Removal	[PunRPC]
	public void RPC_ReBowlLastBall()
	{
		if (Launcher.Instance.IsReConnecting())
		{
			return;
		}
		CONTROLLER.OnWaitScreen = 0;
		ConstantsData_M.Log("Batsman Anim :- " + Singleton<GroundController>.instance.currentBatsmanAnimation);
		ConstantsData_M.Log("Collider :- " + CONTROLLER.CURRENTCOLLIDER);
		ConstantsData_M.Log("Ball Angle :- " + CONTROLLER.BALLANGLE);
		ConstantsData_M.Log("Horizontal Speed :- " + CONTROLLER.HORIZONTALSPEED);
		Singleton<GameData>.instance.RebowlLastBallMultiplayer();
	}

	private IEnumerator RebowlWithRetries()
	{
		int retryCount = 0;
		const int maxRetries = 5;
		while (retryCount < maxRetries)
		{
			if (CONTROLLER.OnWaitScreen == 0)
			{
				yield break;
			}
			if (!Launcher.Instance.IsReConnecting())
			{
				if (GameConstants.isWithAI == false)
				{
					//Photon Removal        photonView.RPC("RPC_ReBowlLastBall", RpcTarget.OthersBuffered);
					CricketNetworkManager.instance.CmdReBowlLastBall(staticVariables.UserProfiledata.user._id);
				}



			}

			yield return new WaitForSeconds(0.2f); // Wait for 10 seconds before retrying


			retryCount++;
		}


	}

	//Photon Removal  [PunRPC]
	public void RPC_CallCheckForOverComplete()
	{
		//print("HEREEEATTTAT");
		ConstantsData_M.Log("REBOWLL KO CALL Kiya1 ");

		Invoke("CallCheckOverComplete", 0.25f);

	}

	// Both of these are called from the REBOWL / review-overturn undo paths in GameData, and both indexed
	// blind. ResetBallList() empties the lists at the start of every over and again on the scene reload a
	// reconnect performs, so a rebowl on the FIRST ball of an over threw ArgumentOutOfRange here — and since
	// the caller runs CheckForOverComplete() AFTER these, the throw aborted the sequence and the over never
	// completed. That is a match freeze from a two-line omission. Guard and log; skipping an undo that has
	// nothing to undo is correct.
	public void RemoveLastExtra()
	{
		if (extras.Count < 2)
		{
			ConstantsData_M.MpLog($"[ScoreBoardBallList] RemoveLastExtra skipped — extras has {extras.Count} entries (list was reset by an over rollover or a reconnect). Nothing to undo.");
			return;
		}
		extras.RemoveRange(extras.Count - 2, 2);
		extras.TrimExcess();
	}

	public void RemoveLastBall()
	{
		if (ballList.Count == 0)
		{
			ConstantsData_M.MpLog("[ScoreBoardBallList] RemoveLastBall skipped — ballList is empty (list was reset by an over rollover or a reconnect). Nothing to undo.");
			return;
		}
		ballList.RemoveAt(ballList.Count - 1);
		ballList.TrimExcess();
		ballCount--;
	}

	public void SaveBallList()
	{
		AutoSave.ballListInfo = string.Empty;
		for (int i = 0; i < ballList.Count; i++)
		{
			AutoSave.ballListInfo = AutoSave.ballListInfo + ballList[i] + "|";
		}
		AutoSave.ballextras = string.Empty;
		for (int j = 0; j < extras.Count; j++)
		{
			AutoSave.ballextras = AutoSave.ballextras + extras[j] + "|";
		}
	}
}

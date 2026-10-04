using DG.Tweening;
using ExitGames.Client.Photon;
 
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class ReviewSystem : Singleton<ReviewSystem>
{
	public GameObject userReview;

	public GameObject AiReview;

	public GameObject yesBtn;

	public GameObject noBtn;

	public Text timerText;

	public Text AiReviewText;

	public Text reviewsLeftText;

	public Text yesBtnText;

	public Text countryName;

	public Image timeFill;

	public Image AiFlag;

	private bool askedForReview;

	public int TeamIndex;

	public static bool YesButtonClicked;

    //Photon Removal	public PhotonView photonView;

    private string buttonClicked;

    private void Start()
    {
		buttonClicked = string.Empty;
    }

    public void ShowUltraEdgeReview()
	{
		if (Singleton<GroundController>.instance.canUserAskForReview)
		{
			noBtn.SetActive(value: true);
			yesBtn.SetActive(value: true);
			TeamIndex = CONTROLLER.myTeamIndex;
			AiReviewText.text = LocalizationData.localizationInstance.getText(611);
			yesBtnText.text = LocalizationData.localizationInstance.getText(164);
		}
		else if (Singleton<GroundController>.instance.canAIAskForReview)
		{
			noBtn.SetActive(value: false);
			TeamIndex = CONTROLLER.opponentTeamIndex;
            AiReviewText.text = LocalizationData.localizationInstance.getText(614);
			yesBtnText.text = LocalizationData.localizationInstance.getText(167);
			yesBtn.SetActive(value: false);
		}
        userReview.SetActive(value: true);
		Sprite[] flags = Singleton<FlagHolderGround>.instance.flags;
		foreach (Sprite sprite in flags)
		{
			if (sprite.name.ToUpper() == CONTROLLER.TeamList[TeamIndex].abbrevation)
			{
				AiFlag.sprite = sprite;
			}
		}
		countryName.text = CONTROLLER.TeamList[TeamIndex].abbrevation.ToUpper();
		reviewsLeftText.text = LocalizationData.localizationInstance.getText(613) + " " + CONTROLLER.TeamList[TeamIndex].noofDRSLeft;
	}

	public void StartTimer()
	{
		timeFill.fillAmount = 1f;
		timerText.text = "7";

		if (CONTROLLER.PlayModeSelected != 8)
		{
			DOTween.To(delegate (float x)
			{
				timerText.text = string.Empty + (int)x;
			}, 6f, 1f, 5f).OnComplete(NoBtnClicked).SetUpdate(isIndependentUpdate: true)
				.SetEase(Ease.Linear);
		}
		else
		{
			if (Singleton<GroundController>.instance.canAIAskForReview)
			{
                DOTween.To(delegate (float x)
                {
                    timerText.text = string.Empty + (int)x;
                }, 6f, 1f, 7.5f).OnComplete(test).SetUpdate(isIndependentUpdate: true)
                .SetEase(Ease.Linear);
			}
			else
			{
                ////Debug.Log("TTTEST3");

                DOTween.To(delegate (float x)
				{
					timerText.text = string.Empty + (int)x;
				}, 6f, 1f, 5f).OnComplete(NoBtnClicked).SetUpdate(isIndependentUpdate: true)
					.SetEase(Ease.Linear);
			}
        }
		

		if (Singleton<GroundController>.instance.canAIAskForReview)
		{
			AiFlag.DOFade(1f, 3f).SetUpdate(isIndependentUpdate: true).OnComplete(AiTimerComplete);
		}
		timeFill.DOFillAmount(0f, 5f).SetUpdate(isIndependentUpdate: true).SetEase(Ease.Linear);
	}

	public void YesBtnClicked()
	{
		buttonClicked = "YesBtn";
		

		if (CONTROLLER.PlayModeSelected == 8 && Singleton<GroundController>.instance.canUserAskForReview)
		{
			if (GameConstants.isWithAI == false && CricketNetworkManager.instance != null)
			{
				CricketNetworkManager.instance.CmdMultiplayerReview(staticVariables.UserProfiledata.user._id, true);
			}
		}


        ////Debug.Log("YYEESSS BUTTONN");
        CONTROLLER.TeamList[TeamIndex].noofDRSLeft--;
		Time.timeScale = 1f;
		userReview.SetActive(value: false);
		AiReview.SetActive(value: false);
		if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
		{
			// ONLINE: skip the caught-behind/UltraEdge ball-tracking replay — like the LBW DRS path it
			// cannot complete online (the bowling client suppresses physics) and repositioning the ball
			// for it corrupted the NEXT delivery into an off-course runaway-bounce that froze the match.
			// The authoritative RpcBallOutcome already stands, so resolve the review cleanly + immediately.
			Singleton<GroundController>.instance.ForceResolveStuckReview();
		}
		else
		{
			// OFFLINE: the replay has real physics and completes normally.
			CONTROLLER.canShowReplay = true;
			CONTROLLER.reviewReplay = true;
			Singleton<GameData>.instance.AnimationCompleted();
		}
		askedForReview = true;
        buttonClicked = string.Empty;
    }

	private void AiTimerComplete()
	{
		int index = LocalizationData.localizationInstance.referenceList.IndexOf(CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].teamName.ToUpper());
		string text = LocalizationData.localizationInstance.getText(index);
		AiReviewText.text = text + " " + LocalizationData.localizationInstance.getText(615);
	}


	public void NoBtnClicked()
	{
		if (CONTROLLER.PlayModeSelected == 8 && (!userReview.activeInHierarchy))
		{
			return;
		}
		buttonClicked = "NoBtn";
		if (CONTROLLER.PlayModeSelected == 8 && Singleton<GroundController>.instance.canUserAskForReview)
		{
			if (GameConstants.isWithAI == false && CricketNetworkManager.instance != null)
			{
				CricketNetworkManager.instance.CmdMultiplayerReview(staticVariables.UserProfiledata.user._id, false);
			}
		}
        if (!askedForReview && !Singleton<GroundController>.instance.canAIAskForReview && CONTROLLER.PlayModeSelected != 8)
		{
			////Debug.Log("NOOOOOOOO");
			askedForReview = true;
			Time.timeScale = 1f;
			Singleton<GroundController>.instance.isUltraEdgeCutscenePlaying = false;
			int num = 0;
			num = ((Singleton<GroundController>.instance.umpireInitialDecision == "out") ? 1 : 0);
			Singleton<GameData>.instance.UpdateCurrentBall(Singleton<GroundController>.instance.validBallCount, Singleton<GroundController>.instance.remainingBallCount, Singleton<GroundController>.instance.totalRunsScored, Singleton<GroundController>.instance.extraRuns, Singleton<GroundController>.instance.currentBatsmanID, num, Singleton<GroundController>.instance.typeOfWicket, Singleton<GroundController>.instance.bowlerId, Singleton<GroundController>.instance.catcherId, Singleton<GroundController>.instance.batsmanOutId, Singleton<GroundController>.instance.isBoundaryHit);
			userReview.SetActive(value: false);
			AiReview.SetActive(value: false);
		}
		else if (Singleton<GroundController>.instance.canAIAskForReview && CONTROLLER.PlayModeSelected!=8)
		{
			timerText.text = "0";
			YesBtnClicked();
		}
		else if(CONTROLLER.PlayModeSelected == 8 && Singleton<GroundController>.instance.canUserAskForReview)
		{
            ////Debug.Log("NOOOOOOOO");
            askedForReview = true;
            Time.timeScale = 1f;
            Singleton<GroundController>.instance.isUltraEdgeCutscenePlaying = false;
            int num = 0;
            num = ((Singleton<GroundController>.instance.umpireInitialDecision == "out") ? 1 : 0);
            Singleton<GameData>.instance.UpdateCurrentBall(Singleton<GroundController>.instance.validBallCount, Singleton<GroundController>.instance.remainingBallCount, Singleton<GroundController>.instance.totalRunsScored, Singleton<GroundController>.instance.extraRuns, Singleton<GroundController>.instance.currentBatsmanID, num, Singleton<GroundController>.instance.typeOfWicket, Singleton<GroundController>.instance.bowlerId, Singleton<GroundController>.instance.catcherId, Singleton<GroundController>.instance.batsmanOutId, Singleton<GroundController>.instance.isBoundaryHit);
            userReview.SetActive(value: false);
            AiReview.SetActive(value: false);
        }
        buttonClicked =string.Empty;
    }
    IEnumerator SendMultiplayerReviewRPC(bool tookReview)
    {
        yield return null; // Wait for next frame (optional)

        //Photon Removal if (PhotonNetwork.IsConnected)
        {
            //Photon Removal     photonView.RPC("MultiplayerReview", RpcTarget.AllBuffered, tookReview);
        }


    }

    //Photon Removal  [PunRPC]
    public void MultiplayerReview(bool TookReview)
	{
		CONTROLLER.TookReview = TookReview;
		if (TookReview)
		{
			buttonClicked = "YesBtn";
		}
		else
		{
			buttonClicked = "NoBtn";
		}
		////Debug.Log("RPCCCCC");
    }

	public void test()
	{
        //if(buttonClicked == string.Empty)
        //{
        //	test();
        //	return;
        //}
        ////Debug.Log("TTTEST2");

        if (!CONTROLLER.TookReview && Singleton<GroundController>.instance.canAIAskForReview)
		{
            ////Debug.Log("TTTEST1");
            askedForReview = true;
			Time.timeScale = 1f;
			Singleton<GroundController>.instance.isUltraEdgeCutscenePlaying = false;
			int num = 0;
			num = ((Singleton<GroundController>.instance.umpireInitialDecision == "out") ? 1 : 0);
			Singleton<GameData>.instance.UpdateCurrentBall(Singleton<GroundController>.instance.validBallCount, Singleton<GroundController>.instance.remainingBallCount, Singleton<GroundController>.instance.totalRunsScored, Singleton<GroundController>.instance.extraRuns, Singleton<GroundController>.instance.currentBatsmanID, num, Singleton<GroundController>.instance.typeOfWicket, Singleton<GroundController>.instance.bowlerId, Singleton<GroundController>.instance.catcherId, Singleton<GroundController>.instance.batsmanOutId, Singleton<GroundController>.instance.isBoundaryHit);
			userReview.SetActive(value: false);
			AiReview.SetActive(value: false);
		}
		else if (CONTROLLER.TookReview && Singleton<GroundController>.instance.canAIAskForReview)
		{
            ////Debug.Log("TTTEST2");
            CONTROLLER.TeamList[TeamIndex].noofDRSLeft--;
			Time.timeScale = 1f;
			userReview.SetActive(value: false);
			AiReview.SetActive(value: false);
			if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
			{
				// ONLINE: skip the opponent's caught-behind replay (can't complete online; corrupted the
				// next delivery). Authoritative outcome stands; resolve cleanly. (See YesBtnClicked.)
				Singleton<GroundController>.instance.ForceResolveStuckReview();
			}
			else
			{
				CONTROLLER.canShowReplay = true;
				CONTROLLER.reviewReplay = true;
				Singleton<GameData>.instance.AnimationCompleted();
			}
			askedForReview = true;
		}
	}

	public void test2()
	{
		StartCoroutine(delay());
        ////Debug.Log("TTTEST");
        //if (!CONTROLLER.TookReview)
        //{
        //    askedForReview = true;
        //    Time.timeScale = 1f;
        //    Singleton<GroundController>.instance.UltraEdgeCutscenePlaying = false;
        //    int num = 0;
        //    num = ((Singleton<GroundController>.instance.UmpireInitialDecision == "out") ? 1 : 0);
        //    Singleton<GameModel>.instance.UpdateCurrentBall(Singleton<GroundController>.instance.validBall, Singleton<GroundController>.instance.canCountBall, Singleton<GroundController>.instance.runsScored, Singleton<GroundController>.instance.extraRun, Singleton<GroundController>.instance.batsmanID, num, Singleton<GroundController>.instance.wicketType, Singleton<GroundController>.instance.bowlerID, Singleton<GroundController>.instance.catcherID, Singleton<GroundController>.instance.batsmanOut, Singleton<GroundController>.instance.isBoundary);
        //    userReview.SetActive(value: false);
        //    AiReview.SetActive(value: false);
        //}
        //else if (CONTROLLER.TookReview)
        //{
        //    CONTROLLER.TeamList[TeamIndex].noofDRSLeft--;
        //    CONTROLLER.canShowReplay = true;
        //    CONTROLLER.reviewReplay = true;
        //    Time.timeScale = 1f;
        //    userReview.SetActive(value: false);
        //    AiReview.SetActive(value: false);
        //    Singleton<GameModel>.instance.AnimationCompleted();
        //    askedForReview = true;
        //}
    }

	IEnumerator delay()
	{
		yield return new WaitForSeconds(3f);
        if (!CONTROLLER.TookReview)
        {
            askedForReview = true;
            Time.timeScale = 1f;
            Singleton<GroundController>.instance.isUltraEdgeCutscenePlaying = false;
            int num = 0;
            num = ((Singleton<GroundController>.instance.umpireInitialDecision == "out") ? 1 : 0);
            Singleton<GameData>.instance.UpdateCurrentBall(Singleton<GroundController>.instance.validBallCount, Singleton<GroundController>.instance.remainingBallCount, Singleton<GroundController>.instance.totalRunsScored, Singleton<GroundController>.instance.extraRuns, Singleton<GroundController>.instance.currentBatsmanID, num, Singleton<GroundController>.instance.typeOfWicket, Singleton<GroundController>.instance.bowlerId, Singleton<GroundController>.instance.catcherId, Singleton<GroundController>.instance.batsmanOutId, Singleton<GroundController>.instance.isBoundaryHit);
            userReview.SetActive(value: false);
            AiReview.SetActive(value: false);
        }
        else if (CONTROLLER.TookReview)
        {
            CONTROLLER.TeamList[TeamIndex].noofDRSLeft--;
            Time.timeScale = 1f;
            userReview.SetActive(value: false);
            AiReview.SetActive(value: false);
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
            {
                // ONLINE: skip the caught-behind replay (can't complete online; corrupted the next
                // delivery). Authoritative outcome stands; resolve cleanly. (See YesBtnClicked.)
                Singleton<GroundController>.instance.ForceResolveStuckReview();
            }
            else
            {
                CONTROLLER.canShowReplay = true;
                CONTROLLER.reviewReplay = true;
                Singleton<GameData>.instance.AnimationCompleted();
            }
            askedForReview = true;
        }
    }
}

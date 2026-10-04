using DG.Tweening;
 
using System.Linq.Expressions;
using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class Tutorial : Singleton<Tutorial>
{

	//[SerializeField] private PhotonView photonView;

	public GameObject holder;

	public GameObject posHolder;

	public GameObject shotHolder;

	public GameObject bowlingHolder;

	public GameObject bowlingSpotArrow;

	public GameObject tutorialToggle;

	private Transform _transform;

	public Transform hand;

	public Transform[] arrows;

	private DG.Tweening.Sequence sq;

	private Vector3[] toPos = new Vector3[8]
	{
		new Vector3(0f, 60f, 0f),
		new Vector3(-41f, 41f, 0f),
		new Vector3(-60f, 0f, 0f),
		new Vector3(-41f, -41f, 0f),
		new Vector3(0f, -60f, 0f),
		new Vector3(41f, -41f, 0f),
		new Vector3(60f, 0f, 0f),
		new Vector3(41f, 41f, 0f)
	};

	private bool batsmanArrowState;

	private bool bowlingSpotState;

	private bool shotHolderXPos;

	private bool isPaused;

	private bool tutorialBtn;

	private float secCount;
    
	//BowlingSpot Timer

	[SerializeField] private GameObject BowlingSpotInfoTimer;

    [SerializeField] private Image BowlingSpotInfotimerImage;

    [SerializeField] private Text BowlingSpotInfotimerText;

    //ShotInfo Timer

    [SerializeField] private GameObject ShotInfoTimer;

    [SerializeField] private Image ShotInfoTimerImage;

    [SerializeField] private Text ShotInfoTimerText;

    //BatsManMove Timer

    [SerializeField] private GameObject BatsManMoveTimer;

    [SerializeField] private Image BatsManMoveTimerImage;

    [SerializeField] private Text BatsManMoveTimerText;

    protected void Awake()
	{
		sq = DOTween.Sequence();
		_transform = base.transform;
		hideTutorial();
	}

    public void SetAutoBowlingTimer(float Seconds,GameObject Timer, Image TimerImg, Text TimerText)
    {
        secCount = Seconds;
        TimerImg.fillAmount = Seconds;
        //aiReviewstatus.text = LocalizationData.instance.getText(513);
        DG.Tweening.Sequence s = DOTween.Sequence();
		s.SetUpdate(true);
        TweenCallback callback = delegate
        {
            if (!Timer.activeInHierarchy)
            {
                s.Kill();
            }
            SetSecond(TimerText);
        };
        s.Append(TimerImg.DOFillAmount(0f, Seconds));
        for (int i = 0; i < Seconds; i++)
        {
            //if (!Timer.activeInHierarchy)
            //{
            //	s.Kill();
            //}
            s.InsertCallback(i, callback);
        }
        //s.InsertCallback(6f, NoBtnClicked);
        s.InsertCallback(Seconds, AutomaticHideTutorial);
    }

    public void SetSecond(Text TimerText)
    {
        ////Debug.Log("TIMMMERRR");
        TimerText.text = secCount.ToString();
        secCount--;
    }

	public void CallTimer(int IDX)
	{
		switch(IDX)
		{
			case 0: SetAutoBowlingTimer(5f,BowlingSpotInfoTimer,BowlingSpotInfotimerImage, BowlingSpotInfotimerText);
				break;
			case 1: SetAutoBowlingTimer(5f,BatsManMoveTimer,BatsManMoveTimerImage, BatsManMoveTimerText);
				break;
			case 2: SetAutoBowlingTimer(5f,ShotInfoTimer,ShotInfoTimerImage,ShotInfoTimerText);
				break;
		}
	}

	public void AutomaticHideTutorial()
	{
        Time.timeScale = 1f;
		hideTutorial();
    }

    public void hideTutorial()
	{
		if (posHolder.activeInHierarchy && CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
		{
			//photonView.RPC("RPC_BatsManMoved", RpcTarget.AllBuffered);
            //posHolder.SetActive(value: false);
            //Time.timeScale = 0f;
		}

		if (BowlingSpotInfoTimer.activeInHierarchy)
		{
			BowlingSpotInfoTimer.SetActive(false);
		}

		if (BatsManMoveTimer.activeInHierarchy)
		{
			BatsManMoveTimer.SetActive(false);
        }

		if (ShotInfoTimer.activeInHierarchy)
		{
			ShotInfoTimer.SetActive(false);
		}

		if(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
		{
            bowlingHolder.SetActive(value: false);
            posHolder.SetActive(value: false);
            StopArrowAnim();
            StopHandAnim();
            shotHolder.SetActive(value: false);
        }
		else if(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex != CONTROLLER.BattingTeamIndex)
		{
            bowlingHolder.SetActive(value: false);
            posHolder.SetActive(value: false);
            shotHolder.SetActive(value: false);
            StopArrowAnim();
            StopHandAnim();
        }

        if (CONTROLLER.PlayModeSelected != 8)
		{
			posHolder.SetActive(value: false);
			StopHandAnim();
			bowlingHolder.SetActive(value: false);
			StopArrowAnim();
			shotHolder.SetActive(value: false);
		}
	}

    //Photon Removal [PunRPC]
    private void RPC_BatsManMoved()
	{
		CONTROLLER.BATSMANMOVE = true;
		////Debug.Log("BATSMAN MOVEDDD ? " + CONTROLLER.BATSMANMOVE);
	}

    //Photon Removal [PunRPC]
    private void RPC_BowlingSpotInfo()
	{
		////Debug.Log("ONNEEE");
		CONTROLLER.BOWLINGSPOTINFO = true;
		Time.timeScale = 1f;
		Singleton<GameData>.instance.opponentWatchingTutorialPanel.SetActive(false);
		
		Singleton<GroundController>.instance.BowlerWaiting();
    }

    public void CallRPCBowlingSpotInfo()
	{
        //Photon Removal    if (PhotonNetwork.IsConnected)
        {
            //Photon Removal       photonView.RPC("RPC_BowlingSpotInfo", RpcTarget.OthersBuffered);
        }



    }

    private void StartArrowAnim()
	{
		sq = DOTween.Sequence();
		sq.SetUpdate(isIndependentUpdate: true);
        for (int i = 0; i < arrows.Length; i++)
		{
			sq.Insert(0f, arrows[i].DOLocalMove(toPos[i], 1f));
			sq.Insert(0.8f, arrows[i].GetComponent<Image>().DOFade(0f, 0.4f));
            ////Debug.Log("ANIMATION");
        }
    }

	private void StopArrowAnim()
	{
		for (int i = 0; i < arrows.Length; i++)
		{
			sq.Insert(0f, arrows[i].DOLocalMove(Vector3.zero, 0f));
			sq.Insert(0f, arrows[i].GetComponent<Image>().DOFade(1f, 0f));
		}
	}

	private void StartHandAnim()
	{
		sq = DOTween.Sequence();
		sq.Insert(0f, hand.DOLocalMoveX(50f, 1f)).SetLoops(-1, LoopType.Yoyo);
		sq.SetUpdate(isIndependentUpdate: true);
	}

	private void StopHandAnim()
	{
        hand.DOLocalMoveX(150f, 0f).SetUpdate(isIndependentUpdate: true);
	}

	public void showPositionHolder()
	{
        if (CONTROLLER.PlayModeSelected == 8)
        {
            return;
        }
  //      if (CONTROLLER.PlayModeSelected == 8 && !CONTROLLER.BOWLINGSPOTINFO)
		//{
  //          Time.timeScale = 0f;
		//	Singleton<GameModel>.instance.OpponentIsWatchingTutorialPanel.SetActive(true);
  //          return;
		//}
		Time.timeScale = 0f;
		posHolder.SetActive(value: true);
		StartHandAnim();
		if(CONTROLLER.PlayModeSelected == 8)
		{
			BatsManMoveTimer.SetActive(true);
			CallTimer(1);
		}
	}

	public void showShotHolder()
	{
        if (CONTROLLER.PlayModeSelected == 8)
        {
            return;
        }
        if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
		{
			Time.timeScale = 0f;
			if(CONTROLLER.PlayModeSelected == 8)
			{
				ShotInfoTimer.SetActive(true);
				CallTimer(2);
            }
			if (CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)
			{
				shotHolder.SetActive(value: true);
				StartArrowAnim();
			}
		}
	}

	public void showBowlingHolder()
	{
        if (CONTROLLER.PlayModeSelected == 8)
        {
            return;
        }
        Invoke("showNow", 0.01f);
	}

	private void showNow()
	{
		if(CONTROLLER.PlayModeSelected == 8)
		{
			////Debug.Log("STOPPPPRPRRRRRITTTT");
            BowlingSpotInfoTimer.SetActive(true);
			CallTimer(0);
        }
		Time.timeScale = 0f;
		bowlingHolder.SetActive(value: true);
    }

	public void updateBowlingHolderPos(Vector3 bowlingSpotPos)
	{
		bowlingSpotArrow.transform.localPosition = new Vector3(0f, 0.5f, 0f);
	}

	public void setBoolean()
	{
		if (CONTROLLER.tutorialToggle == 1 && CONTROLLER.PlayModeSelected != 6)
		{
			if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
			{
				posHolder.SetActive(batsmanArrowState);
                ////Debug.Log("STOP ");

                if (batsmanArrowState)
				{
					StartHandAnim();
				}
				else
				{
					StopHandAnim();
				}
				if (shotHolderXPos)
				{
					showShotHolder();
				}
			}
			else
			{
				bowlingHolder.SetActive(bowlingSpotState);
			}
		}
		else
		{
			hideTutorial();
		}
		batsmanArrowState = false;
		bowlingSpotState = false;
		shotHolderXPos = false;
		isPaused = false;
		tutorialBtn = false;
	}

	public void SwitchOffTutorial()
	{
		CONTROLLER.tutorialToggle = 0;
        if (CONTROLLER.PlayModeSelected == 8)
        {
            Singleton<GroundController>.instance.CallOppTutorial(false);
        }
        if (Time.timeScale == 0f)
		{
			Time.timeScale = 1f;
		}
		hideTutorial();
		tutorialToggle.SetActive(value: false);
		SavePlayerPrefs.SetSettingsList();
	}

	public void getBoolean()
	{
		if (!isPaused)
		{
			////Debug.Log("STOP ");
			tutorialBtn = tutorialToggle.activeSelf;
			batsmanArrowState = posHolder.activeSelf;
			bowlingSpotState = bowlingHolder.activeSelf;
			shotHolderXPos = shotHolder.activeSelf;
			posHolder.SetActive(value: false);
			bowlingHolder.SetActive(value: false);
			isPaused = true;
		}
	}

	public void ShowMe()
	{
		holder.SetActive(value: true);
	}

	public void HideMe()
	{
		holder.SetActive(value: false);
	}
}

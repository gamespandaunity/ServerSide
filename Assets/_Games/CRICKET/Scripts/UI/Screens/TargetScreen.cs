using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class TargetScreen : Singleton<TargetScreen>
{
	public Image LogoFlag;

	public Text okText;

	public Text TargetTxt;

	public GameObject holder;

    private float secCount;

    [SerializeField] private Image timerImage;

    [SerializeField] private Text timerText;

    [SerializeField] private GameObject Timer;

    protected void Awake()
	{
		Hide(boolean: true);
	}

	public void addEventListener()
	{
	}

	private string ReplaceText(string original, string replace)
	{
		string result = string.Empty;
		//Debug.Log(original + " " + replace + " ");
		if (original.Contains("#"))
		{
			result = original.Replace("#", replace);
		}
		return result;
	}

	private string ReplaceText(string original, string replace1, string replace2)
	{
		string text = string.Empty;
		int index = 0;
		//Debug.Log(original + " " + replace1 + " " + replace2 + " " + text);
		//Debug.Log(replace1);
		if (LocalizationData.localizationInstance.referenceList.Contains(replace1.ToUpper()))
		{
			index = LocalizationData.localizationInstance.referenceList.IndexOf(replace1.ToUpper());
		}
		if (original.Contains("#"))
		{
			text = original.Replace("#", LocalizationData.localizationInstance.getText(index));
		}
		//Debug.Log(original + " " + replace1 + " " + replace2 + " " + text);
		if (text.Contains("$"))
		{
			text = text.Replace("$", replace2);
		}
		//Debug.Log(original + " " + replace1 + " " + replace2 + " " + text);
		return text;
	}

	public void UpdateTarget()
	{
		if (CONTROLLER.PlayModeSelected != 7)
		{
			CONTROLLER.TargetToChase = CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores + 1;
			if (Singleton<GameData>.instance != null)
				Singleton<GameData>.instance.RepairCollapsedTarget();
		}
		if (CONTROLLER.BattingTeamIndex == CONTROLLER.opponentTeamIndex)
		{
			Sprite[] flags = Singleton<FlagHolderGround>.instance.flags;
			foreach (Sprite sprite in flags)
			{
				if (sprite.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.myTeamIndex].abbrevation)
				{
					LogoFlag.sprite = sprite;
				}
			}
			okText.text = LocalizationData.localizationInstance.getText(231);
			TargetTxt.text = LocalizationData.localizationInstance.getText(256);
			TargetTxt.text = ReplaceText(TargetTxt.text, CONTROLLER.TargetToChase.ToString());
			return;
		}
		Sprite[] flags2 = Singleton<FlagHolderGround>.instance.flags;
		foreach (Sprite sprite2 in flags2)
		{
			if (sprite2.name.ToUpper() == CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].abbrevation)
			{
				LogoFlag.sprite = sprite2;
			}
		}
		okText.text = LocalizationData.localizationInstance.getText(232);
		TargetTxt.text = LocalizationData.localizationInstance.getText(410);
		TargetTxt.text = ReplaceText(TargetTxt.text, CONTROLLER.TeamList[CONTROLLER.opponentTeamIndex].teamName, (CONTROLLER.TargetToChase - 1).ToString());
	}

	public void Continue()
	{
		CONTROLLER.isAutoPlayed = false;
		Hide(boolean: true);
		CONTROLLER.currentInnings = 1;
		Singleton<GameData>.instance.ResetVariables();
	}

	public void Hide(bool boolean)
	{
		if (boolean)
		{
			holder.SetActive(value: false);
            if(CONTROLLER.PlayModeSelected == 8)
            {
                Timer.SetActive(false);
            }
            Singleton<StandbyCam>.instance.PauseTween();
		}
		else
		{
			if(CONTROLLER.PlayModeSelected == 8)
			{
				//Timer.SetActive(true);
			}
			Singleton<StandbyCam>.instance.RotateStandbyCam();
			UpdateTarget();
			holder.SetActive(value: true);
			if(CONTROLLER.PlayModeSelected == 8)
			{
                if (Timer.activeInHierarchy)
				{
					return;
				}
                //Debug.Log("TARGETTTTT HOOO GYAa");
				SetTargetScreenTimer(5f);
			}
		}
	}

    public void SetTargetScreenTimer(float Seconds)
    {
        Timer.SetActive(true);

        secCount = Seconds;
        timerImage.fillAmount = 1f;
        //aiReviewstatus.text = LocalizationData.instance.getText(513);
        Sequence s = DOTween.Sequence();
        s.SetUpdate(true);
        TweenCallback callback = delegate
        {
            if (!Timer.activeInHierarchy)
            {
                s.Kill();
				Timer.SetActive(false);
            }
            SetSecond();
        };
        s.Append(timerImage.DOFillAmount(0f, Seconds));
        for (int i = 0; i < Seconds; i++)
        {
            s.InsertCallback(i, callback);
        }
        //s.InsertCallback(6f, NoBtnClicked);
        s.InsertCallback(Seconds, Continue);
    }

    public void SetSecond()
    {
        timerText.text = secCount.ToString();
        secCount--;
    }
}

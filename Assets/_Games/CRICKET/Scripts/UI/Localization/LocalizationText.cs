using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class LocalizationText : Singleton<LocalizationText>
{
	public Font[] fontAssets;

	public int index = -1;

	public int languageIndex = 1;

	private Text text;

	private bool textAvailable;

	private bool firstTime = true;

	public string tempValue = string.Empty;

	public int stringNoOfChar;

	public string defaultString;

    private void Start()
    {
		OnEnable1();
    }
    public void OnEnable1()
	{
		if (firstTime)
		{
			defaultString = GetComponent<Text>().text;
			
			firstTime = false;
		}
		if (text == null)
		{
			text = GetComponent<Text>();
		}
		if (index == -2)
		{
			text.text = ReplaceOriginalText(GetComponent<Text>().text, stringNoOfChar);
			index = LocalizationData.localizationInstance.referenceList.IndexOf(text.text);
			text.text = LocalizationData.localizationInstance.getText(index);
			text.text = ReplaceText(text.text, tempValue);
			text.font = fontAssets[1];
			
			return;
		}
		index = -1;
		if (defaultString == string.Empty)
		{
			if (LocalizationData.localizationInstance.referenceList.Contains(text.text.ToUpper()))
			{
				index = LocalizationData.localizationInstance.referenceList.IndexOf(text.text.ToUpper());
			}
		}
		else if (PlayerPrefs.HasKey(defaultString))
		{
			index = PlayerPrefs.GetInt(defaultString);
		}
		else if (LocalizationData.localizationInstance.referenceList.Contains(defaultString.ToUpper()))
		{
			//Debug.Log("found" + index);
			index = LocalizationData.localizationInstance.referenceList.IndexOf(defaultString.ToUpper());
			PlayerPrefs.SetInt(defaultString, index);
		}
		text.font = fontAssets[1];
		text.fontStyle = FontStyle.Normal;
		text.alignByGeometry = true;
		
			text.lineSpacing = 1f;
		
		//Debug.Log(index + " " + text.text);
		if (index != -1)
		{
			//Debug.Log(index + " " + text.text);
			text.text = LocalizationData.localizationInstance.getText(index);
		}
		if (index == 18)
		{
			text.text = LocalizationData.localizationInstance.getText(18) + " " + LocalizationData.localizationInstance.getText(19);
		}
		if (index == 124)
		{
			text.text = LocalizationData.localizationInstance.getText(124) + "!";
		}
		if (index == 20)
		{
			text.text = LocalizationData.localizationInstance.getText(20) + " " + LocalizationData.localizationInstance.getText(21);
		}
		if (index == 22)
		{
			text.text = LocalizationData.localizationInstance.getText(22) + " \n\n" + LocalizationData.localizationInstance.getText(23) + " \n\n" + LocalizationData.localizationInstance.getText(24) + " \n\t\t" + LocalizationData.localizationInstance.getText(25) + " \n\t\t" + LocalizationData.localizationInstance.getText(26) + " \n\t\t" + LocalizationData.localizationInstance.getText(27) + " \n" + LocalizationData.localizationInstance.getText(28) + " \n\t\t" + LocalizationData.localizationInstance.getText(29) + " \n\t\t" + LocalizationData.localizationInstance.getText(30) + " \n\t\t" + LocalizationData.localizationInstance.getText(31) + " \n" + LocalizationData.localizationInstance.getText(32) + " \n\t\t" + LocalizationData.localizationInstance.getText(33) + " \n\t\t" + LocalizationData.localizationInstance.getText(34) + " \n\t\t" + LocalizationData.localizationInstance.getText(35);
		}
		if (index == 36)
		{
			text.text = LocalizationData.localizationInstance.getText(36) + " \n" + LocalizationData.localizationInstance.getText(37) + " \n\t- " + LocalizationData.localizationInstance.getText(38) + " \n\t- " + LocalizationData.localizationInstance.getText(39) + " \n\t- " + LocalizationData.localizationInstance.getText(40) + " \n\t- " + LocalizationData.localizationInstance.getText(41) + " \n\t- " + LocalizationData.localizationInstance.getText(42) + " \n\t- " + LocalizationData.localizationInstance.getText(43) + " \n\t- " + LocalizationData.localizationInstance.getText(44) + " \n\t- " + LocalizationData.localizationInstance.getText(45) + " \n\t- " + LocalizationData.localizationInstance.getText(46) + " \n\t- " + LocalizationData.localizationInstance.getText(47);
		}
		if (index == 48)
		{
			text.text = LocalizationData.localizationInstance.getText(48) + " \n1 " + LocalizationData.localizationInstance.getText(49) + ": " + LocalizationData.localizationInstance.getText(50) + " " + LocalizationData.localizationInstance.getText(51) + " \n2 " + LocalizationData.localizationInstance.getText(52) + ": " + LocalizationData.localizationInstance.getText(53);
		}
		if (index == 54)
		{
			text.text = LocalizationData.localizationInstance.getText(54) + " \n\n1 " + LocalizationData.localizationInstance.getText(11) + ": \n" + LocalizationData.localizationInstance.getText(55) + " " + LocalizationData.localizationInstance.getText(56) + " " + LocalizationData.localizationInstance.getText(57) + " \n\n2 " + LocalizationData.localizationInstance.getText(12) + ": \n" + LocalizationData.localizationInstance.getText(58) + " " + LocalizationData.localizationInstance.getText(59) + " " + LocalizationData.localizationInstance.getText(60);
		}
		if (index == 61)
		{
			text.text = string.Empty + LocalizationData.localizationInstance.getText(61) + " \n" + LocalizationData.localizationInstance.getText(62) + " \n" + LocalizationData.localizationInstance.getText(63) + " \n" + LocalizationData.localizationInstance.getText(64);
		}
		if (index == 65)
		{
			text.text = "- " + LocalizationData.localizationInstance.getText(65) + " \n- " + LocalizationData.localizationInstance.getText(66) + " \n- " + LocalizationData.localizationInstance.getText(67);
		}
		if (index == 69)
		{
			text.text = LocalizationData.localizationInstance.getText(68) + " \n" + LocalizationData.localizationInstance.getText(69) + " \n\t\t* " + LocalizationData.localizationInstance.getText(70) + ":4 " + LocalizationData.localizationInstance.getText(74) + " \n\t\t* " + LocalizationData.localizationInstance.getText(71) + ":6 " + LocalizationData.localizationInstance.getText(74) + " \n\t\t* " + LocalizationData.localizationInstance.getText(72) + ":2 " + LocalizationData.localizationInstance.getText(74) + " \n\t\t* " + LocalizationData.localizationInstance.getText(73) + ":2 " + LocalizationData.localizationInstance.getText(74) + " \n\t\t* " + LocalizationData.localizationInstance.getText(75) + ":2 " + LocalizationData.localizationInstance.getText(74) + " \n\t\t" + LocalizationData.localizationInstance.getText(76) + "\n\n" + LocalizationData.localizationInstance.getText(77) + "\n" + LocalizationData.localizationInstance.getText(78) + " \n\t\t* " + LocalizationData.localizationInstance.getText(79) + ":2 " + LocalizationData.localizationInstance.getText(74) + " \n\t\t* " + LocalizationData.localizationInstance.getText(80) + ":6 " + LocalizationData.localizationInstance.getText(74) + " \n\t\t* " + LocalizationData.localizationInstance.getText(81) + ":4 " + LocalizationData.localizationInstance.getText(74) + " \n\t\t* " + LocalizationData.localizationInstance.getText(82) + ":2 " + LocalizationData.localizationInstance.getText(74);
		}
		if (index == 83)
		{
			text.text = LocalizationData.localizationInstance.getText(83) + " \n\n" + LocalizationData.localizationInstance.getText(84) + " \n\n\t\t* " + LocalizationData.localizationInstance.getText(85) + " \n\t\t* " + LocalizationData.localizationInstance.getText(86) + " \n\t\t* " + LocalizationData.localizationInstance.getText(87) + " \n\t\t* " + LocalizationData.localizationInstance.getText(88) + " \n\t\t* " + LocalizationData.localizationInstance.getText(89) + " \n\t\t* " + LocalizationData.localizationInstance.getText(90) + " \n\t\t* " + LocalizationData.localizationInstance.getText(91) + " \n\t\t* " + LocalizationData.localizationInstance.getText(92) + " \n\t\t* " + LocalizationData.localizationInstance.getText(93) + " \n\t\t* " + LocalizationData.localizationInstance.getText(94);
		}
		if (index == 96)
		{
			text.text = LocalizationData.localizationInstance.getText(96) + " " + LocalizationData.localizationInstance.getText(97) + " " + LocalizationData.localizationInstance.getText(98) + " " + LocalizationData.localizationInstance.getText(99) + " " + LocalizationData.localizationInstance.getText(100);
		}
		if (index == 101)
		{
			text.text = LocalizationData.localizationInstance.getText(101) + " " + LocalizationData.localizationInstance.getText(102) + " " + LocalizationData.localizationInstance.getText(103) + " \n" + LocalizationData.localizationInstance.getText(104) + " \n" + LocalizationData.localizationInstance.getText(105) + " \n" + LocalizationData.localizationInstance.getText(106) + " \n" + LocalizationData.localizationInstance.getText(107) + " \n" + LocalizationData.localizationInstance.getText(108) + " \n" + LocalizationData.localizationInstance.getText(109) + " \n" + LocalizationData.localizationInstance.getText(110);
		}
		if (index == 267)
		{
			text.text = LocalizationData.localizationInstance.getText(267);
			text.text = ReplaceText((CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6).ToString());
		}
		if (index == 268)
		{
			text.text = LocalizationData.localizationInstance.getText(268) + " " + ((float)CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchScores / (float)(CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchBalls / 6)).ToString("F2");
		}
		if (index == 637)
		{
			text.text = LocalizationData.localizationInstance.getText(637);
			text.text = ReplaceText(CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchBalls / 6 + "." + CONTROLLER.TeamList[CONTROLLER.BowlingTeamIndex].currentMatchBalls % 6);
		}
		if (index == 223)
		{
			text.text = LocalizationData.localizationInstance.getText(223);
		}
		if (index == 355)
		{
			text.text = LocalizationData.localizationInstance.getText(355) + " " + LocalizationData.localizationInstance.getText(356) + " " + LocalizationData.localizationInstance.getText(357);
		}
		if (index == 375)
		{
			text.text = LocalizationData.localizationInstance.getText(375) + " " + LocalizationData.localizationInstance.getText(376);
		}
		if (index == 377)
		{
			text.text = LocalizationData.localizationInstance.getText(377) + " " + LocalizationData.localizationInstance.getText(378);
		}
		if (index == 380)
		{
			text.text = LocalizationData.localizationInstance.getText(380) + " " + LocalizationData.localizationInstance.getText(381);
		}
		if (index == 383)
		{
			text.text = LocalizationData.localizationInstance.getText(383) + "\n" + LocalizationData.localizationInstance.getText(384);
		}
		if (index == 392)
		{
			text.text = LocalizationData.localizationInstance.getText(392);
			text.text = ReplaceText(text.text, "50");
		}
		if (index == 594)
		{
			text.text = LocalizationData.localizationInstance.getText(594) + "\n" + LocalizationData.localizationInstance.getText(595) + "\n" + LocalizationData.localizationInstance.getText(596);
		}
		if (index == 597)
		{
			text.text = LocalizationData.localizationInstance.getText(597) + "\n" + LocalizationData.localizationInstance.getText(598) + "\n" + LocalizationData.localizationInstance.getText(599);
		}
		if (index == 600)
		{
			text.text = LocalizationData.localizationInstance.getText(600) + "\n" + LocalizationData.localizationInstance.getText(601) + "\n" + LocalizationData.localizationInstance.getText(602);
		}
		if (index == 626)
		{
			text.text = LocalizationData.localizationInstance.getText(626) + "\n\n" + LocalizationData.localizationInstance.getText(627) + "\n" + LocalizationData.localizationInstance.getText(628) + "\n" + LocalizationData.localizationInstance.getText(629) + "\n\n" + LocalizationData.localizationInstance.getText(630) + "\n" + LocalizationData.localizationInstance.getText(631) + "\n\n" + LocalizationData.localizationInstance.getText(632) + "\n" + LocalizationData.localizationInstance.getText(633) + "\n" + LocalizationData.localizationInstance.getText(634) + "\n" + LocalizationData.localizationInstance.getText(635) + "\n" + LocalizationData.localizationInstance.getText(636);
		}
		if (index == 288)
		{
			text.text = LocalizationData.localizationInstance.getText(288) + ".\n" + LocalizationData.localizationInstance.getText(178) + "!";
		}
		if (index == 492)
		{
			text.text = LocalizationData.localizationInstance.getText(492) + "\n\n" + LocalizationData.localizationInstance.getText(493);
		}
		if (index == 642)
		{
			text.text = LocalizationData.localizationInstance.getText(642) + ".\n" + LocalizationData.localizationInstance.getText(567) + "!";
		}
	}

	public string ReplaceText(string replace)
	{
		string result = string.Empty;
		if (text.text.Contains("#"))
		{
			result = text.text.Replace("#", replace);
		}
		return result;
	}

	public string ReplaceText(string original, string replace)
	{
		string result = string.Empty;
		//Debug.Log(original + " " + replace + " ");
		if (original.Contains("#"))
		{
			result = original.Replace("#", replace);
		}
		return result;
	}

	public string ReplaceOriginalText(string replace, int charCount)
	{
		string result = string.Empty;
		if (replace.Contains("over match".ToUpper()))
		{
			//Debug.Log(replace);
			tempValue = replace.Substring(0, charCount);
			result = "# OVER MATCH";
		}
		return result;
	}
}

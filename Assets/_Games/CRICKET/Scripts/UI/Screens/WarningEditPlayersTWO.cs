using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class WarningEditPlayersTWO : Singleton<WarningEditPlayersTWO>
{
	public GameObject Holder;

	public Text ErrorTxt;

	protected void Start()
	{
		hideMe();
	}

	public void showMe()
	{
		CONTROLLER.PopupName = "editPopup";
		Singleton<Popups>.instance.ShowMe();
		ErrorTxt.text = LocalizationData.localizationInstance.getText(140);
	}

	public void hideMe()
	{
		Holder.SetActive(value: false);
	}

	public void YesButton()
	{
		hideMe();
		Singleton<Popups>.instance.HideMe();
		Singleton<SquadPageTWO>.instance.showMe(CONTROLLER.EditTeamIndex);
	}

	public void NoButton()
	{
		Singleton<Popups>.instance.HideMe();
	}
}

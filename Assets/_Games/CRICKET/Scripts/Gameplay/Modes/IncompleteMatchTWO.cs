using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using Cricket;

public class IncompleteMatchTWO : Singleton<IncompleteMatchTWO>
{
	//public GameObject Holder;

	protected void Start()
	{
	//	Holder.SetActive(value: false);
	}

	public void YesButton()
	{
		Singleton<Popups>.instance.HideMe();
		CONTROLLER.GameStartsFromSave = false;
		CONTROLLER.isFreeHitBall = false;
		AutoSave.DeleteFile();
		if (CONTROLLER.PlayModeSelected == 0)
		{
			Singleton<GameModeTWO>.instance.getExhibitionState();
		}
		
	}

	public void NoButton()
	{
		Singleton<Popups>.instance.HideMe();
	}

	public void CancelButton()
	{
		hideMe();
		CONTROLLER.CurrentMenu = "landingpage";
	}

	public void showMe()
	{
		CONTROLLER.PopupName = "incompletePopup2";
		CONTROLLER.CurrentMenu = "unfinishedmatch";
		//Singleton<Popups>.instance.ShowMe();
	}

	public void hideMe()
	{
		Singleton<GameModeTWO>.instance.showMe();
		//Holder.SetActive(value: false);
	}
}

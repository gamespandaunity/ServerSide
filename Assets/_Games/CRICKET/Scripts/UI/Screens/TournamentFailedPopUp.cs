using UnityEngine;
using UnityEngine.UI;
using Cricket;

public class TournamentFailedPopUp : Singleton<TournamentFailedPopUp>
{
	public bool showMe;

	public Image cup;

	public GameObject holder;

	public GameObject bottomComponent;

	private void Start()
	{
		HideMe();
		showMe = false;
	}

	public void ShowMe()
	{
		if (CONTROLLER.PlayModeSelected != 2 && CONTROLLER.PlayModeSelected != 8)
		{
			cup.sprite = Singleton<AfterOverSummary>.instance.CupSprites[CONTROLLER.PlayModeSelected];
		}
		else if (CONTROLLER.PlayModeSelected == 8)
		{
			//cup.sprite = Singleton<AfterOverSummary>.instance.CupSprites[0];
            HideMe();
            Singleton<GameData>.instance.LoadMainMenuScene();
			return;
        }
  //      else if (CONTROLLER.tournamentType == "NPL")
		//{
		//	cup.sprite = Singleton<AfterOverSummary>.instance.CupSprites[CONTROLLER.PlayModeSelected];
		//}
		//else if (CONTROLLER.tournamentType == "PAK")
		//{
		//	cup.sprite = Singleton<AfterOverSummary>.instance.CupSprites[4];
		//}
		//else if (CONTROLLER.tournamentType == "AUS")
		//{
		//	cup.sprite = Singleton<AfterOverSummary>.instance.CupSprites[5];
		//}
		holder.SetActive(value: true);
		showMe = false;
	}

	public void HideMe()
	{
		holder.SetActive(value: false);
	}

	public void Close()
	{
		HideMe();
		Singleton<GameData>.instance.LoadMainMenuScene();
	}
}

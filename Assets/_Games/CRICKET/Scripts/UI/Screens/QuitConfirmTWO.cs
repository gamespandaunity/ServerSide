using Cricket;

public class QuitConfirmTWO : Singleton<QuitConfirmTWO>
{
	private string prevPage = string.Empty;

	protected void Start()
	{
		hideMe();
	}

	public void YesButton()
	{
		hideMe();
		Singleton<GameData>.instance.GameQuitted();
	}

	public void NoButton()
	{
		hideMe();
		if (CONTROLLER.CurrentMenu != "controls" && CONTROLLER.CurrentMenu != "settings")
		{
			Singleton<GameData>.instance.AgainToGamePauseScreen();
		}
	}

	public void showMe()
	{
		prevPage = CONTROLLER.CurrentMenu;
		base.gameObject.SetActive(value: true);
		CONTROLLER.CurrentMenu = "quitconfirm";
	}

	public void hideMe()
	{
		CONTROLLER.CurrentMenu = prevPage;
		base.gameObject.SetActive(value: false);
	}
}

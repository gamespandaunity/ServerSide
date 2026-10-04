using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class OutOfCashMenu : MonoBehaviour {

	public Text totalCoins;
	public Text requiredCoins;
	PlayerDataSerializeable playerData;
	public static bool isCarBuy;
	void OnEnable(){
		playerData = PlayerDataController.instance.playerStats;
		if (isCarBuy) {
			requiredCoins.text = (playerData.CarsList[playerData.CurrentSelectedVehicle-1].UnlockPrice-playerData.PlayerGold).ToString();

		} else {
			requiredCoins.text = (playerData.envioronmentList[playerData.CurrentEnvironment-1].UnlockPrice-playerData.PlayerGold).ToString();
		}
		totalCoins.text = playerData.PlayerGold.ToString ();
	}
}

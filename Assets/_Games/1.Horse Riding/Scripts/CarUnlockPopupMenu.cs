using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class CarUnlockPopupMenu : MonoBehaviour {
	//public Text totalCoins;
	public Text requiredCoins;
	PlayerDataSerializeable playerData;
	public bool isUpgrade;
	void OnEnable(){
		playerData = PlayerDataController.instance.playerStats;
		if (isUpgrade) {
			requiredCoins.text = playerData.CarsList [playerData.CurrentSelectedVehicle - 1].UpgradePrice.ToString ();				
		} else {
			requiredCoins.text = playerData.CarsList[playerData.CurrentSelectedVehicle-1].UnlockPrice.ToString();
		}
		//totalCoins.text = playerData.PlayerGold.ToString ();
	}
}

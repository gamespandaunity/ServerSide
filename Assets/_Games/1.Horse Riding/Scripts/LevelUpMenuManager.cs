using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LevelUpMenuManager : MonoBehaviour {

	public int Reward= 50;
	//public Text totalCoins;
	public Text requiredCoins;
	public Text rankText;

	PlayerDataSerializeable playerData;

	void OnEnable(){
		playerData = PlayerDataController.instance.playerStats;	
		requiredCoins.text =Reward.ToString();
		rankText.text =(playerData.Rank+1).ToString();

	}

	public void giveReward(){
		playerData.PlayerGold += Reward;
		playerData.Rank += 1;
		PlayerDataController.instance.SaveData ();
	}
}

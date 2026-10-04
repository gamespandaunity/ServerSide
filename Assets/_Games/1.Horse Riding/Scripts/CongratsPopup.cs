using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CongratsPopup : MonoBehaviour {
	public int Reward= 50;
    public int NitosReward = 4;

    //public Text totalCoins;
    public Text requiredCoins;
	PlayerDataSerializeable playerData;
    public GameObject []sprites;
	void OnEnable(){
		playerData = PlayerDataController.instance.playerStats;	
		requiredCoins.text =Reward.ToString();

    }

    public void giveReward(){
        //if (AdsManager.isAddNitros)
        //{
        //    playerData.NitrosCount += NitosReward;

        //}else
        //{
        //    playerData.PlayerGold += Reward;

        //}
        PlayerDataController.instance.SaveData ();
	}
}

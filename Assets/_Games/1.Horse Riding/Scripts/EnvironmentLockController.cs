using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class EnvironmentLockController : MonoBehaviour {
	public int EnvironmentId;
	public GameObject lockOverLay;
    public GameObject bestTime;
    public Text BestTimeValue;

    public Text priceText;
	void OnEnable(){
		PlayerDataSerializeable pDta = PlayerDataController.instance.playerStats;
		priceText.text = pDta.envioronmentList [EnvironmentId-1].UnlockPrice.ToString();

		for (int i = 0; i < pDta.envioronmentList.Count; i++) {
			if (pDta.envioronmentList [i].ID == EnvironmentId && !pDta.envioronmentList [i].isLocked) {
				lockOverLay.SetActive (false);
                if (MultiPlayerGame.isTimeTrial)
                {
                    bestTime.SetActive(true);
                    BestTimeValue.text = TimeTrialController.ConvertFloatToTime (PlayerDataController.instance.playerStats.BestTime[EnvironmentId - 1], "#0:00.0");

                }
                else
                {
                    bestTime.SetActive(false);
                }
            }
            //else
            //{
            //    bestTime.SetActive(false);
            //}
		}
	}
}

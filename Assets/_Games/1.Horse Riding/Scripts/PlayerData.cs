using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerData : MonoBehaviour {
	public int PlayerGold = 100;
	public int PlayerCash = 100;
	public int CurrentPlayerLevel=1;
	public int CurrentMode=1;
    public int NitrosCount = 0;

    public List<DataContainerCar> CarsList;
	public List<PlayerEnvironmentDefaultData> envioronmentList;


}

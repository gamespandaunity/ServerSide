using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PlayerDataSerializeable  {
	public int PlayerGold = 10000;
	public int PlayerCash = 100000;
    public int NitrosCount = 4;

    public int Rank =1;
	public int xpoints =0;

	public List<PlayerCar> CarsList;
	public List<PlayerEnvironment> envioronmentList;
    public int playerAwatarId;

    public int CurrentSelectedVehicle=1;
	public int CurrentMode=1;
	public int CurrentEnvironment=1;
	public int SelectedControl=0;
	public bool isSoundOn= true;
    public bool isHighQuality = false;
    
    //
    public int currentSelectLevel=1;
    public int CurrentSelectDubaiChampionLevel = 1;
    public int CurrentSelectBritishChampionLevel = 1;
    public int CurrentSelectKentuckyChampionLevel = 1;
    public int CurrentSelectPegasusChampionLevel = 1;

    public int LastUnlockedDubaiChampionLevel=1;
    public int LastUnlockedBritishChampionLevel = 1;
    public int LastUnlockedKentuckyChampionLevel = 1;
    public int LastUnlockedPegasusChampionLevel = 1;
    
    public bool UnlockedDubaiChampion = true;
    public bool UnlockedBritishChampion = false;
    public bool UnlockedKentuckyChampion = false;
    public bool UnlockedPegasusChampion = false;
    
    public bool unlockedAllLevels;
    //

    public List<float> BestTime;
	public int LastWinCount=0;
    public string playerName ="Player";
    public bool isPlayerNameSet;
    public int multiplayerLevel;
    public bool isNotFirstTime;
    public bool isRateUSDone;
    public float SensivityValue =1;
    public string countryCode= "";
    public bool autoSelectCountry = true;
    public bool adspurchased = false;
}

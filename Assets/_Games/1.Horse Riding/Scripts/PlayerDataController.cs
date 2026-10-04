using UnityEngine;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


public class PlayerDataController : MonoBehaviour
{
    public static PlayerDataController instance;
    [HideInInspector]
    public PlayerDataSerializeable playerStats;
    public GameObject DefaultPlayerData;
    string fileTitle = "/GameState.Gd";
    void Awake()
    {
        instance = this;
        LoadData();
        DontDestroyOnLoad(this);
    }

    void LoadSaveDefaultData()
    {
        playerStats = new PlayerDataSerializeable();
        PlayerData defaultPlayerData = DefaultPlayerData.GetComponent<PlayerData>();
        playerStats.PlayerCash = defaultPlayerData.PlayerCash;
        playerStats.PlayerGold = defaultPlayerData.PlayerGold;
        playerStats.NitrosCount = defaultPlayerData.NitrosCount;

        playerStats.CarsList = new List<PlayerCar>();

        for (int i = 0; i < defaultPlayerData.CarsList.Count; i++)
        {
            PlayerCar pCar = new PlayerCar();
            pCar.ID = defaultPlayerData.CarsList[i].ID;

            pCar.isLocked = defaultPlayerData.CarsList[i].isLocked;
            pCar.N2O = defaultPlayerData.CarsList[i].N2O;
            pCar.Speed = defaultPlayerData.CarsList[i].Speed;
            pCar.UnlockPrice = defaultPlayerData.CarsList[i].UnlockPrice;
            pCar.UpgradeLevel = defaultPlayerData.CarsList[i].UpgradeLevel;
            pCar.UpgradePrice = defaultPlayerData.CarsList[i].UpgradePrice;
            pCar.Control = defaultPlayerData.CarsList[i].Control;
            pCar.Acceleration = defaultPlayerData.CarsList[i].Acceleration;
            pCar.isLocked = defaultPlayerData.CarsList[i].isLocked;
            playerStats.CarsList.Add(pCar);

        }

        playerStats.envioronmentList = new List<PlayerEnvironment>();
        playerStats.BestTime = new List<float>();

        for (int i = 0; i < defaultPlayerData.envioronmentList.Count; i++)
        {
            PlayerEnvironment pCar = new PlayerEnvironment();
            pCar.ID = defaultPlayerData.envioronmentList[i].ID;
            pCar.UnlockPrice = defaultPlayerData.envioronmentList[i].UnlockPrice;
            pCar.isLocked = defaultPlayerData.envioronmentList[i].isLocked;
            playerStats.envioronmentList.Add(pCar);
            playerStats.BestTime.Add(1500);


        }

        playerStats.playerAwatarId = 0;

        SaveData();
        //Load ();

    }

    //it's static so we can call it from anywhere
    public void SaveData()
    {
        BinaryFormatter bf = new BinaryFormatter();
        //Application.persistentDataPath is a string, so if you wanted you can put that into debug.log if you want to know where save games are located
        FileStream file = File.Create(Application.persistentDataPath + fileTitle); //you can call it anything you want
        bf.Serialize(file, playerStats);
        file.Close();
    }
    public void UpdateIncrementData()
    {
        PlayerData defaultPlayerData = DefaultPlayerData.GetComponent<PlayerData>();

        //		if(playerData.StarsList.Count < defaultPlayerData.TotalLevels){    
        //			for (int i = playerData.StarsList.Count; i < defaultPlayerData.TotalLevels; i++) {
        //				playerData.StarsList.Add (0);
        //			}
        //		}
        if (playerStats.CarsList.Count < defaultPlayerData.CarsList.Count)
        {
            for (int i = playerStats.CarsList.Count; i < defaultPlayerData.CarsList.Count; i++)
            {
                PlayerCar pCar = new PlayerCar();
                pCar.ID = defaultPlayerData.CarsList[i].ID;

                pCar.isLocked = defaultPlayerData.CarsList[i].isLocked;
                pCar.N2O = defaultPlayerData.CarsList[i].N2O;
                pCar.Speed = defaultPlayerData.CarsList[i].Speed;
                pCar.UnlockPrice = defaultPlayerData.CarsList[i].UnlockPrice;
                pCar.UpgradeLevel = defaultPlayerData.CarsList[i].UpgradeLevel;
                pCar.UpgradePrice = defaultPlayerData.CarsList[i].UpgradePrice;
                pCar.Control = defaultPlayerData.CarsList[i].Control;
                pCar.Acceleration = defaultPlayerData.CarsList[i].Acceleration;
                pCar.isLocked = defaultPlayerData.CarsList[i].isLocked;
                playerStats.CarsList.Add(pCar);

            }
        }

        if (playerStats.envioronmentList.Count < defaultPlayerData.envioronmentList.Count)
        {
            for (int i = playerStats.envioronmentList.Count; i < defaultPlayerData.envioronmentList.Count; i++)
            {
                PlayerEnvironment pCar = new PlayerEnvironment();
                pCar.ID = defaultPlayerData.envioronmentList[i].ID;
                pCar.UnlockPrice = defaultPlayerData.envioronmentList[i].UnlockPrice;
                pCar.isLocked = defaultPlayerData.envioronmentList[i].isLocked;
                playerStats.envioronmentList.Add(pCar);
                playerStats.BestTime.Add(1500);


            }
        }

        playerStats.UnlockedDubaiChampion = true;

        //
        if (playerStats.LastUnlockedDubaiChampionLevel < 1)
        {
            playerStats.LastUnlockedDubaiChampionLevel = 1;
            playerStats.CurrentSelectDubaiChampionLevel = 1;

        }

        if (playerStats.LastUnlockedBritishChampionLevel < 1)
        {
            playerStats.LastUnlockedBritishChampionLevel = 1;
            playerStats.CurrentSelectBritishChampionLevel = 1;

        }

        if (playerStats.LastUnlockedKentuckyChampionLevel < 1)
        {
            playerStats.LastUnlockedKentuckyChampionLevel = 1;
            playerStats.CurrentSelectKentuckyChampionLevel = 1;

        }

        if (playerStats.LastUnlockedPegasusChampionLevel < 1)
        {
            playerStats.LastUnlockedPegasusChampionLevel = 1;
            playerStats.CurrentSelectPegasusChampionLevel = 1;

        }
        // 
    }

    public void LoadData()
    {
        try
        {
            if (File.Exists(Application.persistentDataPath + fileTitle))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(Application.persistentDataPath + fileTitle, FileMode.Open);
                ////Debug.Log("Path " + Application.persistentDataPath + fileName);
                playerStats = (PlayerDataSerializeable)bf.Deserialize(file);
                file.Close();
                UpdateIncrementData();

            }
            else
            {
                LoadSaveDefaultData();
            }
        }
        catch (System.Exception ex)
        {
            LoadSaveDefaultData();

        }

    }



}

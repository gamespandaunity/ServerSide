namespace CarRace
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    /// <summary>
    /// This Class ss used to store and retrieve  user data
    /// </summary>
    public class UserData
    {
        public static void AddCoins(int amount)
        {
            PlayerPrefs.SetInt(PPConst.Coins, PlayerPrefs.GetInt(PPConst.Coins) + amount);
        }
        public static void SubtractCoins(int amount)
        {
            PlayerPrefs.SetInt(PPConst.Coins, PlayerPrefs.GetInt(PPConst.Coins) - amount);
        }
        public static bool CheckUserCoins(int coins)
        {
            return PlayerPrefs.GetInt(PPConst.Coins) >= coins ? true : false;
        }
        public static bool CheckCar(int carIndex)
        {
            return PlayerPrefs.GetInt(PPConst.Car + carIndex, 0) == 0 ? false : true;
        }
        public static void BuyCar(int carIndex, int price)
        {
            SubtractCoins(price);
            UnlockCar(carIndex);
        }
        public static void UnlockCar(int carIndex)
        {
            PlayerPrefs.SetInt(PPConst.Car + carIndex, 1);
        }
        public static bool CheckLevel(int levelIndex)
        {
            return PlayerPrefs.GetInt(PPConst.Map + levelIndex, 0) == 0 ? false : true;
        }
        public static void UnlockLevel(int levelIndex, int price)
        {
            SubtractCoins(price);
            UnlockLevel(levelIndex);
        }
        public static void UnlockLevel(int levelIndex)
        {
            PlayerPrefs.SetInt(PPConst.Map + levelIndex, 1);
        }
        public static bool ShowAds()
        {
            return PlayerPrefs.GetInt(PPConst.Ads, 0) == 0 ? true : false;
        }
        public static void RemoveAds()
        {
            PlayerPrefs.GetInt(PPConst.Ads, 1);
        }
        public static int CheckQualitySettings()
        {
            return PlayerPrefs.GetInt(PPConst.QualityLevel);
        }
        public static int CheckControlType()
        {
            return PlayerPrefs.GetInt(PPConst.ControlType);
        }
        public static int CheckBehavior()
        {
            return PlayerPrefs.GetInt(PPConst.VehicleBehavior);
        }
        public static int CheckDamage()
        {
            return PlayerPrefs.GetInt(PPConst.Damage);
        }
        public static int CheckReverseCamera()
        {
            return PlayerPrefs.GetInt(PPConst.ReverseCamera);
        }

        public static void SaveCarColor(int carIndex, int colorNum)
        {
            PlayerPrefs.SetInt("Car" + carIndex + "ColorNumber", colorNum);
        }
        public static int GetCarColor(int carIndex)
        {
            return PlayerPrefs.GetInt("Car" + carIndex + "ColorNumber", 0);
        }

        private static List<string> namesList = new List<string>()
{
    "Anees", "Lakshmi", "Aryan", "Sofia", "Rohan", "Emma", "Ayaan", "Amelia", "Zara", "Ethan", "Muhammad", "Liam",
    "Harper", "Isabella", "Noah", "Sophia", "Oliver", "Olivia", "Ella", "James", "Nathan", "Aiden", "Fatima", "Yusuf",
    "Alex", "Daniel", "Grace", "Mia", "Lara", "Isaac", "Anna", "Eva", "Oscar", "Noor", "Ava", "Alina", "Zayn", "Zainab",
    "Maya", "Aria", "Lucas", "Mila", "Nora", "Amir", "Harriet", "Ella", "Harry", "Isla", "Finn", "Emilia", "George", "Neha",
    // Additional 50 names:
    "Arjun", "Jasmine", "Vivaan", "Chloe", "Veer", "Zoe", "Vihaan", "Alice", "Ananya", "Zoey", "Kabir", "Grace", "Reyansh",
    "Hannah", "Ishan", "Eleanor", "Shlok", "Harper", "Kian", "Madison", "Advait", "Elizabeth", "Kartikeya", "Eva", "Krish",
    "Stella", "Ishaan", "Scarlett", "Pranav", "Sarah", "Shaurya", "Evelyn", "Rudra", "Victoria", "Yash", "Avery", "Vedant",
    "Addison", "Aditya", "Aubrey", "Darsh", "Lily", "Kian", "Nova", "Arnav", "Penelope", "Atharva", "Grace", "Vihaan", "Riley",
    "Arjun", "Hailey", "Veer", "Aria", "Advait", "Luna", "Rudra", "Eliana", "Shaurya", "Emery", "Pranav", "Ivy"
};
        public static List<string> GetRandomNames(int totalNames)
        {
            totalNames.Show();
            List<string> selectedNames = new List<string>();
            while (selectedNames.Count < totalNames)
            {
                string name = namesList[Random.Range(0, namesList.Count)];
                if (selectedNames.Count > 0)
                    if (selectedNames.Contains(name))
                        continue;
                selectedNames.Add(name);
            }
            return selectedNames;
        }
        public static List<int> GetRandomNumbers(int range, int count)
        {
            List<int> randomNumbersList = new List<int>();

            while (randomNumbersList.Count < count)
            {
                int rand = Random.Range(0, range);
                if (randomNumbersList.Contains(rand))
                    continue;
                randomNumbersList.Add(rand);
            }

            return randomNumbersList;
        }

        public static bool CheckIfPrivacyAccepted()
        {
            return PlayerPrefs.GetInt(PPConst.Privacy, 0) == 0 ? false : true;
        }

        public static void PrivacyAccepted()
        {
            PlayerPrefs.SetInt(PPConst.Privacy, 1);
        }
    }

    /// <summary>
    /// This Call is Used To Store Keys of Player Prefs
    /// </summary>
    public class PPConst
    {
        public static string UserName = "UserName";
        public static string Coins = "Coins";
        public static string Car = "CarNumber";
        public static string QualityLevel = "QualityLevel";
        public static string SavedCar = "SavedCar";
        public static string Volume = "Volume";
        public static string Music = "Music";
        public static string ControlType = "ControlType";
        public static string VehicleBehavior = "VehicleBehavior";
        public static string Damage = "Damage";
        public static string ReverseCamera = "ReverseCamera";
        public static string SensitivityVal = "SensitivityValue";
        public static string Map = "Map";
        public static string First = "FirstTime";
        public static string MicType = "MICTYPE";
        public static string Ads = "Ads";
        public static string Privacy = "Privacy";
    }

}
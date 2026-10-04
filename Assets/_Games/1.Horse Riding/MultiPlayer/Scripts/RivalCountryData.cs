using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RivalCountryData : MonoBehaviour
{

    public string countryCode;
    public string[] rivalsCountryCodes;
    //[HideInInspector]
    public Dictionary<string, string[]> rivalCountryBotNames;
    public string countryBotNames;

    public string GetRandomBotName(string rivalCCode)
    {
        string botName = "bot";
        string[] namesArray;
        if (rivalCountryBotNames != null && rivalCountryBotNames.TryGetValue(rivalCCode, out namesArray))
        {
            botName = namesArray[Random.Range(0, namesArray.Length)];
        }
        return botName;
    }

    public string GetRandomPlayerName()
    {

        string[] namesArray = countryBotNames.Split(',');
        return namesArray[Random.Range(0, namesArray.Length)];
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RivalsCountryController : MonoBehaviour
{
    public static RivalsCountryController Instance;
    public RivalCountryData[] rivalCountryDataList;
    [HideInInspector]
    public RivalCountryData countryData;
    private void Awake()
    {
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        SetRivalCountry(PlayerDataController.instance.playerStats.countryCode);
    }

    public void SetRivalCountry(string countryCode)
    {
        countryData = null;
        for (int i = 0; i < rivalCountryDataList.Length; i++)
        {
            if (rivalCountryDataList[i].countryCode.Equals(countryCode))
            {
                countryData = rivalCountryDataList[i];
                countryData.rivalCountryBotNames = new Dictionary<string, string[]>();
                ////Debug.Log("Rivals " + countryData.rivalsCountryCodes[0].ToString());
                for (int j = 0; j < countryData.rivalsCountryCodes.Length; j++)
                {
                    string rCCode = countryData.rivalsCountryCodes[j];
                    for (int k = 0; k < rivalCountryDataList.Length; k++)
                    {
                        if (rivalCountryDataList[k].countryCode.Equals(rCCode))
                        {
                            ////Debug.Log("Rivals ............... " + rivalCountryDataList[k].countryBotNames);

                            countryData.rivalCountryBotNames.Add(rCCode, rivalCountryDataList[k].countryBotNames.Split(','));
                        }
                    }
                }
                break;
            }
        }
    }


}

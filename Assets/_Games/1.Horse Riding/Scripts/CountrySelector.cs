using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CountrySelector : MonoBehaviour
{
    public Image Flag;
    public Dropdown CountryList;

    public Image profilePic;
    public Dropdown awatarList;
    bool isInialized;
    // Start is called before the first frame update
    void OnEnable()
    {
        if (!isInialized)
        {
            CountryList.options.Clear();
            CountryList.onValueChanged.AddListener(OnCountriesListChange);
            //Parse CountriesNames Enum and get list with all Countries names
            List<string> CountriesNames = new List<string>(System.Enum.GetNames(typeof(CountriesFlags.CountriesNames)));
            List<Dropdown.OptionData> CountryListData = new List<Dropdown.OptionData>();
            foreach (var name in CountriesNames)
            {
                CountryListData.Add(new Dropdown.OptionData(CountriesFlags.countryCodes_NamesMapping[name], CountriesFlags.LoadFlag(name)));
            }
          
            //Filling the list in UI
            CountryList.AddOptions(CountryListData);
        }
         Sprite sprite = CountriesFlags.LoadFlag(PlayerDataController.instance.playerStats.countryCode);
        Flag.sprite = sprite;
       // MainMenuManager.Instance.flagMaterial.mainTexture = sprite.texture;
        //OnCountriesListChange(0);
        //Debug.Log("Country Code "+ PlayerDataController.Instance.playerData.countryCode);
        int seleIndex = CountriesFlags.GetIndexByName(CountryList, CountriesFlags.GetNameOfCountry(PlayerDataController.instance.playerStats.countryCode));

        if (PlayerDataController.instance.playerStats.countryCode == null || PlayerDataController.instance.playerStats.countryCode.Equals(""))
        {
            seleIndex = CountriesFlags.GetIndexByName(CountryList, CountriesFlags.GetNameOfCountry("EW"));
        }
       

        CountryList.value = seleIndex;


        SetAwatar();
    }

    public void SetAwatar()
    {
        profilePic.sprite =null;
        //OnCountriesListChange(0);
       
        awatarList.value = PlayerDataController.instance.playerStats.playerAwatarId;

        isInialized = true;

    }

    public void OnCountriesListChange(int flagIndex)
    {
        Sprite sprite = CountriesFlags.LoadFlag((CountriesFlags.CountriesNames)flagIndex);
        Flag.sprite = sprite;
       // MainMenuManager.Instance.flagMaterial.mainTexture = sprite.texture;

        // CountryName.text = CountriesFlags.countryCodes_NamesMapping[((CountriesFlags.CountriesNames)flagIndex).ToString()] + ", " + ((CountriesFlags.CountriesNames)flagIndex).ToString();
        PlayerDataController.instance.playerStats.countryCode = ((CountriesFlags.CountriesNames)flagIndex).ToString();
        RivalsCountryController.Instance.SetRivalCountry(PlayerDataController.instance.playerStats.countryCode);
    }


    public void OnAwatarChange(int awatarId)
    {
        PlayerDataController.instance.playerStats.playerAwatarId = awatarId;

        profilePic.sprite = null;
        
    }


}

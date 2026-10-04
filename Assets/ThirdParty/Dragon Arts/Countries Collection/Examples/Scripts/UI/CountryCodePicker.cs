using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DragonArts.Collection.Countries;

namespace DragonArts.Collection.Countries.Example {

    public class CountryCodePicker : MonoBehaviour { // a Country Code Picker

        [SerializeField] private CountryGroup countries; // The Countries
        [SerializeField] private TMP_Dropdown dropdown;
        [SerializeField] private TextMeshProUGUI labelText;

        private void Awake ()
        {

            /*List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
            foreach (Country c in countries.list) { // Build Options List from Country Data
               
                options.Add(new TMP_Dropdown.OptionData($"<size=125><sprite name=\"{c.isoCode3}\"></size>{c.dialingCode}"));
            }
            */
            //dropdown.options = options;
            dropdown.value = 130;
           // OnValueChanged(130);
           // defaultPakistanSet(130);
        }

        public void OnValueChanged (int optionIndex) {
            Country c = countries.list[optionIndex];
            // Use Country to fill TextMeshProUGUI with data
            labelText.text = c.name;
            staticVariables.countryDialCode_general = c.dialingCode;
        }


  
    }
}

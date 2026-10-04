using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragonArts.Collection.Countries;
using System;
using TMPro;

namespace DragonArts.Collection.Countries.Example {

    public class CompleteListController : MonoBehaviour {
        
        [SerializeField] private CountryGroup countries; // The Countries
        [SerializeField] private Transform table;
        [SerializeField] private GameObject cell;

        private void Awake () {
            foreach (Transform child in table) {
                Destroy(child.gameObject);
            }

            foreach (Country c in countries.list) {
                GameObject go = Instantiate(cell, table);
                // Use Country to fill TextMeshProUGUI with data
                go.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"<size=225><sprite name=\"{c.isoCode3}\"></size>\n<color=white>{c.name}</color>\n<size=75>{c.capitalCity}";
                go.transform.GetChild(0).GetComponent<TextMeshProUGUI>().enabled = true;
            }
        }
    }
}

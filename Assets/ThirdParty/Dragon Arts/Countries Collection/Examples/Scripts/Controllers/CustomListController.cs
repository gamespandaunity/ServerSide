using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DragonArts.Collection.Countries;
using System;
using TMPro;

namespace DragonArts.Collection.Countries.Example {

    public class CustomListController : MonoBehaviour {
        
        [SerializeField] private CountryGroup countries; // The Countries - Custom Country Group
        [SerializeField] private Image flagImage;
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private AudioSource audioSource;

        private int index;

        private void Awake () {
            Redraw();
        }

        private void OnDestroy () {
            // Release Loaded Country Flags
            foreach (Country c in countries.list) {
                c.ReleaseFlag();
                c.ReleaseAnthem();
            }
        }

        private async void Redraw () {
            Country c = countries.list[index]; // Get Country by Index from your Custom List
            flagImage.sprite = await c.LoadFlag(); // Load Country Flag (Addressable)
            // Use Country to fill TextMeshProUGUI with data
            labelText.text = $"<color=white>{c.name}</color>\n<size=75>{c.capitalCity}";

            audioSource.clip = await c.LoadAnthem(); // Load Country Anthem (Bonus Addressable)
            Invoke("StartAnthem", .25f);
        }

        private void StartAnthem () {
            audioSource.Play();
        }

        public void ShowNext () {
            index = index == countries.list.Count - 1 ? 0 : index + 1;
            Redraw();
        }

        public void ShowPrevious () {
            index= index == 0 ? countries.list.Count - 1 : index - 1;
            Redraw();
        }
    }
}

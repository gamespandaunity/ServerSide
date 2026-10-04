using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DragonArts.Collection.Countries.Example {

    public class SimpleButton : MonoBehaviour { // a Button

        [SerializeField] private AudioClip bell;

        private void Awake () {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick () {
            PlayBell();
        }

        public void PlayBell () {
            Camera.main.GetComponent<AudioSource>().PlayOneShot(bell, .25f);
        }
    }
}

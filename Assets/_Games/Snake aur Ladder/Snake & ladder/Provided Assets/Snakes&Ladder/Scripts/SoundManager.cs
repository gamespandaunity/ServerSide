using System.Collections.Generic;
using UnityEngine;
namespace Snake_Ladder
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager instance;

        public enum AllSounds
        {
            Dice,
        }

        //public AllSounds Sound;
        public bool isMuted;
        public GameObject prefab;
        public List<AudioClip> Sounds;

        private List<AudioSource> instantiatedAudioSources = new List<AudioSource>();

        private void Awake()
        {
            if (instance == null)
            {
                // If no instance exists, set this as the instance and mark it to not be destroyed on scene load
                instance = this;
                InstantiateSoundPrefabs();
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // If an instance already exists, destroy this GameObject
                Destroy(gameObject);
                return;
            }
        }

        private void InstantiateSoundPrefabs()
        {
            // Clear the list before instantiating new prefabs
            instantiatedAudioSources.Clear();

            // Instantiate the prefab multiple times based on the count of Sounds
            for (int i = 0; i < Sounds.Count; i++)
            {
                GameObject soundPrefabInstance = Instantiate(prefab, transform.position, Quaternion.identity);

                // Make the instantiated prefab a child of this GameObject
                soundPrefabInstance.transform.SetParent(transform);

                // Access the AudioSource component of the instantiated prefab
                AudioSource audioSource = soundPrefabInstance.GetComponent<AudioSource>();

                // Check if the prefab has an AudioSource component
                if (audioSource != null)
                {
                    // Assign the AudioClip from the Sounds list to the AudioSource
                    audioSource.clip = Sounds[i];

                    // Add the AudioSource component to the list
                    instantiatedAudioSources.Add(audioSource);
                }
                else
                {
                    Debug.LogError("Prefab is missing an AudioSource component!");
                }
            }
        }

        public void PlaySound(AllSounds sound)
        {
            if (isMuted) return;
            int soundIndex = (int)sound;

            if (soundIndex >= 0 && soundIndex < instantiatedAudioSources.Count)
            {
                AudioSource audioSource = instantiatedAudioSources[soundIndex];

                // If the sound is not playing, play it
                audioSource.Play();
            }
        }

        public void SetLoop(AllSounds sound, bool state)
        {
            int soundIndex = (int)sound;
            instantiatedAudioSources[soundIndex].loop = state;
        }
        public void StopSound(AllSounds sound)
        {
            int soundIndex = (int)sound;

            if (soundIndex >= 0 && soundIndex < instantiatedAudioSources.Count)
            {
                instantiatedAudioSources[soundIndex].Stop();
            }
            else
            {
                Debug.LogError("Invalid sound index!" + sound);
            }
        }

        //public void PlayClickSound()
        //{
        //    PlaySound(AllSounds.ClickUi);
        //}
    }
}

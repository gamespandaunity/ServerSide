using UnityEngine;
using System;
using System.Collections;
using Mirror;

namespace TeenPattiGame
{
    public class VoiceRecorder : NetworkBehaviour//MonoBehaviourPunCallbacks
    {
        private AudioClip recordedAudio;
        //private byte[] audioData;
        public GameObject voiceRecordIndicator;
        public GameObject VoiceBoxPrefab;
        AudioSource audioSouce;
        NetworkIdentity photonView;
        [ShowOnly] public int ProfilePicIndex;

        private const int ChunkSize = 52400; // Adjust the chunk size as needed

        private byte[] audioData;
        private int currentChunkIndex;
        int picIndex;
        bool isMineSent;
        private const int frequency = 44100;
        // instance Creating of Sound recorder Script
        #region Creating Instance;
        private static VoiceRecorder _instance;
        public static VoiceRecorder Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<VoiceRecorder>();
                }
                return _instance;
            }
        }

        // private void Awake()
        // {
        //     if (_instance == null)
        //     {
        //         _instance = this;
        //     }
        //     voiceRecordIndicator.SetActive(false);
        // }
        #endregion

        // private void Start()
        // {
        //     photonView = GetComponent<NetworkIdentity>();
        // }
        // // Start recording audio
        // float recordingStartTime, recordedAudioDuration;
        // public void StartRecording()
        // {
        //     recordingStartTime = Time.time;
        //     voiceRecordIndicator.SetActive(true);
        //     recordedAudio = Microphone.Start(null, false, 6, frequency);
        // }

        // // Stop recording audio and prepare for transmission
        // public void StopRecording()
        // {
        //     recordedAudioDuration = Time.time - recordingStartTime;
        //     Microphone.End(null);
        //     voiceRecordIndicator.SetActive(false);

        //     // Adjust the audio clip length to match the actual recorded duration
        //     int actualLengthSamples = (int)(frequency * recordedAudioDuration);
        //     AudioClip trimmedAudioClip = AudioClip.Create("TrimmedAudio", actualLengthSamples, 1, frequency, false);
        //     float[] data = new float[actualLengthSamples];
        //     recordedAudio.GetData(data, 0);
        //     trimmedAudioClip.SetData(data, 0);
        //     recordedAudio = trimmedAudioClip;

        //     sendingSound();
        // }

        void sendingSound()
        {
            //byte[] fullAudioData = ConvertAudioClipToByteArray(recordedAudio);
            byte[] fullAudioData = WavUtility.FromAudioClip(recordedAudio);

            // Calculate the number of chunks needed
            //int totalChunks = Mathf.CeilToInt((float)fullAudioData.Length / ChunkSize);

            // Send audio data in chunks through RPC
            //currentChunkIndex = 0;
            Debug.LogError("full chunk data: " + fullAudioData.Length);
            isMineSent = true;
            //photon removal  picIndex = UIManager.Instance.GetMyPlayerInfo().player.GetCustomData(LocalSettings.ProfilePic);
            //photon removal  photonView.RPC(nameof(getPicIndexRPC), RpcTarget.All, picIndex);
            DirectAssignAudio();
            //photon removal   photonView.RPC(nameof(ReceiveAudioChunkRPC), RpcTarget.All, fullAudioData);

            //if (GameManager.Instance.playersList.Count > 1)
            //{
            //    for (int i = 0; i < totalChunks; i++)
            //    {
            //        int startIndex = i * ChunkSize;
            //        int chunkLength = Mathf.Min(ChunkSize, fullAudioData.Length - startIndex);
            //        byte[] chunk = new byte[chunkLength];
            //        Buffer.BlockCopy(fullAudioData, startIndex, chunk, 0, chunkLength);

            //        // Send the chunk using PUN RPC
            //        if (GameManager.Instance.playersList.Count > 1)
            //        {
            //            viewID = UIManager.Instance.GetMyPlayerInfo().photonView.ViewID;
            //            photonView.RPC(nameof(ReceiveAudioChunkRPC), RpcTarget.All, chunk, currentChunkIndex, totalChunks);
            //            currentChunkIndex++;
            //        }
            //        yield return new WaitForSeconds(0.5f);
            //    }
            //}
        }
        int viewID = 0;
        public void PlayThisAudioClip(AudioClip audioClip)
        {
            if (audioSouce == null)
                audioSouce = SoundManager.Instance.PlayAudioClip(audioClip, false, true);
            else if (audioSouce.isPlaying)
            {
                audioSouce.clip = audioClip;
                audioSouce.Play();
            }
            else
                audioSouce = SoundManager.Instance.PlayAudioClip(audioClip, false, true);
        }
        #region earlier work Comment
        //void SendAudioToAll(AudioClip audioClip  , float RecodedAudioDuration)
        //{
        //    //AudioClip recordedVoice = CompressAudioWithOpus(audioClip);

        //    byte[] audioData = ConvertAudioClipToByteArray(audioClip);
        //    Debug.LogError("Audio byte length: " + audioData.Length + "     Array is: " + audioData);
        //    photonView.RPC(nameof(SendAudioToAllRPC), RpcTarget.All, audioData/*, recordedAudioDuration*/);
        //}
        //[PunRPC]
        //public void SendAudioToAllRPC(byte[] recordedVoiceBytes/*, float RecodedAudioDuration*/)
        //{
        //    GameObject voiceBox = Instantiate(VoiceBoxPrefab);
        //    LocalSettings.SetPosAndRect(voiceBox, VoiceBoxPrefab.GetComponent<RectTransform>(), VoiceBoxPrefab.transform.parent);
        //    AudioClip recordedVoice = ConvertByteArrayToAudioClip(recordedVoiceBytes);
        //    //float RecodedAudioDuration = recordedAudio.length;
        //    //voiceBox.GetComponent<VoiceHolder>().AssignAudioClip(recordedVoice, RecodedAudioDuration);
        //    voiceBox.GetComponent<VoiceHolder>().AssignAudioClip(recordedVoice, 1);
        //    voiceBox.SetActive(true);
        //    //Debug.LogError("Time of clip: " + recordedAudio.length);
        //}

        //void SendAudioToAll(AudioClip audioClip, float RecodedAudioDuration)
        //{
        //    audioData = AudioUtility.AudioClipToByteArray(audioClip);
        //    //Debug.LogError("Audio byte length: " + audioData.Length + "     Array is: " + audioData);
        //    //photonView.RPC(nameof(SendAudioToAllRPC), RpcTarget.All, audioData, recordedAudioDuration);
        //    photonView.RPC(nameof(ReceiveAudioChunkRPC), RpcTarget.All, audioData.Length, audioData);
        //}
        #endregion
        //photon removal  [PunRPC]
        void getPicIndexRPC(int picIndexx)
        {
            picIndex = picIndexx;
        }
        //photon removal  [PunRPC]
        private void ReceiveAudioChunkRPC(byte[] audioBytes)
        {
            AudioClip ac = WavUtility.ToAudioClip(audioBytes, 1, "RecodedVoice");
            GameObject voiceBox = Instantiate(VoiceBoxPrefab);
            LocalSettings.SetPosAndRect(voiceBox, VoiceBoxPrefab.GetComponent<RectTransform>(), VoiceBoxPrefab.transform.parent);
            //  Sprite profileSprite = GameManager.Instance.PlayerProfileImages.Sprites[picIndex];
            Sprite profileSprite = LocalSettings.ServerSideImge;
            voiceBox.GetComponent<VoiceHolder>().AssignAudioClip(ac, profileSprite);
            voiceBox.SetActive(true);
            PlayThisAudioClip(ac);
            //if (UIManager.Instance.GetMyPlayerInfo().photonView.ViewID == viewID)
            //{
            //    viewID = 0;
            //    Debug.LogError("I am a sender");
            //    return;
            //}
            //if (chunkIndex == 0)
            //{
            //    audioData = new byte[chunk.Length * totalChunks];
            //}

            //Buffer.BlockCopy(chunk, 0, audioData, chunkIndex * chunk.Length, chunk.Length);
            //Debug.LogError("chunk Index: " + chunkIndex + "/" + totalChunks + "chunk length: " + chunk.Length);
            //AudioClip ac = null;
            // If all chunks have been received, reconstruct the audio and play it
            //if (chunkIndex == totalChunks - 1)
            //{
            //    AudioClip receivedClip = ConvertByteArrayToAudioClip(audioData);
            //    ac = receivedClip;

            //    GameObject voiceBox = Instantiate(VoiceBoxPrefab);
            //    LocalSettings.SetPosAndRect(voiceBox, VoiceBoxPrefab.GetComponent<RectTransform>(), VoiceBoxPrefab.transform.parent);
            //    Sprite profileSprite = GameManager.Instance.PlayerProfileImages.Sprites[picIndex];
            //    voiceBox.GetComponent<VoiceHolder>().AssignAudioClip(ac, profileSprite);
            //    voiceBox.SetActive(true);
            //    PlayThisAudioClip(ac);
            //}
        }

        void DirectAssignAudio()
        {
            GameObject voiceBox = Instantiate(VoiceBoxPrefab);
            LocalSettings.SetPosAndRect(voiceBox, VoiceBoxPrefab.GetComponent<RectTransform>(), VoiceBoxPrefab.transform.parent);
            // Sprite profileSprite = GameManager.Instance.PlayerProfileImages.Sprites[picIndex];
            Sprite profileSprite = LocalSettings.ServerSideImge;
            voiceBox.GetComponent<VoiceHolder>().AssignAudioClip(recordedAudio, profileSprite);
            voiceBox.SetActive(true);
            PlayThisAudioClip(recordedAudio);
        }

        #region   Audio to bytes and bytes to audio

        // private byte[] ConvertAudioClipToByteArray(AudioClip audioClip)
        // {
        //     var audioData = new float[audioClip.samples];
        //     audioClip.GetData(audioData, 0);

        //     var byteArray = new byte[audioData.Length * 4];
        //     Buffer.BlockCopy(audioData, 0, byteArray, 0, byteArray.Length);
        //     return byteArray;
        // }

        // private AudioClip ConvertByteArrayToAudioClip(byte[] byteArray)
        // {
        //     var audioData = new float[byteArray.Length / 4];
        //     Buffer.BlockCopy(byteArray, 0, audioData, 0, byteArray.Length);

        //     var audioClip = AudioClip.Create("ReceivedAudio", audioData.Length, 1, 44100, false);
        //     audioClip.SetData(audioData, 0);
        //     return audioClip;
        // }

        #endregion
    }
}
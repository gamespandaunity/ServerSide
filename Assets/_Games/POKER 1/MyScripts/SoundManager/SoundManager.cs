namespace POKER 
{
using POKER;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField]
    SoundMgrScriptable AllSoundsAsset;
    Transform AudioSourcePoolParent;

    public static SoundMgrScriptable AllSounds;

        #region Creating Instance
        public static POKER.SoundManager Instance;
    //public static POKER.SoundManager Instance
    //{
    //    get
    //    {
    //        if (_instance == null)
    //        {
    //            _instance = FindObjectOfType<SoundManager>();
    //         //   DontDestroyOnLoad(_instance.gameObject);
    //        }
    //        return _instance;
    //    }
    //}
    #endregion
    void Awake()
    {

        if (Instance == null)
        {
                Instance = this;
            //DontDestroyOnLoad(this);
            AllSounds = AllSoundsAsset;
        }
        else
        {
            if (this != Instance)
                Destroy(this.gameObject);
        }
    }
    AudioSource ASource;
    public void PlayAudioClip(AudioClip audioClip, bool isLoop)
    {
        if (!LocalSettings.GetSoundEffect())
            return;
        Vector3 position = Vector3.zero;
        getAudioSource(position);
        ASource.loop = isLoop;
        ASource.clip = audioClip;
        ASource.playOnAwake = true;
        if (isLoop)
            ASource.Play();
        else
            ASource.PlayOneShot(audioClip);
        ASource.name = "AS: " + audioClip.name;
    }
    int soundNumber;
    public AudioSource PlayAudioClip(AudioClip audioClip, bool isLoop, bool isReturntype)
    {
        Vector3 position = Vector3.zero;
        getAudioSource(position);
        ASource.loop = isLoop;
        ASource.clip = audioClip;
        ASource.playOnAwake = true;
        if (isLoop)
            ASource.Play();
        else
            ASource.PlayOneShot(audioClip);
        ASource.name = "AS: " + audioClip.name;
        return ASource;
    }
    void getAudioSource(Vector3 position)
    {
        ASource = null;
        if (!AudioSourcePoolParent)
        {
            AudioSourcePoolParent = (new GameObject("AudioSourcesPoolParent")).transform;
        }
        if (AudioSourcePoolParent.childCount > 0)
        {
            for (int i = 0; i < AudioSourcePoolParent.childCount; i++)
            {
                if (!AudioSourcePoolParent.GetChild(i).gameObject.GetComponent<AudioSource>().isPlaying)
                {
                    ASource = AudioSourcePoolParent.GetChild(i).gameObject.GetComponent<AudioSource>();
                    ASource.gameObject.transform.position = position;
                    break;
                }
            }
        }
        if (!ASource)
        {
            GameObject Audiosource = Instantiate(AllSounds.AudioSourcePrefab, position, Quaternion.identity);
            Audiosource.transform.parent = AudioSourcePoolParent;
            ASource = Audiosource.GetComponent<AudioSource>();
        }
        //Debug.LogError("playing: " + soundNumber++);
    }
}
}
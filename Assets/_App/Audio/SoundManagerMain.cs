using UnityEngine;

public class SoundManagerMain : MonoBehaviour
{
    public static SoundManagerMain instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            DontDestroyOnLoad(gameObject);
        }
    }
    private void Start()
    {

        SetDefaultPlayerPrefs(); //RAR
    }


    public void SetDefaultPlayerPrefs() //RAR
    {
        if (!PlayerPrefs.HasKey("Sound"))
        {
            PlayerPrefs.SetInt("Sound", 1);
        }
    }

    public AudioSource clickSound;

    public void ClickSoundPlay()
    {
        clickSound.Play();
    }


}

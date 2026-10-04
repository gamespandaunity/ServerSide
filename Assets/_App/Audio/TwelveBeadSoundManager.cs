using UnityEngine;

public class TwelveBeadSoundManager : MonoBehaviour
{
    public AudioClip[] clips;
    public AudioSource EffectSource;
    public static TwelveBeadSoundManager instance;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        
    }

    public void PlayAnySound(int index)
    {
        EffectSource.PlayOneShot(clips[index]);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}

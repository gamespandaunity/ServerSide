namespace POKER 
{
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SoundAction : MonoBehaviour
{
    public enum SoundType
    {
        BUTTONCLICK
    }

    public SoundType CurrentSound;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(PlaySound);
    }

    void PlaySound()
    {
        if (CurrentSound == SoundType.BUTTONCLICK)
            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ButtonSound, false);
        //Debug.LogError("Checking sound click count");
    }
}

}
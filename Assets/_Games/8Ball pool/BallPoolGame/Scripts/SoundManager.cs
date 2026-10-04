using UnityEngine;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;

            DontDestroyOnLoad(gameObject);
        }
    }



    private void Update() //RAR
    {
        if (SceneManager.GetActiveScene().buildIndex == 2)
        {
            Destroy(gameObject);
        }
    }




    public AudioSource pieceAdd;
    public AudioSource clickSound;


    public void PieceAddSound()
    {
        pieceAdd.Play();
    }

    public void ClickSoundPlay()
    {
        clickSound.Play();
    }
}

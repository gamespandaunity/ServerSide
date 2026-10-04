using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManagerMainMenu : MonoBehaviour
{
    public static MusicManagerMainMenu instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

         //   DontDestroyOnLoad(gameObject);
        }else
        {
            Destroy(gameObject);
        }
    }



    public AudioSource musicBG;



    private void Update()
    {
        if (SceneManager.GetActiveScene().buildIndex == 1) // login panel
        {
            Destroy(gameObject);
        }
        else if (SceneManager.GetActiveScene().buildIndex == 6) // 8 ball pool
        {
            Destroy(gameObject);
        }
        else if (SceneManager.GetActiveScene().buildIndex == 10) // carrom
        {
            Destroy(gameObject);
        }
        else if (SceneManager.GetActiveScene().buildIndex == 16) // poker
        {
            Destroy(gameObject);
        }
        else if (SceneManager.GetActiveScene().buildIndex == 20) // chess
        {
            Destroy(gameObject);
        }
        else if (SceneManager.GetActiveScene().buildIndex == 25) // ludo
        {
            Destroy(gameObject);
        }
    }
}

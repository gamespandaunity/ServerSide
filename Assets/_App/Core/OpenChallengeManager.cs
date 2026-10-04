using UnityEngine;
using UnityEngine.SceneManagement;

public class OpenChallengeManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }



    public void BackBtntoSilverCoinScene()
    {
        SceneLoaderUtility.LoadScene("PlaywithFriendSilverCoinsScene");
    }

    public void YourChallengesBtn()
    {
        SceneLoaderUtility.LoadScene("Home");
    }
}

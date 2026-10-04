using NetworkManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManagerMainMenu : MonoBehaviour
{
    void Start()
    {
        SetDefaultPlayerPrefs(); //RAR
        Physics.simulationMode = SimulationMode.FixedUpdate;
    }


    public void SetDefaultPlayerPrefs() //RAR
    {
       
    }
    public void SetSilverCoin(int modeId)// aight ball
    {
        GameModeManager. isAI = false;
        staticVariables.isgoldcoins = false;
        ApiAndRoomManager.currentGameId =1;
        AightBallPoolNetworkGameAdapter.is3DGraphics = modeId == 0;
        //SceneLoaderUtility.LoadScene (homeScene);

        SoundManagerMain.instance.ClickSoundPlay();
        //print("test multi silver coins");
        //print("staticVariables.isgoldcoins ");
    }

    public void SetgoldGameMode(int modeId) // aight ball

    {
        GameModeManager.isAI = false;
        staticVariables.isgoldcoins = true;
        ApiAndRoomManager.currentGameId = 1;
        SoundManagerMain.instance.ClickSoundPlay();
        AightBallPoolNetworkGameAdapter.is3DGraphics = modeId == 0;
        //SceneLoaderUtility.LoadScene (homeScene);
        

        //SceneLoaderUtility.LoadScene("PlaywithFriendSilverCoinsScene");


        //print("test gold coins");
    }
    public void carromSilverMode()
    {
        staticVariables.isgoldcoins = false;
        
        ApiAndRoomManager.currentGameId = 2;

        SceneLoaderUtility.LoadScene("PlaywithFriendSilverCoinsScene");

        //print("test multi silver coins");
    }

    public void carromGoldMode()
    {

        staticVariables.isgoldcoins = true;
        ApiAndRoomManager.currentGameId = 2;
        

        SceneLoaderUtility.LoadScene("PlaywithFriendSilverCoinsScene");


        //print("test gold coins");
    }
}

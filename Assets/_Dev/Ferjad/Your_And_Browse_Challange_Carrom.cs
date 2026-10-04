using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class Your_And_Browse_Challange_Carrom : MonoBehaviour
{
    [SerializeField] private Text main_Palyer_name;
    [SerializeField] private InputField betAmontInput;
    [SerializeField] private GameObject your_Browse_Panel, EnterBetPanel, browseAllChallengePanel;
    private const string prefValue_betAmount = "betAmount_carrom";
    [SerializeField] private GameObject roomListContainer;
    [SerializeField] private GameObject RoomList_t;
    public static int enteredBetAmount_Carrom;

    void Start()
    {
        UserModel userModel2 = staticVariables.UserProfiledata;
        main_Palyer_name.text = userModel2.user.first_name + " " + userModel2.user.last_name;


    }
    public void BetAmount_Save()
    {
        enteredBetAmount_Carrom = int.Parse(betAmontInput.text);
        StartCoroutine(UpdateRoom());
    }

    public void YourChallenge_()
    {
        EnterBetPanel.gameObject.SetActive(true);
        your_Browse_Panel.gameObject.SetActive(false);
    }
    //  BACK BUTTON YOUR CHALLENGE PANEL TO PLAYWITHFRIEND SILVER COIN SCENE
    public void Back_YourChallengeToPlayWithFriendSilverCoin()
    {
        SceneLoaderUtility.LoadScene("PlaywithFriendSilverCoinsScene");
    }
    // BACK BUTTON  BROWSE CHALLENGE TO YOUR CHALLENGE 
    public void Back_BrowseChallenge_To_YourChallenge()
    {
        browseAllChallengePanel.gameObject.SetActive(false);
        your_Browse_Panel.gameObject.SetActive(true);
    }
    // BACK BUTTON  ENTER BET AMOUNT TO YOUR CHALLENGE
    public void Back_EnterBet_To_YourChallenge()
    {
        EnterBetPanel.gameObject.SetActive(false);
        your_Browse_Panel.gameObject.SetActive(true);
    }
    public void BrowseAllChallenge_()
    {
        browseAllChallengePanel.gameObject.SetActive(true);
        your_Browse_Panel.gameObject.SetActive(false);
    }

    List<GameObject> InRoomPlayers = new List<GameObject>();
    IEnumerator UpdateRoom()
    {
        
      
        yield return new WaitForSeconds(0.2f);





    }
    
}

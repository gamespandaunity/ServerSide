using BallPool;
using NetworkManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VersusPanel_Winner : MonoBehaviour
{
    [SerializeField] private Text playerTextField , coinCollectorMain_Text  ;

    [SerializeField] private RectTransform winnterTransform , playerParent , otherPlayerParent;

     private int betAmountDouble, initiator = 0;

    
    

    
    

    [SerializeField] private float delayDecrementAmount;
    
  
    [SerializeField] private Sprite golden, silver;
    

    // Start is called before the first frame update
    void Start()
    {
        betAmountDouble = EightBallPoolNetworkManager.mainPlayer.prize * 2;
        coinCollectorMain_Text.text = betAmountDouble.ToString();
        //print(betAmountDouble + "      " + " double bet");



     

    }


    bool isCompleted = false;
    private float t = 0.0f;

    // Update is called once per frame
    void Update()
    {


        if (betAmountDouble > initiator && !isCompleted)
        {

            StartCoroutine(InitiatorIncrementor());
        }

        
      
    }
    
    IEnumerator InitiatorIncrementor()
    {
        isCompleted = true;
        while (betAmountDouble > 0)
        {
            if(betAmountDouble > 20  && betAmountDouble <= 50)
            {
                betAmountDouble = Mathf.Max(0, betAmountDouble - 10);
            }else if(betAmountDouble > 50 && betAmountDouble <= 100)
            {
                betAmountDouble = Mathf.Max(0, betAmountDouble - 20);
            }
            else if(betAmountDouble > 100 && betAmountDouble <= 200)
            {
                betAmountDouble = Mathf.Max(0, betAmountDouble - 30);
            }
            else
            {
                betAmountDouble = Mathf.Max(0, betAmountDouble - 55);
            }
            coinCollectorMain_Text.text = betAmountDouble.ToString();
            yield return new WaitForSeconds(delayDecrementAmount);
        }

    }
    public bool IsGameWin = false;
    private void GameWin(BallPoolPlayer winner)
    {


        IsGameWin = true;

        if (GameModeManager.isAI) // ai mode
        {
            if (winner.playerId == AightBallPoolPlayer.mainPlayer.playerId)
            {
                //playerTxtAIMode.text = "You";
                // SetParentAndPosition_WinnerTxt(playerParent, 124f);
                ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());

                //ApiAndRoomManager._instance.winLossPlayerAPi();
            }
            else
            {

                // AITxtAIMode.text = "AI";
             //   SetParentAndPosition_WinnerTxt(otherPlayerParent, 124f);
               
                //print("AI won AI Mode");
                ApiAndRoomManager._instance.WinnerLossAIChallenge("ai");

                //ApiAndRoomManager._instance.winLossPlayerAPi();
                //        Versus_P.GetComponent<VersusPanel_Winner>().enabled = true;
            }
        }
        else  // multiplayer mode
        {
            if (winner.playerId == AightBallPoolPlayer.mainPlayer.playerId)
            {


                ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());

              
            }
            else
            {

                ApiAndRoomManager._instance.WinnerLossChallenge(EightBallPoolNetworkManager.opponentPlayer.userId.ToString());
             
            }
        }



    }
    private void SetParentAndPositiwon_WinnerTxt(RectTransform parent, float y_val)
    {
        winnterTransform.gameObject.SetActive(true);

        winnterTransform.transform.SetParent(parent.transform);


        winnterTransform.anchorMin = new Vector2(0.5f, 0.5f);
        winnterTransform.anchorMax = new Vector2(0.5f, 0.5f);

        // Set the pivot to (0.5, 0.5) for consistent scaling and rotation around the center
        winnterTransform.pivot = new Vector2(0.5f, 0.5f);

        // Set the y position to -124
        winnterTransform.anchoredPosition = new Vector2(0f, y_val);
    }

   

}

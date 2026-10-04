using NetworkManagement;
 
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CarromWinPanelHandler : MonoBehaviour
{
    [SerializeField] private Text WinplayerText, looserPlayerText , WinnerCoinCounter, looserCoinCounter;
    [SerializeField] int betAmount , secondBetAmount ,initiator = 0;
   
    [SerializeField] private float waitSeconds, closeConfettiWait;
    public GameObject confetti1, confetti2, firework;

    [SerializeField] private AudioSource collectCoinsSource;
    [SerializeField] private AudioClip coinCollectionClips;
    bool isDecrementing =false, isCompleted = false;

    public Image[] coinmoving;
   
    public Image chest;
    UserModel userModel;
    int totalBalance;
    public GameObject Coin2;
    private void OnEnable()
    {

 UserModel userModel2 = staticVariables.UserProfiledata;
        // set coin 
        if (staticVariables.isgoldcoins)
        {
            foreach (Image coin in coinmoving)
            {
                coin.sprite = SpritesManager.Instance.spritesScriptable.goldenCoin;
            }
            chest.sprite = SpritesManager.Instance.spritesScriptable.goldChest;  
            WinnerCoinCounter.text = userModel.user.silver_balance.ToString();
            totalBalance =userModel.user.gold_balance;
        }
        else
        {
            foreach (Image coin in coinmoving)
            {
                coin.sprite = SpritesManager.Instance.spritesScriptable.silvercoin;
            }
            chest.sprite = SpritesManager.Instance.spritesScriptable.silverChest;
            WinnerCoinCounter.text = userModel.user.gold_balance.ToString();
            totalBalance = userModel.user.silver_balance;
        }
       
    }

    

    // Start is called before the first frame update
    void Start()
    {
        //Photon Removal  WinplayerText.text = PhotonNetwork.LocalPlayer.NickName;
        //* looserPlayerText.text = CarromGameManager.Instance.OpponentPlayer.NickName;

        betAmount = EightBallPoolNetworkManager.mainPlayer.prize;
        secondBetAmount = EightBallPoolNetworkManager.mainPlayer.prize;
        looserCoinCounter.text = EightBallPoolNetworkManager.mainPlayer.prize.ToString();
        WinnerCoinCounter.text = EightBallPoolNetworkManager.mainPlayer.prize.ToString();
        collectCoinsSource.clip = coinCollectionClips;
        collectCoinsSource.loop = false;
        collectCoinsSource.Play();
        Coin2.SetActive(true);
        StartCoroutine(ParticleToggle());

        if (betAmount > 0 && !isDecrementing)
        {
            StartCoroutine(DecrementBetAmount());

        }
        if (initiator <= betAmount && !isCompleted)
        {

            StartCoroutine(InitiatorIncrementor());
        }
    }

    IEnumerator ParticleToggle()
    {
        confetti1.gameObject.SetActive(true);
        confetti2.gameObject.SetActive(true);
       
        yield return new WaitForSeconds(closeConfettiWait);   
        confetti1.gameObject.SetActive(false);
        confetti2.gameObject.SetActive(false);
        firework.gameObject.SetActive(true);
    }
    void Update()
    {

      

    }
    IEnumerator DecrementBetAmount()
    {
        isDecrementing = true;


        while (betAmount > 0)
        {
            if (betAmount >= 20 && betAmount <= 100)
            {
                betAmount = Mathf.Max(0, betAmount - 5);
            }
            else if (betAmount > 100 && betAmount <=2100)
            {
                betAmount = Mathf.Max(0, betAmount - 50);
            }
            else
            {
                betAmount = Mathf.Max(0, betAmount - 550);
            }

            //if (staticVariables.isgoldcoins)
            //{

            //    totalBalance += betAmount;

            //}
            //else
            //{

            //    totalBalance += betAmount;
            //}

            looserCoinCounter.text = betAmount.ToString();
            yield return new WaitForSeconds(waitSeconds); // Adjust the delay to control the speed
        }


    }
    IEnumerator InitiatorIncrementor()
    {
        isCompleted = true;
        while (initiator < secondBetAmount)
        {
           
            if (secondBetAmount > 0 && secondBetAmount <= 100)
            {
                //initiator += 20;
               initiator = Mathf.Min(initiator + 20, secondBetAmount);
            }
            else if (secondBetAmount > 100 && secondBetAmount <= 200)
            {
                //initiator += 35;
                 initiator = Mathf.Min(initiator + 35, secondBetAmount);
            }
            else if(secondBetAmount > 200 && secondBetAmount <= 1000)
            {
              //  initiator += 110;
                 initiator = Mathf.Min(initiator + 110, secondBetAmount);

            }
            else
            {
                  //initiator += 850;
             initiator = Mathf.Min(initiator  + 850, secondBetAmount);
            }
            totalBalance = initiator;
            //if (staticVariables.isgoldcoins)
            //{

            //    totalBalance += initiator;

            //}
            //else
            //{

            //    totalBalance += initiator;
            //}

            WinnerCoinCounter.text = totalBalance.ToString();
            yield return new WaitForSeconds(waitSeconds);
        }

    }


}

   

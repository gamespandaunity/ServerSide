using NetworkManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CoinsCollecto : MonoBehaviour
{

    [SerializeField] private float delayAnimRight2, decrementBetWait;
    [SerializeField] private float closePanelWait = 4f;
    [SerializeField] private TextMeshProUGUI betAmountContainer_Text;
    [SerializeField] private Sprite golden, silver;
    private bool isDecrementing = false;
    public int betAmount;
    public Image[] coins;
    bool isZero = false;
    private float t = 0.0f;

    [SerializeField] private GameObject animSecond;

    // Start is called before the first frame update
    private void Awake()
    {
        //staticVariables.gamesId = "1";
        if (staticVariables.isgoldcoins)
        {
            for (int i = 0; i < coins.Length; i++)
            {
                coins[i].sprite = golden;

            }
        }
        else
        {
            for (int i = 0; i < coins.Length; i++)
            {
                coins[i].sprite = silver;

            }
        }



        if (GameUIManager.isAi)
        {
            betAmount = GameUIManager.betAmount;
        }
        else
        {

            betAmount = EightBallPoolNetworkManager.mainPlayer.prize; // update bet amont here // old value 500   //RAR
        }
        betAmountContainer_Text.text = betAmount.ToString();
    }
    private void OnEnable()
    {
    }
    void Start()
    {

        StartCoroutine(Activate2AnimRight());
        if (betAmount > 0 && !isDecrementing)
        {
            StartCoroutine(DecrementBetAmount());
            StartCoroutine(ClosePanels());
        }
    }
    IEnumerator Activate2AnimRight()
    {
        yield return new WaitForSeconds(delayAnimRight2);
        animSecond.gameObject.SetActive(true);
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
            else if (betAmount > 100 && betAmount <= 1000)
            {
                betAmount = Mathf.Max(0, betAmount - 60);
            }

            else
            {
                betAmount = Mathf.Max(0, betAmount - 850);
            }

            betAmountContainer_Text.text = betAmount.ToString();
            yield return new WaitForSeconds(decrementBetWait); // Adjust the delay to control the speed
        }


    }
    IEnumerator ClosePanels()
    {
        yield return new WaitForSeconds(closePanelWait);
                  // HomeMenuManager.instance.GoToPlayWithAI();
    }


}






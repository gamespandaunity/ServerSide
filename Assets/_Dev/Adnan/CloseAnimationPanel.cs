using DG.Tweening;
using NetworkManagement;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CloseAnimationPanel : MonoBehaviour
{
    public GameObject reversePanel;
    public GameObject reverseTextPanel;


    public Image logo, bg, chest, totalBlncContainer;
    public Sprite PoolballLogo, CarromSprite, ludo, poker;

    public Image[] coinmoving;
    public Sprite silverCoin, goldCoins;
    [SerializeField] private float movementDelays;
    private bool isDecrementing = false, isIncrementing = false;
    public int betAmount;
    [SerializeField] private TextMeshProUGUI betAmountContainer_LeftText, betAmountContainer_RightText;

    public AudioSource closePanelSourcel;
    public AudioClip panelCloseClip;
    public RawImage rawProfileImage;
    UserModel userModel;
    [SerializeField] Text playerName;
    int incrementedValue = 0, totalAmount = 0;
    [Header("INSIDE CHEST COIN")]
    [Tooltip("FILL ARRAY FOR ON CREATE CHALLENGE PANEL NOT FOR REVERSE  {keep active in scene }")] public Image[] coinInChest;
    [SerializeField] float invokeDelayVal, fadeDuration;
    private Vector3 chestScale, chestPos;
    [Tooltip("keep acrive in reverse panel")] public Image reverse_FullCoin;
    public static bool isPanelAnimated = false;


    public Sprite totalblnceSprite_s, totalblnceSprite_g;
    public TextMeshProUGUI totalBlncContainerText;

    public static CloseAnimationPanel instance;
    private void Awake()
    {
        instance = this;
    }
    void UpdateUserCoins(UserBalanceResponse coins)
    {
        ////print("update");
        totalBlncContainerText.text = staticVariables.isgoldcoins ? coins.data.gold_balance.ToString() : coins.data.silver_balance.ToString();
    }
    void OnEnable()
    {
        UpdateUserCoins(ApiAndRoomManager.LastFetchedCoins);
        ApiAndRoomManager.OnCoinsUpdated += UpdateUserCoins;

        SpritesManager.Instance.spritesScriptable.BgHandlerAccordingTo_Id(bg);
        StartCoroutine(PlaySound());
        userModel = staticVariables.UserProfiledata;



        OnReverseChallenge_Panel();

        DeActivatePanel();
        incrementedValue = 0;
        totalAmount = 0;


        if (EightBallPoolNetworkManager.mainPlayer != null && EightBallPoolNetworkManager.mainPlayer.prize != 0)
        {
            betAmount = RoomsListManager.instance.currentRoomPrize; // update bet amont here
            betAmountContainer_LeftText.text = betAmount.ToString();
            totalAmount = betAmount;
            betAmountContainer_RightText.text = "0";
        }
        else
        {
            ConstantsData_M.Log(" main prize null ");
        }
        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(logo);
        // set coin 
        if (staticVariables.isgoldcoins)
        {
            totalBlncContainer.sprite = totalblnceSprite_g;
            foreach (Image coin in coinmoving)
            {
                coin.sprite = goldCoins;
            }
            foreach (Image coin in coinInChest)
            {
                coin.sprite = goldCoins;
            }
        }
        else
        {
            totalBlncContainer.sprite = totalblnceSprite_s;
            foreach (Image coin in coinmoving)
            {
                coin.sprite = silverCoin;
            }
            foreach (Image coin in coinInChest)
            {
                coin.sprite = silverCoin;
            }
            reverse_FullCoin.sprite = SpritesManager.Instance.spritesScriptable.coinsInSilver;
        }
        if (betAmount > 0 && !isDecrementing)
        {
            StartCoroutine(DecrementBetAmount());
        }
        if (incrementedValue == 0 && !isIncrementing)
        {
            StartCoroutine(IncrementBetAmount());
        }
       // //print(userModel);
        playerName.text = userModel.user.first_name;//HomeMenuManager.instance.barBG.transform.localScale = Vector3.zero;

        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(logo);
    }


    #region REVERSE CALLENGE
    public void OnReverseChallenge_Panel()
    {
        if (gameObject.name == "ReversChallengeLoadingPanel")
        {
            reverse_FullCoin.color = new Color(reverse_FullCoin.color.r, reverse_FullCoin.color.g, reverse_FullCoin.color.b, 1);
        }
        Invoke(nameof(ActiveFader), invokeDelayVal);
    }
    private void ActiveFader()
    {
        reverse_FullCoin.DOFade(0, fadeDuration).SetEase(Ease.Linear);
    }
    #endregion
    public void Panel() //RAR
    {
        closePanelSourcel.PlayOneShot(panelCloseClip);
        this.gameObject.SetActive(false);
    }
    public void DeActivatePanel() //RAR
    {
        Invoke("Panel", 7f);
    }
    private void Start()
    {
        StartCoroutine(waitingToDisable());
        reverseTextPanel.SetActive(false);
        closePanelSourcel = closePanelSourcel.GetComponent<AudioSource>();
        closePanelSourcel.clip = panelCloseClip;
        setProfile();
    }
    IEnumerator PlaySound()
    {
        yield return new WaitForSeconds(4f);
        yield return new WaitForSeconds(3f);
        if (closePanelSourcel != null)
        {
            closePanelSourcel.PlayOneShot(panelCloseClip);
        }
        else
        {
            ConstantsData_M.Log("audio source null");
        }
    }

    public void InSideChestCoin_Animation()
    {
        foreach (Image img in coinInChest)
        {
            img.DOFade(1, fadeDuration);
        }
    }

    IEnumerator DecrementBetAmount()
    {
        "decrement ho rha hai bhai".Show();
        yield return new WaitForSeconds(1f);
        isDecrementing = true;
        while (betAmount > 0)
        {
            //yield return new WaitForSeconds(movementDelays); // Adjust the delay to control the speed
            if (betAmount >= 20 && betAmount <= 100)
            {
                betAmount = Mathf.Max(0, betAmount - 5);
            }

            else
            {
                betAmount = Mathf.Max(0, betAmount - 25);
            }

            yield return new WaitForSeconds(movementDelays); // Adjust the delay to control the speed
            betAmountContainer_RightText.text = betAmount.ToString();
        }
    }

    IEnumerator IncrementBetAmount()
    {

        yield return new WaitForSeconds(1f);
        isIncrementing = true;
        while (incrementedValue < totalAmount)
        {
            if (betAmount >= 20 && betAmount <= 100)
            {
                incrementedValue += 5;
            }
            else
            {
                incrementedValue += 25;
            }
            incrementedValue = Mathf.Min(incrementedValue, totalAmount);
            yield return new WaitForSeconds(movementDelays); // Adjust the delay to control the speed
            betAmountContainer_LeftText.text = incrementedValue.ToString();
        }

    }
    private void OnDisable()
    {
        ApiAndRoomManager.OnCoinsUpdated -= UpdateUserCoins;

        StopCoroutine(DecrementBetAmount());
        isDecrementing = false;
        betAmount = 0;
        StopCoroutine(IncrementBetAmount());
        isIncrementing = false;
        betAmount = 0;
        if (gameObject.name == "CreateChallengeLoadingPanel")
        {


            foreach (Image img in coinInChest)
            {
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0f); // Set alpha back to 0
                img.DOKill();
            }
        }
        if (gameObject.name == "ReversChallengeLoadingPanel")
        {
            reverse_FullCoin.DOKill();
            reverse_FullCoin.color = new Color(reverse_FullCoin.color.r, reverse_FullCoin.color.g, reverse_FullCoin.color.b, 1);
        }
    }
    string imageUrl;
    public void setProfile()
    {

        if (rawProfileImage != null && staticVariables.ProfilePicture != null)
        {
            rawProfileImage.texture = staticVariables.ProfilePicture;
        }
        else
        {
            ConstantsData_M.Log("rawProfileImage or staticVariables.apiSetImage is null.");
        }

    }



    IEnumerator waitingToDisable()
    {
        yield return new WaitForSeconds(5.5f);
        reverseTextPanel.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        reversePanel.SetActive(false);


    }
}

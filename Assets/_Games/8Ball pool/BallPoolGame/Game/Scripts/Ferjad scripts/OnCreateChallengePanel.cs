using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NetworkManagement;
using TMPro;
using DG.Tweening;
public class OnCreateChallengePanel : MonoBehaviour
{
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

    [Tooltip("keep acrive in reverse panel")] public Image reverse_FullCoin;
    public static bool isPanelAnimated = false;

    public GameObject barBg;
    public Sprite totalblnceSprite_s, totalblnceSprite_g;
    public TextMeshProUGUI totalBlncContainerText;
    void OnEnable()
    {
        //print("on crete" + staticVariables.isgoldcoins);

        UpdateUserCoins(ApiAndRoomManager.LastFetchedCoins);

        ApiAndRoomManager.OnCoinsUpdated += UpdateUserCoins;
        SpritesManager.Instance.spritesScriptable.BgHandlerAccordingTo_Id(bg);
        StartCoroutine(PlaySound());
        userModel = staticVariables.UserProfiledata;// JsonUtility.FromJson<UserModel>(userModeljson);
        OnCreateChallenge_Panel();



        if (EightBallPoolNetworkManager.mainPlayer != null && EightBallPoolNetworkManager.mainPlayer.prize != 0)
        {
            betAmount = EightBallPoolNetworkManager.mainPlayer.prize; // update bet amont here

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
        }

        if (betAmount > 0 && !isDecrementing)
        {
            StartCoroutine(DecrementBetAmount());
        }
        if (incrementedValue == 0 && !isIncrementing)
        {
            StartCoroutine(IncrementBetAmount());
        }

        playerName.text = staticVariables.UserProfiledata.user.first_name;
      

    }

    public void OnCreateChallenge_Panel()
    {
        foreach (Image img in coinInChest)
        {
            img.color = new Color(img.color.r, img.color.g, img.color.b, 0f); // Set alpha back to 0           
        }
        Invoke("InSideChestCoin_Animation", 10f);

    }
    void UpdateUserCoins(UserBalanceResponse coins)
    {
        //print("update");
        totalBlncContainerText.text = staticVariables.isgoldcoins ? coins.data.gold_balance.ToString() : coins.data.silver_balance.ToString();
    }



    private void Start()
    {

        closePanelSourcel = closePanelSourcel.GetComponent<AudioSource>();
        closePanelSourcel.clip = panelCloseClip;
        setProfile();
    }
    IEnumerator PlaySound()
    {
        yield return new WaitForSeconds(4f);
       barBg.SetActive(true);
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
        closePanelSourcel.PlayOneShot(panelCloseClip);
        
        DestroyCreatePanel();

    }

    IEnumerator DecrementBetAmount()
    {
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
            betAmountContainer_LeftText.text = betAmount.ToString();

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
            betAmountContainer_RightText.text = incrementedValue.ToString();
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

    public void setProfile()
    {

        if (rawProfileImage != null && staticVariables.ProfilePicture != null)
        {
            rawProfileImage.texture = staticVariables.ProfilePicture;
        }
        else
        {
            ServerConnection.DownloadSprite("/" + staticVariables.UserProfiledata.user.file_url, OnSucess =>
            {
                rawProfileImage.texture = OnSucess;
            }, OnFailed =>
            {
                rawProfileImage.texture = SpritesManager.Instance.spritesScriptable.nullProfileImg;

            });
            ConstantsData_M.Log("rawProfileImage or staticVariables.apiSetImage is null.");
        }

    }
    public void DestroyCreatePanel()
    {
        Destroy(gameObject);
    }


}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NetworkManagement;
using UnityEngine.SceneManagement;
using DG.Tweening;
using UnityEngine.Networking;

public class VersusPanelProcess : MonoBehaviour
{
    [SerializeField] private Text playerTextField, opponentNameText, coinCollectorMain_Text;
    [SerializeField] private int betAmountDouble, initiator = 0;

    [SerializeField] private AudioSource collectCoinsSource;
    [SerializeField] private AudioClip coinCollectionClips;
    public float waitSeconds, randomizerWait, fadeDuration;
    public GameObject leftProfile, rightProfile;
    public GameObject leftRandomizerImg_animator, rightRandomizerImg_animator;
    private bool isRandomizerStop = false;
    public Animator leftCoinAnimator, rightCoinAnimator;
    public Image logo, BG;
    public Image[] CoinBallance;
    public Sprite[] coinImages;
    public RawImage playerRawProfile, opponentRawProfile;



    public Image bcunchCoinInChest;
    UserModel userModel;
    // SET BG ACCORDING TO ID



    private void OnEnable()
    {
        UserModel userModel2 = staticVariables.UserProfiledata;//F
        userModel = staticVariables.UserProfiledata;
        //JsonUtility.FromJson<UserModel>(userModeljson);//F

        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(logo);

        SpritesManager.Instance.spritesScriptable.BgHandlerAccordingTo_Id(BG);
        bcunchCoinInChest.color = new Color(bcunchCoinInChest.color.r, bcunchCoinInChest.color.g, bcunchCoinInChest.color.b, 0);
        if (staticVariables.isgoldcoins)
        {
            CoinBallance[0].sprite = coinImages[0];
            CoinBallance[1].sprite = coinImages[0];
            bcunchCoinInChest.sprite = SpritesManager.Instance.spritesScriptable.coinsInGold;
            if (coinImages.Length > 0)
            {
                if (CoinBallance.Length > 0)
                    CoinBallance[0].sprite = coinImages[0];

                if (CoinBallance.Length > 1)
                    CoinBallance[1].sprite = coinImages[0];
            }

        }
        else
        {
            bcunchCoinInChest.sprite = SpritesManager.Instance.spritesScriptable.coinsInSilver;
            if (coinImages.Length > 1)
            {
                if (CoinBallance.Length > 0)
                    CoinBallance[0].sprite = coinImages[1];

                if (CoinBallance.Length > 1)
                    CoinBallance[1].sprite = coinImages[1];
            }
            CoinBallance[0].sprite = coinImages[1];
            CoinBallance[1].sprite = coinImages[1];

        }
        "ON Enable".Show(gameObject.name);
        StartCoroutine(StopRandomizer());

    }


    void Start()
    {
        GameObject[] ob = GameObject.FindGameObjectsWithTag("notification");
        foreach (var item in ob)
        {
            Destroy(item);
        }
        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(logo);
        if (staticVariables.isgoldcoins)
        {
            bcunchCoinInChest.sprite = SpritesManager.Instance.spritesScriptable.coinsInGold;
        }
        else
        {
            bcunchCoinInChest.sprite = SpritesManager.Instance.spritesScriptable.coinsInSilver;
        }

        playerTextField.text = userModel.user.first_name.ToString() + " " + userModel.user.last_name.ToString();
        //  StartCoroutine(StopRandomizer());
        if (initiator <= betAmountDouble && !isCompleted)
        {
            StartCoroutine(InitiatorIncrementor());
        }

        opponentNameText.text = GameManager.newOpponentName;

        if (GameModeManager.isAI)
        {
            opponentNameText.text = "AI";
            StartCoroutine(waitAndClose());
        }
        if (staticVariables.isfromreferllinks)
        {
            playerRawProfile.texture = staticVariables.ProfilePicture;
            opponentRawProfile.texture = staticVariables.opponentImage;
        }
        else
        {
            setProfile();
        }
    }
    IEnumerator waitAndClose()
    {
        yield return new WaitForSeconds(5);
        this.gameObject.SetActive(false);
    }
    IEnumerator StopRandomizer()
    {
        yield return new WaitForSeconds(randomizerWait);
        leftRandomizerImg_animator.GetComponent<Animator>().enabled = false;
        rightRandomizerImg_animator.GetComponent<Animator>().enabled = false;
        leftRandomizerImg_animator.gameObject.SetActive(false);
        rightRandomizerImg_animator.GetComponent<Animator>().enabled = false;
        isRandomizerStop = true;

        if (isRandomizerStop)
        {
            betAmountDouble = EightBallPoolNetworkManager.mainPlayer.prize;
            collectCoinsSource.clip = coinCollectionClips;
            collectCoinsSource.loop = false;
            collectCoinsSource.Play();

            leftProfile.GetComponent<CoinsCollecto>().enabled = true;
            rightProfile.GetComponent<CoinsCollecto>().enabled = true;

            leftCoinAnimator.enabled = true;
            rightCoinAnimator.enabled = true;
            yield return new WaitForSeconds(1.5f);
            bcunchCoinInChest.DOFade(1, fadeDuration);

        }
        if (GameModeManager.isAI)
        {
            //"Aarha hai yahan".Show();
            HomeMenuManager.instance.PlayAgainstAI();
        }
        else
        {
            gameObject.SetActive(false);
        }

    }
    bool isCompleted = false;
    IEnumerator InitiatorIncrementor()
    {
        isCompleted = true;
        while (initiator < betAmountDouble)
        {
            if (betAmountDouble > 0 && betAmountDouble <= 50)
            {
                initiator += 10;
            }
            else if (betAmountDouble > 50 && betAmountDouble <= 100)
            {
                initiator += 20;
            }
            else if (betAmountDouble > 100 && betAmountDouble <= 200)
            {
                initiator += 35;
            }
            else
            {
                initiator += 50;

            }
            coinCollectorMain_Text.text = initiator.ToString();
            yield return new WaitForSeconds(waitSeconds);
        }

    }
    string imageUrl;
    public void setProfile()
    {
        playerRawProfile.texture = staticVariables.ProfilePicture;
        // FOR AI
        if (GameModeManager.isAI) // for home scene
        {
            opponentRawProfile.texture = SpritesManager.Instance.spritesScriptable.aiImg;
        }
        //FOR MULTIPLAYER
        if (SceneManager.GetActiveScene().name.Contains("AightBallPool"))
        {
            if (GameModeManager.isAI)   // for gameplay
            {
                opponentRawProfile.texture = SpritesManager.Instance.spritesScriptable.aiImg;
            }
            else
            {
                //if (opponentRawProfile != null && staticVariables.opponentImage != null)
                //{
                //    opponentRawProfile.texture = staticVariables.opponentImage;
                //}
                //else
                {
                    ServerConnection.DownloadSprite($"/{NetworkManagement.EightBallPoolNetworkManager.opponentPlayer.imageURL}", DownloadedTexture =>
                    {
                        opponentRawProfile.texture = DownloadedTexture;
                        staticVariables.opponentImage = DownloadedTexture;
                    });


                }
            }
        }
    }
    public void backButton()
    {
        gameObject.SetActive(false);
    }
}

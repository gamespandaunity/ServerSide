using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BallPool;
using BallPool.Mechanics;
using UnityEngine.Networking;
using System.Collections;
public class PlayerUI : MonoBehaviour
{
    [SerializeField] private bool isMainPlayer;
    [SerializeField] private Text coinsText;
    [SerializeField] private Text nameText;
    public RawImage avatarImage;
    [SerializeField] private BallsUIManager ballsUIManager;
    [SerializeField] private Image[] ballsImage;
    private Text[] ballsText;
    private Image[] ballsImageColor;
    public static string opponent_name;
    public Image sliderImage;
    public Sprite sliderSprite;
    private List<Ball> balls;
    UserModel userModel;
    void Awake()
    {
        ballsText = new Text[ballsImage.Length];
        ballsImageColor = new Image[ballsImage.Length];
        for (int i = 0; i < ballsImage.Length; i++)
        {
            ballsText[i] = ballsImage[i].GetComponentInChildren<Text>();
            ballsImageColor[i] = ballsImage[i].transform.Find("Color").GetComponent<Image>();
        }
    }
    private void Start()
    {
        //  setProfile();
    }
    public void SetPlayer(BallPoolPlayer player) //RAR
    {

        /*nameText.text = player.name;
        coinsText.text = player.coins + "";*/
        setProfile();

        //RAR
        if (GameModeManager.isAI)
        {
            if (isMainPlayer)
            {
                //   nameText.text = "You";
                userModel = staticVariables.UserProfiledata;// JsonUtility.FromJson<UserModel>(userModeljson);//F
                //print("player balls name +" + player.balls.Count);


                if (userModel != null)//f
                {
                    nameText.text = userModel.user.first_name.ToString() + " " + userModel.user.last_name.ToString();//f
                }
                coinsText.text = player.coins + "";
            }
            else
            {
                nameText.text = "AI";
                coinsText.text = player.coins + "";

                //GameModeManager.instance.isAI = false;
            }
        }
        else
        {
            if (isMainPlayer)
            {
                userModel = staticVariables.UserProfiledata;// JsonUtility.FromJson<UserModel>(userModeljson);//F
                print("isMainPlayer +" + isMainPlayer);


                if (userModel != null)//f
                {
                    nameText.text = userModel.user.first_name.ToString() + " " + userModel.user.last_name.ToString();//f
                }
                // Show the match bet (Prize) in the bet block, not the player's remaining balance.
                ShowBetCoins(player);
            }
            else
            {
                // Opponent profile can arrive AFTER the game scene loads (slow mobile networks) —
                // OpponetProfile may even still be null right here, which used to throw an NRE and
                // abort this whole method, leaving that side's name/bet unloaded ("profile loading
                // issue" on one player's screen). Bind the name as soon as the data lands instead
                // of reading it exactly once.
                StartCoroutine(BindOpponentName());
                // Show the match bet (Prize) in the bet block, not the opponent's remaining balance.
                ShowBetCoins(player);
            }
            //nameText.text = player.name;
            //coinsText.text = player.coins + "";
            //opponent_name = player.name;
            //print("opponent name in static" + opponent_name);
        }

    }

    // The match bet shown in the bet block is NetworkGameManager.Prize — a [SyncVar] that is still 0
    // for the first frames after the game scene loads. SetPlayer runs at game start and read it
    // immediately, so the bet block printed "0"/blank; only a reconnect (which lands after Prize has
    // synced) happened to show it correctly. Set it now and, if not yet synced, poll briefly and
    // update once the real value arrives — so the bet shows on first load without a reconnect.
    void ShowBetCoins(BallPoolPlayer player)
    {
        if (NetworkGameManager.Instance != null)
        {
            coinsText.text = NetworkGameManager.Instance.Prize + "";
            if (NetworkGameManager.Instance.Prize <= 0)
                StartCoroutine(WaitAndShowBet());
        }
        else
        {
            coinsText.text = player.coins + "";
        }
    }

    IEnumerator WaitAndShowBet()
    {
        float timeout = 10f;
        while (timeout > 0f && (NetworkGameManager.Instance == null || NetworkGameManager.Instance.Prize <= 0))
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.Prize > 0)
            coinsText.text = NetworkGameManager.Instance.Prize + "";
    }



    public void SetActiveBallsIds(BallPoolPlayer player)
    {
        if (player == null)
        {
            return;
        }
        string[] activeBallsIds = player.GetActiveBallsIds();
        if (activeBallsIds == null)
        {
            return;
        }
        /////////////////////////////////


        for (int i = 1; i < activeBallsIds.Length; i++)
        {
            // print("acttive ball string data" + int.Parse(activeBallsIds[i]));
        }



        /////////////////////////////////
        //print("active ball id  length" + player.GetActiveBallsIds());
        for (int i = 0; i < ballsImage.Length; i++)
        {
            if (i < activeBallsIds.Length)
            {
                //  //print("active ball id  length" + activeBallsIds[i]);
                int id = int.Parse(activeBallsIds[i]);
                ballsText[i].text = id + "";
                if (isMainPlayer)
                {
                    ballsImageColor[i].sprite = AightBallPoolPlayer.mainPlayer.isSolids ? ballsUIManager.ballSpriteSolids : (AightBallPoolGameLogic.isBlackBall(id) ? ballsUIManager.ballSpriteSolids : ballsUIManager.ballSpriteStripes);
                }
                else
                {
                    ballsImageColor[i].sprite = AightBallPoolPlayer.otherPlayer.isSolids ? ballsUIManager.ballSpriteSolids : (AightBallPoolGameLogic.isBlackBall(id) ? ballsUIManager.ballSpriteSolids : ballsUIManager.ballSpriteStripes);
                }


                //Color color = ballsUIManager.ballsColors[id - 1];
                //ballsImageColor[i].color = new Color(color.r, color.g, color.b);


                Sprite ballsImg = ballsUIManager.allBallSprites[id - 1]; //RAR
                ballsImageColor[i].sprite = ballsImg; //RAR

                ballsImageColor[i].GetComponent<RectTransform>().localScale = new Vector3(0.8f, 0.8f, 0.8f); //RAR

            }
            else
            {
                ballsText[i].text = "";
                ballsImageColor[i].sprite = ballsUIManager.ballSpriteDefault;
                Color color = ballsUIManager.ballColorDefault;
                ballsImageColor[i].color = new Color(color.r, color.g, color.b);

                ballsImageColor[i].GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f); //RAR
            }
        }
    }


    public void SetActive(bool value)
    {
        avatarImage.gameObject.SetActive(value);
    }
    public void setProfile()
    {
        // Set the slider image
        if (sliderImage != null && sliderSprite != null)
        {
            sliderImage.sprite = sliderSprite;
        }
        else
        {
            ConstantsData_M.Log("SliderImage or SliderSprite is null.");
        }

        if (staticVariables.isfromreferllinks)
        {
            if (isMainPlayer)
            {
                avatarImage.texture = staticVariables.ProfilePicture;


            }
            else
            {
                avatarImage.texture = staticVariables.opponentImage;
            }
        }
        else
        {


            // Check if the game mode is AI
            if (GameModeManager.isAI)
            {
                // Set the avatar image based on whether it's the main player or not
                if (isMainPlayer)
                {
                    if (avatarImage != null)
                    {
                        avatarImage.texture = staticVariables.ProfilePicture;
                    }
                    else
                    {
                        ConstantsData_M.Log("AvatarImage is null.");
                    }
                }
                else
                {
                    if (avatarImage != null && SpritesManager.Instance.spritesScriptable != null && SpritesManager.Instance.spritesScriptable.aiImg != null)
                    {
                        avatarImage.texture = SpritesManager.Instance.spritesScriptable.aiImg;
                    }
                    else
                    {
                        ConstantsData_M.Log("AvatarImage or ScriptableSpriteHolder or AIImg is null.");
                    }
                }
            }
            else // Game mode is not AI
            {
                // Set the avatar image based on whether it's the main player or not
                if (isMainPlayer)
                {
                    if (avatarImage != null)
                    {
                        avatarImage.texture = staticVariables.ProfilePicture;

                        // On slow (mobile) networks ProfilePicture may not be downloaded yet at scene
                        // load, leaving the OWN avatar blank. Load it robustly: wait briefly for the
                        // cached texture, else download by file_url, else show the default avatar.
                        if (avatarImage.texture == null || staticVariables.ProfilePicture == null)
                        {
                            StartCoroutine(LoadMainAvatar());
                        }
                    }
                    else
                    {
                        ConstantsData_M.Log("AvatarImage is null.");
                    }
                }
                else
                {
                    // Opponent avatar sometimes showed BLANK: the imageURL could be empty / not yet set
                    // at scene load, or the download returned null, and (unlike the main-player branch)
                    // there was no fallback. Load it robustly — wait briefly for the URL, reuse the
                    // cached image, and fall back to the default profile sprite on empty-URL / failure.
                    StartCoroutine(LoadOpponentAvatar());
                }
            }
        }
    }

    IEnumerator SaveDelay(Texture2D img)
    {
        yield return new WaitForSeconds(0.5f);
        staticVariables.opponentImage = img;
    }

    // Binds the opponent's NAME as soon as their profile data lands (it can arrive after the game
    // scene loads on slow mobile networks; reading it exactly once left the name blank — or threw,
    // killing the rest of the profile UI on that side).
    IEnumerator BindOpponentName()
    {
        float timeout = 10f;
        while (timeout > 0f && (staticVariables.OpponetProfile == null
                                || string.IsNullOrEmpty(staticVariables.OpponetProfile.userName)))
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        if (nameText != null && staticVariables.OpponetProfile != null
            && !string.IsNullOrEmpty(staticVariables.OpponetProfile.userName))
        {
            nameText.text = staticVariables.OpponetProfile.userName;
            opponent_name = staticVariables.OpponetProfile.userName;
        }
    }

    // Robust OWN-avatar loader (mirror of LoadOpponentAvatar): waits briefly for the cached
    // ProfilePicture, else downloads by the user's file_url, else shows the default avatar.
    IEnumerator LoadMainAvatar()
    {
        float timeout = 5f;
        while (timeout > 0f && staticVariables.ProfilePicture == null)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        if (avatarImage == null) yield break;
        if (staticVariables.ProfilePicture != null)
        {
            avatarImage.texture = staticVariables.ProfilePicture;
            yield break;
        }
        var userData = staticVariables.UserProfiledata != null ? staticVariables.UserProfiledata.user : null;
        if (userData != null && !string.IsNullOrEmpty(userData.file_url))
        {
            ServerConnection.DownloadSprite($"/{userData.file_url}", downloaded =>
            {
                if (downloaded != null && avatarImage != null)
                {
                    avatarImage.texture = downloaded;
                    staticVariables.ProfilePicture = downloaded;
                }
                else
                {
                    SetDefaultAvatar();
                }
            });
        }
        else
        {
            SetDefaultAvatar();
        }
    }

    // Robust opponent-avatar loader. Fixes "opponent picture blank": waits up to a few seconds for the
    // opponent profile / image URL to populate (it can arrive just after the game scene loads), reuses
    // an already-downloaded image, and on an empty URL or a failed/null download shows the default
    // profile sprite instead of leaving the RawImage blank.
    IEnumerator LoadOpponentAvatar()
    {
        float timeout = 5f;
        while (timeout > 0f && (staticVariables.OpponetProfile == null
                                || string.IsNullOrEmpty(staticVariables.OpponetProfile.imageURL)))
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        if (avatarImage == null) yield break;

        if (staticVariables.opponentImage != null)
        {
            avatarImage.texture = staticVariables.opponentImage;
            yield break;
        }

        if (staticVariables.OpponetProfile != null && !string.IsNullOrEmpty(staticVariables.OpponetProfile.imageURL))
        {
            ServerConnection.DownloadSprite($"/{staticVariables.OpponetProfile.imageURL}", downloaded =>
            {
                if (downloaded != null && avatarImage != null)
                {
                    avatarImage.texture = downloaded;
                    staticVariables.opponentImage = downloaded;
                    StartCoroutine(SaveDelay(downloaded));
                }
                else
                {
                    SetDefaultAvatar();
                }
            });
        }
        else
        {
            SetDefaultAvatar();
        }
    }
 void SetDefaultAvatar()
    {
        // NOTE: in THIS (server) project SpritesHOlder.nullProfileImg is a single Texture2D —
        // the CLIENT project has a Texture2D[] and picks a random one. Keep the types straight
        // when syncing this file between the projects.
        var sprites = SpritesManager.Instance != null ? SpritesManager.Instance.spritesScriptable : null;
        if (avatarImage != null && sprites != null && sprites.nullProfileImg != null)
        {
            avatarImage.texture = sprites.nullProfileImg;
        }
    }

    public static Texture2D opponentImg;

}


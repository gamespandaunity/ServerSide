// Start is called once before the first execution of Update after the MonoBehaviour is created
using DG.Tweening;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityExtensions;
public class ResultManager : MonoBehaviour
{
    public static ResultManager instance;


    [FormerlySerializedAs("Winner")] public RawImage winnerAvatar;
    [FormerlySerializedAs("Looser")] public RawImage loserAvatar;

    [FormerlySerializedAs("WinnerName")] public Text winnerNameText;
    [FormerlySerializedAs("LooserName")] public Text loserNameText;
    [FormerlySerializedAs("popupText")] public TextMeshProUGUI resultPopupText;

    [FormerlySerializedAs("FinishPanel")] public GameObject finishPanelUI;



    [FormerlySerializedAs("FriendRequest")] public GameObject friendRequestButton;
    [FormerlySerializedAs("Rematch")] public GameObject rematchButton;
    public GameObject chest;
    public GameObject winnerText, loserText, drawText;
    public Image[] coins;
    public Sprite[] gold, silver;
    public static bool isGameFinished = false;
    public static bool GameSpawnedFinished = false;

    // ── Robust duplicate-result guard (survives the result object's lifecycle) ──
    // GameSpawnedFinished above is reset to false in OnDisable/OnDestroy and the
    // rematch methods. Because ResultManager.Start() immediately loads the Home
    // scene, the result object can tear down and flip GameSpawnedFinished back to
    // false while the match is still "over". A delayed SECOND trigger (typically
    // after a network reconnect) then passes that guard and spawns a DUPLICATE
    // Win/Lose panel for the SAME match. This latch is keyed to the match's
    // server-synced transaction id (stable across reconnect) and is NEVER reset by
    // teardown, so only the FIRST result for a given match can ever open. A new
    // match / rematch carries a new transaction id, so its result still shows.
    public static string lastResultCommittedMatchId = null;

    // Resolve the current networked match's stable id. Prefer the SyncVar on
    // NetworkGameManager (authoritative, survives reconnect); fall back to the
    // cached challenge id. Returns null for AI/offline (no NetworkGameManager),
    // so those flows are never affected by the latch.
    private static string ResolveResultMatchId()
    {
        if (NetworkGameManager.Instance == null) return null;
        if (!string.IsNullOrEmpty(NetworkGameManager.Instance.transactionId))
            return NetworkGameManager.Instance.transactionId;
        if (ApiAndRoomManager._instance != null && !string.IsNullOrEmpty(ApiAndRoomManager._instance.winLoseChallengeId))
            return ApiAndRoomManager._instance.winLoseChallengeId;
        return null;
    }

    /// <summary>
    /// Returns true if the caller may spawn the match result NOW, and records this
    /// match so any later duplicate trigger (e.g. after a reconnect) is blocked.
    /// AI/offline matches (no resolvable match id) fall back to the legacy
    /// GameSpawnedFinished guard (return true) so single-player flow is unchanged.
    /// </summary>
    public static bool TryCommitResultLatch()
    {
        string mid = ResolveResultMatchId();
        if (string.IsNullOrEmpty(mid)) return true;          // AI/offline or no id → don't block
        if (mid == lastResultCommittedMatchId) return false; // result already shown for this match
        lastResultCommittedMatchId = mid;
        return true;
    }

    // ── Keep the local player on the same screen side they had during gameplay ──
    // The WinLoseGameManager prefab hard-positions the WINNER block on the LEFT
    // (x = -450) and the LOSER block on the RIGHT (x = +450). Because the avatars are
    // assigned by OUTCOME (winner texture -> winner block), the local player jumps
    // from one side to the other depending on whether they won, which does not match
    // where they sat in the game scene. The WINNER / LOSER captions live INSIDE their
    // own block, so moving the two blocks keeps every caption attached to the correct
    // player: the winner still reads WINNER, the loser still reads LOSER — only the
    // left/right placement changes so it mirrors gameplay.
    public enum ResultSide
    {
        None,   // feature off — keep the prefab's fixed winner-left / loser-right layout
        Auto,   // detect the local player's side from the still-loaded game scene
        Left,
        Right
    }

    /// <summary>
    /// Per-game override for which side the local player occupied during gameplay.
    /// A game can set this while building its HUD (e.g. ResultManager.LocalPlayerSideOverride
    /// = ResultManager.ResultSide.Right). Left at Auto, the side is detected from the
    /// game scene's avatars at result time.
    /// </summary>
    public static ResultSide LocalPlayerSideOverride = ResultSide.Auto;

    [Tooltip("Used when the local player's gameplay side cannot be detected (scene already unloaded, avatars not on screen, etc.).")]
    [SerializeField] private ResultSide fallbackLocalPlayerSide = ResultSide.Left;

    [Header("Winner highlight boxes (left / right)")]
    [Tooltip("Box shown when the winner block ends up on the LEFT. Auto-found by name ('LeftWinnerBox') if left empty.")]
    [SerializeField] private GameObject leftWinnerBox;
    [Tooltip("Box shown when the winner block ends up on the RIGHT. Auto-found by name ('RightWinnerBox') if left empty.")]
    [SerializeField] private GameObject rightWinnerBox;

    [SerializeField]private Sprite[] gameLogos;

    public Image GameLogo;
    public void Start()
    {
        // Ensure voice channel is left when any game's result screen loads.
        // Covers games that don't call MirrorNetwork.cleanup() on exit (e.g. Poker).
       // VivoxManager.Instance?.LeaveMatchChannel();
        SceneManager.LoadSceneAsync("Home");
    }
    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
    private void OnDisable()
    {
        instance = null;
        GameSpawnedFinished = false;
        isGameFinished = false;
    }
    //  public void WinnerInitialize(Texture2D pp, string name)
    public void InitializeWinnerUI(Texture2D pp, string name)
    {
        winnerAvatar.texture = pp;
        winnerNameText.text = name;
    }
    //  public void LooserInitialize(Texture2D pp, string name)
    public void InitializeLoserUI(Texture2D pp, string name)
    {
        loserAvatar.texture = pp;
        loserNameText.text = name;
    }
    //#region Local-player side placement

    /// <summary>
    /// Single entry point used by every result flow: first mirrors the winner / loser
    /// blocks onto the local player's gameplay side, then lights the winner box that
    /// matches the side the WINNER block actually ended up on.
    /// </summary>
    /// <param name="localPlayerIsWinner">True when the local user's avatar goes into the winner block.</param>
    /// <param name="hasWinner">False on a draw — both winner boxes stay hidden.</param>
    private void ApplyResultLayout(bool localPlayerIsWinner, bool hasWinner = true)
    {
        ApplyLocalPlayerSide(localPlayerIsWinner);
        ApplyWinnerBoxSide(hasWinner);
    }

    /// <summary>
    /// Activates LeftWinnerBox or RightWinnerBox depending on which screen side the
    /// winner block sits on AFTER the placement above has run. Must therefore be
    /// called after ApplyLocalPlayerSide, never before.
    /// </summary>
    private void ApplyWinnerBoxSide(bool hasWinner)
    {
        ResolveWinnerBoxes();
        if (leftWinnerBox == null && rightWinnerBox == null) return;

        if (!hasWinner) // draw — nobody won, so neither box is correct
        {
            if (leftWinnerBox != null) leftWinnerBox.SetActive(false);
            if (rightWinnerBox != null) rightWinnerBox.SetActive(false);
            return;
        }

        bool winnerOnRight = IsWinnerBlockOnRight();
        if (leftWinnerBox != null) leftWinnerBox.SetActive(!winnerOnRight);
        if (rightWinnerBox != null) rightWinnerBox.SetActive(winnerOnRight);

        Debug.Log($"[ResultManager] Winner box side = {(winnerOnRight ? "Right" : "Left")}.");
    }

    /// <summary>
    /// True when the winner block is placed to the RIGHT of the loser block. Falls back
    /// to false (the prefab's default winner-left layout) when the blocks can't be
    /// resolved — in that case nothing was moved either, so left is still correct.
    /// </summary>
    private bool IsWinnerBlockOnRight()
    {
        if (winnerAvatar == null || loserAvatar == null) return false;
        if (!TryResolveResultBlocks(out RectTransform winnerBlock, out RectTransform loserBlock)) return false;

        return BlockOrderX(winnerBlock) > BlockOrderX(loserBlock);
    }
    public void UpdateResultSummary(bool localPlayerIsWinner, bool isDraw = false, bool isAI = false)
    {
        NetworkGameManager network = isAI ? null : NetworkGameManager.Instance;
        bool isGold = network != null ? network.IsGoldCoin : staticVariables.isgoldcoins;
      //  ApplyCoinSprites(isGold);
        long bet = Math.Max(0, network != null ? network.Prize : staticVariables.currentPrize);
        long totalBet = bet * 2;
        int gameId = network != null ? network.currentGameId : ApiAndRoomManager.currentGameId;
      //  ApplyGameBackground(gameId);
        GameButton_InfoSetter game = staticVariables.All_games_Ref.Find(item => item != null && item.game_id == gameId);
        //string commission = game != null ? game.Commission : ApiAndRoomManager.GameComission;
        //long platformFee = CalculatePlatformFee(totalBet, commission, isDraw);
       // resultWinnerPayout = isDraw ? 0 : totalBet - platformFee;

        User user = staticVariables.UserProfiledata?.user;
        string localId = user?._id.ToString();
        long? localPrevious = user == null ? (long?)null : (isGold ? user.gold_balance : user.silver_balance);
        long? opponentPrevious = null;
        if (network != null)
        {
            // Registration captures the profile balances before result settlement.
            foreach (NetworkPlayerData player in new[] { network.creatorData, network.joinerData })
            {
                if (player == null || string.IsNullOrEmpty(player.playerId)) continue;
                long balance = isGold ? player.goldCoins : player.silverCoins;
                if (player.playerId == localId) localPrevious = balance;
                else opponentPrevious = balance;
            }
        }
        if (isAI && staticVariables.isGuest)
        {
            // AiBGSetup already deducted the guest's entry bet.
            localPrevious = (long)GuestDataManager.FetchGuestCoins() + bet;
        }

        bool playerOneIsWinnerSlot = true;
       // if (Player_1_Panel != null && winnerAvatar != null && loserAvatar != null &&
          //  TryResolveResultBlocks(out RectTransform winnerBlock, out RectTransform loserBlock))
        {
         //   playerOneIsWinnerSlot = Player_1_Panel.transform.IsChildOf(winnerBlock);
        }
        // On a draw the existing avatar initialization puts the local user in the winner slot.
        bool localInWinnerSlot = isDraw || localPlayerIsWinner;
        long? winnerPrevious = localInWinnerSlot ? localPrevious : opponentPrevious;
        long? loserPrevious = localInWinnerSlot ? opponentPrevious : localPrevious;

     //   SetPlayerResult(Player_1_Profile, Player_1_Panel, Player_1_CoinBar,
     //       Player_1_Coins_WonsText, Player_1_Previous_BalanceText, Player_1_Updated_balanceText,
    //        Player_1_CoinTypeText, Player_1_ResultLabel, playerOneIsWinnerSlot, isDraw, isGold,
    //        playerOneIsWinnerSlot ? winnerPrevious : loserPrevious, bet);
     //   SetPlayerResult(Player_2_Profile, Player_2_Panel, Player_2_CoinBar,
    //        Player_2_Coins_WonsText, Player_2_Previous_BalanceText, Player_2_Updated_balanceText,
   //         Player_2_CoinTypeText, Player_2_ResultLabel, !playerOneIsWinnerSlot, isDraw, isGold,
    //        playerOneIsWinnerSlot ? loserPrevious : winnerPrevious, bet);

    //    SetResultText(TotalBetAmountText, FormatCoins(totalBet));
    //    SetResultText(WinnerGetAmountText, FormatCoins(resultWinnerPayout));
    //    SetResultText(PlatformFeeText, FormatCoins(platformFee));

        if (resultPopupText != null)
        {
            string coinType = isGold ? "gold" : "silver";
         //   resultPopupText.text = isDraw
         //       ? "It's a draw! Both bets are returned. No platform fee is charged."
         //       : localPlayerIsWinner
        //            ? $"You won {FormatCoins(resultWinnerPayout)} {coinType} coins after a platform fee of {FormatCoins(platformFee)} coins."
       //             : $"You lost {FormatCoins(bet)} {coinType} coins. Please try again.";
        }
    }

    /// <summary>
    /// Picks up the two boxes by name when they haven't been wired in the Inspector,
    /// so the prefab keeps working without a manual hookup. Inactive children are
    /// included, otherwise a box that starts disabled could never be found.
    /// </summary>
    private void ResolveWinnerBoxes()
    {
        if (leftWinnerBox != null && rightWinnerBox != null) return;

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (leftWinnerBox == null && t.name == "LeftWinnerBox") leftWinnerBox = t.gameObject;
            else if (rightWinnerBox == null && t.name == "RightWinnerBox") rightWinnerBox = t.gameObject;

            if (leftWinnerBox != null && rightWinnerBox != null) return;
        }
    }

    /// <summary>
    /// Moves the winner / loser blocks so the LOCAL player ends up on the same screen
    /// side they occupied during gameplay. Captions travel with their block, so the
    /// winner keeps the WINNER caption and the loser keeps the LOSER one.
    /// Must be called while the game scene is still loaded (i.e. before the result
    /// panel's Start() kicks off the Home scene load) for Auto detection to work.
    /// </summary>
    /// <param name="localPlayerIsWinner">True when the local user's avatar goes into the winner block.</param>
    private void ApplyLocalPlayerSide(bool localPlayerIsWinner)
    {
        if (LocalPlayerSideOverride == ResultSide.None) return;
        if (winnerAvatar == null || loserAvatar == null) return;

        if (!TryResolveResultBlocks(out RectTransform winnerBlock, out RectTransform loserBlock))
        {
            Debug.Log("[ResultManager] Could not resolve winner/loser blocks — leaving default placement.");
            return;
        }

        ResultSide desired = LocalPlayerSideOverride == ResultSide.Auto
            ? DetectLocalPlayerSideFromGameplay()
            : LocalPlayerSideOverride;

        if (desired == ResultSide.Auto || desired == ResultSide.None)
            desired = fallbackLocalPlayerSide;
        if (desired != ResultSide.Left && desired != ResultSide.Right) return;

        RectTransform localBlock = localPlayerIsWinner ? winnerBlock : loserBlock;
        RectTransform otherBlock = localPlayerIsWinner ? loserBlock : winnerBlock;

        float localX = BlockOrderX(localBlock);
        float otherX = BlockOrderX(otherBlock);
        if (Mathf.Approximately(localX, otherX)) return; // stacked layout — nothing to mirror

        bool localIsCurrentlyRight = localX > otherX;
        bool localShouldBeRight = desired == ResultSide.Right;
        if (localIsCurrentlyRight == localShouldBeRight) return; // already correct

        SwapPlacement(winnerBlock, loserBlock);
        Debug.Log($"[ResultManager] Local player placed on {desired} (winner={localPlayerIsWinner}) to match gameplay layout.");
    }

    /// <summary>
    /// Walks up from the winner and loser avatars until it finds the two ancestor
    /// transforms that share a parent — those are the movable per-player blocks that
    /// carry the avatar, the name and the WINNER / LOSER caption.
    /// </summary>
    private bool TryResolveResultBlocks(out RectTransform winnerBlock, out RectTransform loserBlock)
    {
        winnerBlock = null;
        loserBlock = null;

        List<Transform> winnerChain = new List<Transform>();
        for (Transform t = winnerAvatar.transform; t != null; t = t.parent)
            winnerChain.Add(t);

        for (Transform loserNode = loserAvatar.transform; loserNode != null; loserNode = loserNode.parent)
        {
            foreach (Transform winnerNode in winnerChain)
            {
                if (winnerNode == loserNode) continue;
                if (winnerNode.parent == null || winnerNode.parent != loserNode.parent) continue;

                winnerBlock = winnerNode as RectTransform;
                loserBlock = loserNode as RectTransform;
                return winnerBlock != null && loserBlock != null;
            }
        }

        return false;
    }

    /// <summary>
    /// Reads the local player's side straight off the still-loaded game scene by
    /// locating the on-screen avatars that use the local and opponent profile
    /// textures. Every game lays its HUD out differently, so this beats hard-coding
    /// a side. Anything inside the result panel itself is ignored.
    /// </summary>
    private ResultSide DetectLocalPlayerSideFromGameplay()
    {
        float? mineX = FindGameplayAvatarScreenX(staticVariables.ProfilePicture);
        float? oppX = FindGameplayAvatarScreenX(staticVariables.opponentImage);

        if (mineX == null || oppX == null) return ResultSide.Auto;
        if (Mathf.Approximately(mineX.Value, oppX.Value)) return ResultSide.Auto;

        return mineX.Value > oppX.Value ? ResultSide.Right : ResultSide.Left;
    }

    /// <summary>
    /// Screen-space X of the largest active UI graphic drawing the given texture.
    /// Largest wins so a full HUD portrait is preferred over a small badge/icon.
    /// </summary>
    private float? FindGameplayAvatarScreenX(Texture texture)
    {
        if (texture == null) return null;

        float? bestX = null;
        float bestArea = 0f;

        foreach (RawImage raw in FindObjectsByType<RawImage>(FindObjectsSortMode.None))
        {
            if (raw.texture != texture) continue;
            ConsiderGameplayAvatar(raw.rectTransform, ref bestX, ref bestArea);
        }

        foreach (Image img in FindObjectsByType<Image>(FindObjectsSortMode.None))
        {
            if (img.sprite == null || img.sprite.texture != texture) continue;
            ConsiderGameplayAvatar(img.rectTransform, ref bestX, ref bestArea);
        }

        return bestX;
    }

    private void ConsiderGameplayAvatar(RectTransform rt, ref float? bestX, ref float bestArea)
    {
        if (rt == null) return;
        if (rt.IsChildOf(transform)) return; // the result panel's own avatars

        Rect rect = rt.rect;
        Vector3 scale = rt.lossyScale;
        float area = Mathf.Abs(rect.width * scale.x) * Mathf.Abs(rect.height * scale.y);
        if (area <= bestArea) return;

        bestArea = area;
        bestX = ScreenX(rt);
    }

    private static float ScreenX(RectTransform rt)
    {
        Canvas canvas = rt.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.WorldToScreenPoint(cam, rt.position).x;
    }

    /// <summary>
    /// Comparable horizontal placement for two siblings. Anchors dominate so blocks
    /// anchored to opposite screen edges still order correctly.
    /// </summary>
    private static float BlockOrderX(RectTransform rt)
    {
        float anchorCentre = (rt.anchorMin.x + rt.anchorMax.x) * 0.5f;
        return anchorCentre * 100000f + rt.anchoredPosition.x;
    }

    /// <summary>
    /// Exchanges only the placement of two sibling blocks — their own size and
    /// contents (avatar, name, caption) stay untouched.
    /// </summary>
    private static void SwapPlacement(RectTransform a, RectTransform b)
    {
        Vector2 anchorMin = a.anchorMin, anchorMax = a.anchorMax;
        Vector2 pivot = a.pivot, anchoredPos = a.anchoredPosition;

        a.anchorMin = b.anchorMin;
        a.anchorMax = b.anchorMax;
        a.pivot = b.pivot;
        a.anchoredPosition = b.anchoredPosition;

        b.anchorMin = anchorMin;
        b.anchorMax = anchorMax;
        b.pivot = pivot;
        b.anchoredPosition = anchoredPos;
    }
    public void Draw()
    {
        winnerText.SetActive(false);
        loserText.SetActive(false);
        drawText.SetActive(true);
        chest.SetActive(false);
        finishPanelUI.SetActive(true);
        resultPopupText.text = "Game ended, No balance deducted";
        InitializeWinnerUI(staticVariables.ProfilePicture,
                            staticVariables.UserProfiledata.user.first_name + " " +
                            staticVariables.UserProfiledata.user.last_name);

        InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
        ApiAndRoomManager._instance.DrawChallenge(ApiAndRoomManager._instance.winLoseChallengeId);
        isGameFinished = true;
    }
    void OnEnable()
    {
        instance = this;

        if (SnokerNetwork.IsMultiplayer)
        {

        }
    }
    public void HandleGameResultAltMultiplayer(bool IsWin, string PlayerId)
    {

        for (int i = 0; i < coins.Length; i++)
        {
            coins[i].sprite = staticVariables.isgoldcoins ? gold[i] : silver[i];

        }
        isGameFinished = true;
        $"{PlayerId} {IsWin}Finish Panel".Show();
        finishPanelUI.SetActive(true);
        MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
        MirrorNetwork.Instance.OpponentDisconnectReason = DisconnectReason.ApplicationQuit;
        MirrorNetwork.Instance.cleanup();
        NetworkGameManager.OnRematchRecived += RematchRequestRecived;

        $"1".Show();

        rematchButton.SetActive(true);
        ApiAndRoomManager._instance.CheckFriendStatus(staticVariables.lastOpponentId, OnSuccess =>
        {
            Single<int> player = JsonUtility.FromJson<Single<int>>(OnSuccess);

            if (player.message.Contains("not"))
            {
                friendRequestButton.SetActive(true);
            }
            else
            {
                friendRequestButton.SetActive(false);
            }
        });
        if (IsWin)
        {
            $"2".Show();
            if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
            {
                InitializeWinnerUI(staticVariables.ProfilePicture,
                    staticVariables.UserProfiledata.user.first_name + " " +
                    staticVariables.UserProfiledata.user.last_name);

                InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                int prizeAmount = NetworkGameManager.Instance != null ?
           NetworkGameManager.Instance.Prize : staticVariables.currentPrize;
                resultPopupText.text = $"Congratulations! Your account has been credited with {prizeAmount} {prizeType} coins";
                ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
                $"3".Show();
            }
            else
            {
                InitializeLoserUI(staticVariables.ProfilePicture,
                    staticVariables.UserProfiledata.user.first_name + " " +
                    staticVariables.UserProfiledata.user.last_name);
                string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                int prizeAmount = NetworkGameManager.Instance != null ?
           NetworkGameManager.Instance.Prize : staticVariables.currentPrize;
                resultPopupText.text = $"{prizeAmount} {prizeType} coins has been deducted from your account. Please try again";
                InitializeWinnerUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                $"4".Show();
            }
        }
        else
        {
            $"5".Show();
            if (PlayerId == staticVariables.UserProfiledata.user._id.ToString())
            {
                $"6".Show();
                InitializeLoserUI(staticVariables.ProfilePicture,
                    staticVariables.UserProfiledata.user.first_name + " " +
                    staticVariables.UserProfiledata.user.last_name);
                string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                int prizeAmount = NetworkGameManager.Instance != null ?
           NetworkGameManager.Instance.Prize : staticVariables.currentPrize;
                resultPopupText.text = $"{prizeAmount} {prizeType} coins has been deducted from your account. Please try again";
                InitializeWinnerUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);

            }
            else
            {
                $"7".Show();
                InitializeWinnerUI(staticVariables.ProfilePicture,
                    staticVariables.UserProfiledata.user.first_name + " " +
                    staticVariables.UserProfiledata.user.last_name);

                string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
                int prizeAmount = NetworkGameManager.Instance != null ?
           NetworkGameManager.Instance.Prize : staticVariables.currentPrize;
                resultPopupText.text = $"Congratulations! Your account has been credited with {prizeAmount} {prizeType} coins";
                InitializeLoserUI(staticVariables.opponentImage, staticVariables.OpponetProfile.userName);
                ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
            }
        }



    }
    public void HandleGameResultAltAI(bool IsWin, string PlayerId)
    {
        $"8".Show();
        rematchButton.SetActive(false);
        friendRequestButton.SetActive(false);
        if (IsWin)
        {

            InitializeWinnerUI(staticVariables.ProfilePicture,
                staticVariables.UserProfiledata.user.first_name);
            ApiAndRoomManager._instance.WinnerLossAIChallenge(staticVariables.UserProfiledata.user._id.ToString());
            GuestDataManager._instance.UpdateGuestCoins(staticVariables.currentPrize * 2);

            string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
            resultPopupText.text = $"Congratulations! Your account has been credited with {staticVariables.currentPrize} {prizeType} coins";
            InitializeLoserUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
            "YOU WIN".Show();
            $"9".Show();

        }
        else
        {

            InitializeLoserUI(staticVariables.ProfilePicture,
                staticVariables.UserProfiledata.user.first_name + " " +
                staticVariables.UserProfiledata.user.last_name);
            string prizeType = staticVariables.isgoldcoins ? "gold" : "silver";
            resultPopupText.text = $"{staticVariables.currentPrize} {prizeType} coins has been deducted from your account. Please try again";
            InitializeWinnerUI(SpritesManager.Instance.spritesScriptable.aiImg, "AI");
            ApiAndRoomManager._instance.WinnerLossAIChallenge("ai");
            $"10".Show();

        }
    }
    public void CheckRematchStatus()
    {
        StickManager.instance.CmdCheckRematchStatus(staticVariables.UserProfiledata.user._id.ToString());
    }
    public void RematchStatus(int playerCount)
    {
        playerCount.Show("player count on rematch status");
        if (playerCount >= 2)
        {
            rematchButton.SetActive(true);
        }
    }
    public void LeaveMultiplayerRoom()
    {
        //if (SnokerNetwork.IsMultiplayer)                                                                                                                                     //Photon Removal
        //{
        //    if (PhotonNetwork.InRoom)
        //    {
        //        PhotonNetwork.CurrentRoom.PlayerTtl = 0;
        //        PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //    }
        //    PhotonNetwork.LeaveRoom();
        //}
        if (SnokerNetwork.IsMultiplayer)
        {
            GameModeManager.isAI = false;

            SnokerGameManager.bGameOver = false;
            Time.timeScale = 1f;
            //switchScreen("MainMenu");
            if (SnokerNetwork.IsMultiplayer)
            {
                //_snokerNetwork._runner.Shutdown(false);
                //Photon Removal PhotonNetwork.LeaveRoom();
                Debug.LogError("photon.leave room");
                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
                if (NetworkGameManager.Instance)
                {
                    NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
                    // this.Delay(1, () => NetworkManager.singleton.StopClient());
                    NetworkGameManager.Instance?.CmdReloadServer();
                }
            }
            else
            {

            }
        }
        GameSpawnedFinished = false;
        isGameFinished = false;
        instance = null;
        Destroy(this.gameObject);
    }
    bool sender;
    public void RequestRematch()
    {
        sender = true;
        NetworkGameManager.Instance.CmdRequestRematch();

        //if (PhotonNetwork.InRoom)                                                                                                                                                   //Photon Removal
        //{
        //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;
        //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
        //    PhotonNetwork.LeaveRoom();
        //}
        // OnPlayerLeftRoom();
        GameSpawnedFinished = false;
        isGameFinished = false;
    }
    public void CreateRematchChallenge()
    {
        // Don't create rematch in AI mode

        staticVariables.screenstatus = "sad";

        Dictionary<string, string> betRequestDict = new Dictionary<string, string>
        {
            ["first_player"] = staticVariables.UserProfiledata.user._id.ToString(),
            ["second_player"] = staticVariables.OpponetProfile.userId,
            ["game_id"] = ApiAndRoomManager.currentGameId.ToString(),
            ["remark"] = "Multipler with Golden Coins",
            ["screenstatus"] = staticVariables.screenstatus,
            ["coins_type"] = staticVariables.isgoldcoins ? "gold" : "silver"
        };


        if (staticVariables.isgoldcoins)
        {
            betRequestDict["silver"] = "0";
            betRequestDict["gold"] = "100";
        }
        else
        {
            betRequestDict["silver"] = "100";
            betRequestDict["gold"] = "0";
        }

        ApiAndRoomManager._instance.create_Challenge_Invite(
            betRequestDict,
            success =>
            {
                if (rematchButton != null)
                    rematchButton.SetActive(false);
                Debug.Log(success);

                CreateBetResponseModel userModeli = JsonUtility.FromJson<CreateBetResponseModel>(success);
                Debug.Log(success);
                string txId = userModeli.data.transaction_id;
                Debug.Log("Deploying Server " + txId);
                // Rematch creator — mark before connecting so server assigns CreatorRef correctly.
                MirrorNetwork.Instance.isMasterClient = true;
                MirrorNetwork.Instance.edgegapAPIClient.Deploy(txId, staticVariables.UserProfiledata.user._id.ToString());

                // Rematch delivery fix (#4): actively NOTIFY the opponent of the incoming rematch.
                // create_Challenge_Invite above only PERSISTS the backend challenge (which then shows
                // as a red entry in the opponent's My Challenges via the socket broadcast) — it does
                // NOT push a live invite, so previously the opponent never "received" the rematch
                // request. This mirrors the canonical notify in AcceptNotificationInstance:
                // _transaction_id + the OPPONENT's _user_id (the player being invited).
                Dictionary<string, string> rematchNotify = new Dictionary<string, string>
                {
                    ["_transaction_id"] = txId,
                    ["_user_id"] = staticVariables.OpponetProfile.userId,
                    ["_server_address"] = ""
                };
                ApiAndRoomManager._instance.notifyToplayChallenge(rematchNotify, _ => { });

                Instantiate(LoadingPanel);
                // When server is ready, join it as the creator
                System.Action<string> onReady = null;
                onReady = (readyTxId) =>
                {
                    if (readyTxId != txId) return;
                    EdgegapAPIClient.OnServerReady -= onReady;
                    MirrorNetwork.Instance.edgegapAPIClient.JoinGame(readyTxId, null);
                };
                EdgegapAPIClient.OnServerReady += onReady;

                AndroidUtility._ShowAndroidToastMessage("Rematch Request Sent");
                Destroy(gameObject);

            },
            fail =>
            {
                ApiAndRoomManager._instance.DisplayError("Failed to send rematch request");
            }
        );
        GameSpawnedFinished = false;
        isGameFinished = false;
    }
    public GameObject LoadingPanel;

    public void RematchRequestRecived()
    {
        if (!sender)
        {
            PopupMessageManager.instance.ReconnectionStatus(true, () =>
            {
                NetworkGameManager.Instance.CmdStartRematch();
            }, () =>
            {
                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
                if (NetworkGameManager.Instance)
                    NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
                this.Delay(1, () => NetworkManager.singleton.StopClient());
                SceneManager.LoadScene("Home");
            });

        }
    }
    void OnDestroy()
    {
        GameSpawnedFinished = false;
        isGameFinished = false;
    }


    public void OnPlayerLeftRoom()
    {
        //Photon Removal  PunNetwork.HandleRoomJoined = null;
        //Photon Removal  PunNetwork.HandleRoomJoined += HandleOpenChallengeCreated;
        //{
        //    NetworkManager.mainPlayer.prize = NetworkManager.mainPlayer.prize;
        //}
        //staticVariables.screenstatus = "sad";
        //Dictionary<string, string> betRequestDict = new Dictionary<string, string>();
        //betRequestDict["first_player"] = staticVariables.UserProfiledata.user._id.ToString();
        //betRequestDict["second_player"] = staticVariables.lastOpponentId.ToString();
        //betRequestDict["game_id"] = ApiAndRoomManager.currentGameId.ToString();
        //betRequestDict["remark"] = "Multipler with Golden Coins";
        //betRequestDict["screenstatus"] = staticVariables.screenstatus;
        //betRequestDict["coins_type"] = staticVariables.isgoldcoins ? "gold" : "silver";

        //if (staticVariables.isgoldcoins)
        //{
        //    betRequestDict["gold"] = NetworkManager.mainPlayer.prize.ToString();
        //    betRequestDict["silver"] = "0";
        //}
        //else
        //{
        //    betRequestDict["silver"] = NetworkManager.mainPlayer.prize.ToString();
        //    betRequestDict["gold"] = "0";
        //}

        //ApiAndRoomManager._instance.create_Challenge_Invite(betRequestDict,
        //successMessage =>
        //{
        //    ConstantsData_M.Log(staticVariables.isgoldcoins + "=>- 1reate success room---");
        //    ApiAndRoomManager._instance.ModifyUserBalance();
        //    ConstantsData_M.Log(staticVariables.isgoldcoins + "=>- 2reate success room---");
        //    ConstantsData_M.Log("Bet ID");
        //    rematchButton.SetActive(false);
        //    AndroidUtility._ShowAndroidToastMessage("Rematch Request Send");
        //},
        //failureMessage =>
        //{

        //    ApiAndRoomManager._instance.DisplayError("failureMessage");

        //    //Debug.Log("Gold Bet Failed to Create: " + failureMessage);

        //});
        SoundManagerMain.instance.ClickSoundPlay();
    }

    //    void HandleOpenChallengeCreated(Photon.Realtime.Room room)                                                                                             //Photon Removal
    //    {
    //        PlayerProfile personProfile = NetworkManager.PlayerFromString(PhotonNetwork.CurrentRoom.Name);
    //        ApiAndRoomManager._instance.challenge_transaction_id = personProfile.roomIds;
    //        ConstantsData_M.Log("roomlistmanager 00000 " + ApiAndRoomManager._instance.challenge_transaction_id + "::" + ApiAndRoomManager._instance.winLoseChallengeId);
    //        // HomeMenuManager.instance.yourChallengeLoader.SetActive(false);
    //        ConstantsData_M.Log("leaveing | Rooom :" + PhotonNetwork.CurrentRoom.Name);
    //#if !UNITY_EDITOR
    //        PhotonNetwork.LeaveRoom();
    //#endif
    //    }
    public void RequestFriendAdd()
    {
        friendRequestButton.SetActive(false);
        ApiAndRoomManager._instance.SendRequestToFriends(staticVariables.OpponetProfile.userId, OnSuccess =>
        {
            Single<int> response = JsonUtility.FromJson<Single<int>>(OnSuccess);
            AndroidUtility._ShowAndroidToastMessage(response.message);

        });
    }
}

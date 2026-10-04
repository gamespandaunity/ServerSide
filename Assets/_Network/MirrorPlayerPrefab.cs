using Mirror;
using Twelve;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityExtensions;

public class MirrorPlayerPrefab : NetworkBehaviour
{
    [SyncVar] public string playerId;
    [SyncVar] public NetworkPlayerData playerData = new();

    public static MirrorPlayerPrefab Instance;

    [Command(requiresAuthority = false)]
    public void CmdSetPlayerData(NetworkPlayerData playerData, NetworkConnectionToClient sender = null)
    {
        Debug.Log("Setting Player Data :" + playerData.playerId);

        // Case B lazy load: JSON was written after OnStartServer() ran (LocalTestManager flow)
        if (string.IsNullOrEmpty(NetworkGameManager.Instance.creatorUserId))
            NetworkGameManager.Instance.LoadLocalConfig();

        string envCreatorId = NetworkGameManager.Instance.creatorUserId;
        bool isCreator;

        if (!string.IsNullOrEmpty(envCreatorId))
        {
            // Edgegap production or manual local JSON: authoritative creator ID
            isCreator = playerData.playerId == envCreatorId;
        }
        else
        {
            // No config at all: fall back to isMasterClient hint + arrival order
            bool wantsCreator    = playerData.isCreator;
            bool creatorSlotFree = NetworkGameManager.Instance.CreatorRef == null;
            bool joinerSlotFree  = NetworkGameManager.Instance.JoinerRef  == null;
            if      (wantsCreator  && creatorSlotFree) isCreator = true;
            else if (!wantsCreator && joinerSlotFree)  isCreator = false;
            else                                       isCreator = creatorSlotFree;
        }

        playerData.isCreator = isCreator;
        AssignPlayerRole(sender, playerData, isCreator);
        TargetConfirmRole(sender, isCreator);
    }

    [Server]
    private void AssignPlayerRole(NetworkConnectionToClient sender, NetworkPlayerData data, bool isCreator)
    {
        string role = isCreator ? "Creator" : "Joiner";

        // Always update the connection ref (reconnect restores the live conn)
        if (isCreator) NetworkGameManager.Instance.CreatorRef = sender;
        else           NetworkGameManager.Instance.JoinerRef  = sender;

        // Reconnect: same player already stored — preserve accumulated game state
        NetworkPlayerData existing = isCreator
            ? NetworkGameManager.Instance.creatorData
            : NetworkGameManager.Instance.joinerData;

        if (existing != null && existing.playerId == data.playerId)
        {
            Debug.Log($"[SERVER] {role} reconnected | conn={sender?.connectionId} | player={data.playerName} — state preserved");
            return;
        }

        // Fresh connection: store initial data
        NetworkPlayerData stored = new()
        {
            playerId    = data.playerId,
            playerName  = data.playerName,
            isCreator   = isCreator,
            goldCoins   = data.goldCoins,
            silverCoins = data.silverCoins
        };

        if (isCreator) NetworkGameManager.Instance.creatorData = stored;
        else           NetworkGameManager.Instance.joinerData  = stored;

        Debug.Log($"[SERVER] {role} assigned | conn={sender?.connectionId} | player={data.playerName}");
    }

    [TargetRpc]
    private void TargetConfirmRole(NetworkConnection conn, bool isCreator)
    {
        MirrorNetwork.Instance.isMasterClient = isCreator;
        Debug.Log($"[CLIENT] Role confirmed by server: {(isCreator ? "Creator" : "Joiner")}");
        if (!isCreator)
            ApiAndRoomManager._instance.winLoseChallengeId = NetworkGameManager.Instance.transactionId;
    }

    // ─── Client: Startup ───────────────────────────────────────────────────────

    public override void OnStartClient()
    {
        base.OnStartClient();

        PopupMessageManager.instance.SetPanelStaus(false, "Reconnect");
        PopupMessageManager.instance.ReconnectionPanelStatus(false);
        SnokerNetwork.IsMultiplayer = true;
        GameConstants.isWithAI     = false;

        string balance = staticVariables.isgoldcoins
            ? ApiAndRoomManager.LastFetchedCoins.data.gold_balance.ToString()
            : ApiAndRoomManager.LastFetchedCoins.data.silver_balance.ToString();
        PlayerPrefs.SetString(LocalSettings.TotalChips, balance);
        PlayerPrefs.Save();

        LocalSettings.SetPlayername(staticVariables.userNickName);
        MirrorNetwork.hasJoinedGame = true;

        if (!isLocalPlayer) return;

        Instance = this;

        var user = staticVariables.UserProfiledata.user;
        string userId = user._id.ToString();

        playerId = userId;
        MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.Connected;
        NetworkGameManager.Instance.CmdSendDisconnectType((int)DisconnectReason.Connected);

        playerData = new NetworkPlayerData
        {
            playerId    = userId,
            playerName  = $"{user.first_name} {user.last_name}",
            goldCoins   = user.gold_balance,
            silverCoins = user.silver_balance,
            isCreator   = MirrorNetwork.Instance.isMasterClient
        };

      //  if (SceneManager.GetActiveScene().name != "Home") return;
      Debug.Log("Setting Player Data :"+ playerData.playerId);
        CmdSetPlayerData(playerData);
        NetworkGameManager.Instance.CmdSetCurrentServerRequestId1();
    }


    private void OnApplicationPause(bool pause)
    {
        if (!isLocalPlayer) return;

        string userId = staticVariables.UserProfiledata.user._id.ToString();

        if (NetworkGameManager.Instance)
            NetworkGameManager.Instance.CmdSetPauseState(pause);

        if (pause)
        {
            MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.UserGoesBackground;
            if (NetworkGameManager.Instance)
                NetworkGameManager.Instance.CmdSendDisconnectType((int)DisconnectReason.UserGoesBackground);
            PlayerPauseEvents.OnPlayerPaused?.Invoke(playerId, userId);
        }
        else
        {
            // Resuming is NOT a disconnect. Calling the reconnect entry point here while the client is still
            // connected starts a phantom pass that takes the one-reconnect-at-a-time guard, so the REAL
            // disconnect arriving a moment later can only be DEFERRED behind it — and its decision then lands
            // a full status round-trip late, which is the window the reason-reset timer used to fire in
            // ("Mirror Client Disconnect - Reason: ApplicationPause" followed by "No reconnection attempt -
            // Reason: Connected", 17-08 report). Only ask when we are actually disconnected; if the drop
            // happened while backgrounded, OnStopClient has already dispatched it.
            if (!NetworkClient.isConnected)
                MirrorNetwork.Instance.ReconnectAfterDisconnect();
            if (NetworkGameManager.Instance)
                NetworkGameManager.Instance.CmdSendDisconnectType((int)DisconnectReason.Connected);
            PlayerPauseEvents.OnPlayerResumed?.Invoke(playerId, userId);
        }

        Debug.Log($"[MirrorPlayer] Local player pause={pause}");
    }
}

public static class PlayerPauseEvents
{
    public static System.Action<string, string> OnPlayerPaused;
    public static System.Action<string, string> OnPlayerResumed;
}

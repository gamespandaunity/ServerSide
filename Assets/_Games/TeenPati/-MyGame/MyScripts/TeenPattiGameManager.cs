using TeenPattiGame;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Mirror;

public class TeenPattiGameManager : NetworkBehaviour
{
    public int maxPlayers = 4;
    public int minPlayers = 2;
    public int turnDuration = 10; // in seconds

    public float startWaitTime = 10;

    public int currentPlayerIndex = 0;
    public List<TeenPattiPlayer> players = new List<TeenPattiPlayer>();
    public float turnEndTime;
    bool isStarted;
    public TMP_Text GameStartWaitText;

    bool MinimumPlayerSatisfied;
    bool GameIsGoingToStart;
    public static TeenPattiGameManager Instance;
    public GameObject ActionPanel;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {

    }

    void CheckingMinimumPlayersRequired()
    {
        if (NetworkGameManager.Instance.currentPlayerCount < 2)
        {
            GameStartWaitText.text = "Waiting For Other Players";
        }
        else
        {
            MinimumPlayerSatisfied = true;
            GameIsGoingToStart = true;
        }
    }


    // [PunRPC]
    private void WaitForOtherPlayersToJoinBeforeStart(float network_time)
    {
        GameStartWaitText.text = "Game Starting In " + network_time.ToString("0") + " seconds";
        if (network_time < 0)
        {
            GameStartWaitText.gameObject.SetActive(false);
            GameIsGoingToStart = false;
            GameStarted();
        }
    }

    void OtherPlayersWait()
    {
        startWaitTime -= Time.deltaTime;
        //  photonView.RPC("WaitForOtherPlayersToJoinBeforeStart", RpcTarget.All, startWaitTime);
        CmdWaitForOtherPlayersToJoinBeforeStart(startWaitTime);
    }

    [Command(requiresAuthority = false)]
    public void CmdWaitForOtherPlayersToJoinBeforeStart(float startWaitTime)
    {
        RpcWaitForOtherPlayersToJoinBeforeStart(startWaitTime);
    }

    [ClientRpc]
    public void RpcWaitForOtherPlayersToJoinBeforeStart(float startWaitTime)
    {
        WaitForOtherPlayersToJoinBeforeStart(startWaitTime);
    }
    void GameStarted()
    {
        players.Clear();

        TeenPattiPlayer[] temp = FindObjectsOfType<TeenPattiPlayer>();


        for (int i = 0; i < temp.Length; i++)
        {
            players.Add(temp[i]);
        }

        if (MirrorNetwork.Instance.isMasterClient)
        {
            for (int i = 0; i < players.Count; i++)
            {
                TeenPattiPlayer player = players[i].GetComponent<TeenPattiPlayer>();
                player.SetPlayerIndex(i);
            }

            isStarted = true;
            StartTurn();
        }
    }

    private void Update()
    {
        if (MirrorNetwork.Instance.isMasterClient && !MinimumPlayerSatisfied)
        {
            CheckingMinimumPlayersRequired();
        }

        if (MirrorNetwork.Instance.isMasterClient && GameIsGoingToStart)
        {
            OtherPlayersWait();
        }

        if (MirrorNetwork.Instance.isMasterClient && Time.time > turnEndTime && isStarted)
        {
            // Time's up, end the current turn
            EndTurn();
        }
    }

    private void StartTurn()
    {
        // Notify all players that a new turn has started
        //  photonView.RPC("RPC_StartTurn", RpcTarget.All, currentPlayerIndex, turnDuration);
        CmdStartTurn(currentPlayerIndex, turnDuration);

        // Set the turn end time
        turnEndTime = Time.time + turnDuration;
    }
    [Command(requiresAuthority = false)]
    public void CmdStartTurn(int currentPlayerIndex, int turnDuration)
    {
        RpcStartTurn(currentPlayerIndex, turnDuration);
    }

    [ClientRpc]
    public void RpcStartTurn(int currentPlayerIndex, int turnDuration)
    {
        RPC_StartTurn(currentPlayerIndex, turnDuration);
    }
    private void EndTurn()
    {
        // Notify all players that the current turn has ended
        //  photonView.RPC("RPC_EndTurn", RpcTarget.All, currentPlayerIndex);
        CmdEndTurn(currentPlayerIndex);

        // Increment the current player index for the next turn
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;

        // Start the next turn
        StartTurn();
    }
    [Command(requiresAuthority = false)]
    public void CmdEndTurn(int currentPlayerIndex)
    {
        RpcEndTurn(currentPlayerIndex);
    }

    [ClientRpc]
    public void RpcEndTurn(int currentPlayerIndex)
    {
        RPC_EndTurn(currentPlayerIndex);
    }
    // [PunRPC]
    private void RPC_StartTurn(int playerIndex, int turnDuration)
    {
        // Highlight the active player and start the turn timer
        if (players != null && playerIndex >= 0 && playerIndex < players.Count)
        {
            players[playerIndex].Highlight();
            players[playerIndex].StartTurn(turnDuration);
        }
        else
        {
            //  Debug.LogError("Invalid player index or players array is null!");
        }

    }

    // [PunRPC]
    private void RPC_EndTurn(int playerIndex)
    {
        // End the turn for the active player
        if (players != null && playerIndex >= 0 && playerIndex < players.Count)
        {
            players[playerIndex].EndTurn();
            players[playerIndex].Unhighlight();
        }
        else
        {
            //  Debug.LogError("Invalid player index or players array is null!");
        }
    }
}

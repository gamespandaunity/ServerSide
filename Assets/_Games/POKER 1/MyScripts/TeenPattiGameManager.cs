//
using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace POKER
{
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
            //if (PhotonNetwork.CurrentRoom.Players.Count < 2)
            //{
            //    GameStartWaitText.text = "Waiting For Other Players";
            //}
            //else
            {
                MinimumPlayerSatisfied = true;
                GameIsGoingToStart = true;
            }
        }


        [ClientRpc]
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

            if (NetworkServer.active)
            {
                // Spawn the players and initialize the game
                for (int i = 0; i < players.Count; i++)
                {
                    TeenPattiPlayer player = players[i].GetComponent<TeenPattiPlayer>();
                    player.SetPlayerIndex(i);
                }

                isStarted = true;
                // Start the first turn
                StartTurn();
            }
        }

        private void Update()
        {
            if (NetworkServer.active && !MinimumPlayerSatisfied)
            {
                CheckingMinimumPlayersRequired();
            }

            if (NetworkServer.active && GameIsGoingToStart)
            {
                OtherPlayersWait();
            }

            if (NetworkServer.active && Time.time > turnEndTime && isStarted)
            {
                // Time's up, end the current turn
                EndTurn();
            }
        }

        private void StartTurn()
        {
            // Notify all players that a new turn has started
            RPC_StartTurn(currentPlayerIndex, turnDuration);

            // Set the turn end time
            turnEndTime = Time.time + turnDuration;
        }

        private void EndTurn()
        {
            // Notify all players that the current turn has ended
            RPC_EndTurn(currentPlayerIndex);

            // Increment the current player index for the next turn
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;

            // Start the next turn
            StartTurn();
        }

        [ClientRpc]
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

        [ClientRpc]
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
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BallPool.Mechanics;
using NetworkManagement;
using Mirror;

namespace BallPool
{
    public delegate void TurnChangedHandler();
    public delegate void PlayerActionHandler(BallPoolPlayer player);
    /// <summary>
    /// The player.
    /// </summary>
    public abstract class BallPoolPlayer
    {
        /// <summary>
        /// The main player.
        /// </summary>
        public static BallPoolPlayer mainPlayer
        {
            get { return (players == null || players.Length < 1) ? null : players[0]; }
        }
        /// <summary>
        /// Gets the player identifier in game, not social id.
        /// </summary>
        /// <value>The player identifier.</value>
        public int playerId
        {
            get;
            private set;
        }
        public string name
        {
            get;
            set;
        }
        public int coins
        {
            get;
            private set;
        }
        public void SetCoins(int coins)
        {
            this.coins = coins;
        }
        public static BallPoolPlayer GetWinner()
        {
            foreach (BallPoolPlayer player in players)
            {
                if (player.isWinner)
                {
                    return player;
                }
            }
            return null;
        }
        public static void SetWinner(int playerId)
        {
            foreach (BallPoolPlayer player in players)
            {
                player.isWinner = player.playerId == playerId;
            }
        }
        /// <summary>
        /// The current player, whose turn to play at the moment.
        /// </summary>
        public static BallPoolPlayer currentPlayer
        {
            get
            {
                foreach (BallPoolPlayer player in players)
                {
                    if (player.playerId == turnId)
                    {
                        return player;
                    }
                }
                //if (NetworkServer.active)
                //{
                //    return null;
                //}
                return null;
            }
        }
        /// <summary>
        /// Updates the player coins when game is completed.
        /// </summary>



        /// <summary>
        /// Maybe Avatar texture
        /// </summary>
        public object avatar
        {
            get;
            protected set;
        }
        public string avatarURL
        {
            get;
            private set;
        }

        public List<Ball> balls
        {
            get;
            protected set;
        }

        public static int playersCount
        {
            get;
            set;
        }

        public static BallPoolPlayer[] players = new BallPoolPlayer[2];

        /// <summary>
        /// if 1: Main Player turn els , if 2: Other Player turn.
        /// </summary>
        public static int turnId
        {
            get;
            set;
        }
        public bool isWinner
        {
            get;
            set;
        }
        public bool isDraw
        {
            get;
            set;
        }
        public bool myTurn
        {
            get;
            private set;
        }


        public static void Deactivate()
        {
            OnPlayerInitialized = null;
            OnTurnChanged = null;
            if (players != null)
            {
                foreach (BallPoolPlayer player in players)
                {
                    player.OnDeactivate();
                    player.isWinner = false;
                    player.myTurn = false;
                    player.balls = null;
                }
            }
        }
        public static bool initialized
        {
            get
            {
                return players != null;
            }
        }
        public static event TurnChangedHandler OnTurnChanged;
        public static event PlayerActionHandler OnPlayerInitialized;

        public enum TurnChangeReason
        {
            ShotEnd = 0,
            Timeout = 1
        }

        /// <summary>
        /// Change the players turn.
        /// </summary>
        public static void ChangeTurn(TurnChangeReason reason = TurnChangeReason.ShotEnd)
        {
            Debug.Log("Changing Turn From User Id: " + turnId);
            if (PlayerPrefs.GetInt("EightballMultiplayer") == 1)
            {
                // Online: turn changes are AUTHORITATIVE on the server. Only the ACTIVE player asks
                // the server to flip the turn; the server then drives EVERY client through
                // RpcNotifyTurnChanged -> ApplyTurn -> SetTurn. The watcher must NOT fall through to
                // the offline local toggle below — OnEndShot runs ChangeTurn() on BOTH clients, and
                // if the watcher's local needToChangeTurn eval ever diverges from the server (e.g. a
                // borderline pot, or the active player potting their own ball so the server KEEPS the
                // turn) the watcher would flip its turn locally with no server correction, and the two
                // devices then stick on different players' turns ("game stuck after a couple shots").
                if (MyEightBallNetwork.Instance != null && turnId == staticVariables.UserProfiledata.user._id)
                    MyEightBallNetwork.Instance.CmdChangeTurn(staticVariables.UserProfiledata.user._id, (int)reason);
                return; // Server handle karega, local change nahi
            }

            Physics.Simulate(Time.fixedDeltaTime); //RAR
                                                   // UnityEngine.Debug.Log("ChangeTurn " + BallPoolPlayer.turnId);
            if (turnId == staticVariables.UserProfiledata.user._id)
            {
                turnId = GameModeManager.isAI ? 1 : int.Parse(EightBallPoolNetworkManager.opponentPlayer.userId);
                Physics.Simulate(Time.fixedDeltaTime); //RAR
                if (GameUIController.instance.CueRenderer2D.GetComponent<MeshRenderer>().material == null)
                {
                    if (GameUIController.instance.CueRenderer2D.GetComponent<MeshRenderer>().material.mainTexture == null)
                    {
                        GameUIController.instance.CueRenderer2D.GetComponent<MeshRenderer>().material.mainTexture = staticVariables.cueMainTex;
                    }
                }
            }
            else
            {
                turnId = staticVariables.UserProfiledata.user._id;
                Physics.Simulate(Time.fixedDeltaTime); //RAR
                if (GameUIController.instance.CueRenderer2D.GetComponent<MeshRenderer>().material == null)
                {
                    if (GameUIController.instance.CueRenderer2D.GetComponent<MeshRenderer>().material.mainTexture == null)
                    {
                        GameUIController.instance.CueRenderer2D.GetComponent<MeshRenderer>().material.mainTexture = staticVariables.cueOpponentTex;
                    }
                }
            }

            for (int i = 0; i < players.Length; i++)
            {

                players[i].myTurn = (players[i].playerId == turnId);
                Physics.Simulate(Time.fixedDeltaTime); //RAR
            }
            if (OnTurnChanged != null)
            {
                OnTurnChanged();
                Physics.Simulate(Time.fixedDeltaTime); //RAR
            }

            Physics.Simulate(Time.fixedDeltaTime); //RAR
        }
        /// <summary>
        /// Set the players turn.
        /// </summary>
        public static void SetTurn(int turnId)
        {
            Debug.Log("Settting turn for :" + turnId);
            BallPoolPlayer.turnId = turnId;
            for (int i = 0; i < players.Length; i++)
            {
                players[i].myTurn = (players[i].playerId == turnId);
                Physics.Simulate(Time.fixedDeltaTime); //RAR
            }
            if (OnTurnChanged != null)
            {
                Physics.Simulate(Time.fixedDeltaTime); //RAR
                OnTurnChanged();
            }
        }
        /// <summary>
        /// Gets the activ balls Ides array.
        /// </summary>
        public string[] GetActiveBallsIds()
        {
            if (balls == null)
            {
                return null;
            }
            string[] data = new string[balls.Count];
            for (int i = 0; i < balls.Count; i++)
            {
                data[i] = balls[i].id + "";
            }
            return data;
        }
        public BallPoolPlayer(int playerId, string name, int coins, object avatar, string avatarURL)
        {
            this.playerId = playerId;
            this.name = name;
            this.coins = coins;
            this.avatar = avatar;
            this.avatarURL = avatarURL;
            if (OnPlayerInitialized != null)
            {
                OnPlayerInitialized(this);
            }
        }

        public abstract void OnDeactivate();

        public virtual void SetActiveBalls(Ball[] balls)
        {
            this.balls = new List<Ball>(0);
            foreach (Ball ball in balls)
            {
                if (!ball.inPocket)
                {
                    this.balls.Add(ball);
                }
            }
        }

    }
}

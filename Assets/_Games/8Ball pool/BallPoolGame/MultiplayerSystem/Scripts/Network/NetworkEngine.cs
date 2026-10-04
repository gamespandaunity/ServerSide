using System.Collections;
#if PHOTON_UNITY_NETWORKING
 
 

#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetworkManagement
{
    public delegate void NetworkHandler(NetworkState state);
    public enum NetworkState
    {
        Disconnected = 0,
        Connected,
        LostConnection,
        CreatedRoom,
        JoinedToRoom,
        LeftRoom,
        RoomCreateFailed,
        JoinRoomFailed,
        OpponentReadToPlay,
        Rejoining

    }

#if PHOTON_UNITY_NETWORKING
    public abstract class NetworkEngine : MonoBehaviourPunCallbacks
#else
public abstract class NetworkEngine : MonoBehaviour
#endif
    {
        public event NetworkHandler OnNetwork;

        public NetworkGameAdapter adapter { get; private set; }

        public int sendRate { get; protected set; }

        public bool opponenWaitingForYourTurn { get; protected set; }
        public abstract void SendRemoteMessage(string message, params object[] args);



        public void SetAdapter(NetworkGameAdapter adapter)
        {
            this.adapter = adapter;
        }

        protected void CallNetworkState(NetworkState state)
        {
            this.state = state;
            if (OnNetwork != null)
            {
                OnNetwork(state);
            }
        }

        public virtual void Initialize()
        {
            opponenWaitingForYourTurn = false;
        }

        protected const float BackgroundTimeout = 60.0f;
        protected virtual void Awake()
        {
            DontDestroyOnLoad(gameObject);
            state = NetworkState.Disconnected;
        }

        //IEnumerator Start()
        //{
        //    Connect();
        //    while (true)
        //    {
        //        yield return new WaitForSeconds(3.0f);
        //        if (SceneManager.GetActiveScene().name=="Home" && (state == NetworkState.Disconnected || state == NetworkState.LostConnection))
        //        {
        //            Debug.Log("Reconneting Hardly");
        //            Connect();
        //        }
        //    }
        //}

        public virtual void Disable()
        {
            OnNetwork = null;
        }

        public NetworkState state
        {
            get;
            private set;
        }

        public abstract void Resset();

        public abstract void Disconnect();

        public abstract void LeftRoom();

        public abstract void Connect();

        public abstract void OnOpponenReadToPlay(string playerData, bool is3DGraphicMode);

        public abstract void OnOpponenStartToPlay(int turnId);

        public abstract void OnSendTime(float time01);

        public void OnMadeTurn()
        {
            opponenWaitingForYourTurn = false;
        }

        public abstract void StartSimulate(string ballsState);

        public abstract void EndSimulate(string ballsState);

        public virtual void OnOpponenWaitingForYourTurn()
        {
            opponenWaitingForYourTurn = true;
        }

        public abstract void OnOpponenInGameScene();
        public abstract void OnOpponentForceGoHome();
    }
}

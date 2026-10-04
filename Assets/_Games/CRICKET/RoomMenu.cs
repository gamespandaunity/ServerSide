using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
using TMPro;
using Mirror;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Runtime.CompilerServices;
using Cricket;
public class RoomMenu : NetworkBehaviour
{
    public static RoomMenu Instance;

    private float secCount;

    //Photon Removal public PhotonView photonView;

    [SerializeField] private Button SelectButton;

    [SerializeField] private Button LeaveButton;

    [SerializeField] private Image timerImage;

    [SerializeField] private Text timerText;

    [SerializeField] public GameObject Timer;

    [SerializeField] private EntryFeesAndRewards entryFeesAndRewards;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        SelectButton.interactable = false;
        // Signal the server that this client's MainMenu scene is fully loaded.
        // CricketNetworkManager.CmdMenuSceneReady counts both players; once both signal,
        // the server broadcasts RpcBothPlayersInMenu which opens the Room panel and starts
        // the team-selection timer in sync — no SyncVar polling, no missed RPCs.
        CricketNetworkManager.instance?.CmdMenuSceneReady();
    }

   
    public void OnOpponentConnected()
    {
        //Photon Removal photonView.RPC("RPC_OnOpponentConnected", RpcTarget.AllBuffered);
    CricketNetworkManager.instance?.CmdOnOpponentConnected();
    }

    //Photon Removal  [PunRPC]
    public void RPC_OnOpponentConnected()
    {
        StartTimer();
    }

    public void StartTimer()
    {
        ////Debug.Log("STARTTTTT HO GYAA");
        if (!Timer.activeInHierarchy)
        {
            CONTROLLER.SELECTTEAM = 0;
            ////Debug.Log("KITNE PLAYERS");
            Timer.SetActive(true);
            SelectButton.interactable = true;
            SetSelectTeamTimer(10f);
        }
    }

    public void SetSelectTeamTimer(float Seconds)
    {
        secCount = Seconds;
        timerImage.fillAmount = Seconds;
        //aiReviewstatus.text = LocalizationData.instance.getText(513);
        DG.Tweening.Sequence s = DOTween.Sequence();
        TweenCallback callback = delegate
        {
            if (!Timer.activeInHierarchy)
            {
                s.Kill();
            }
            SetSecond();
        };
        s.Append(timerImage.DOFillAmount(0f, Seconds));
        for (int i = 0; i < Seconds; i++)
        {
            s.InsertCallback(i, callback);
        }
        //s.InsertCallback(6f, NoBtnClicked);
        s.InsertCallback(Seconds, CallSelectTeam);
    }

    public void SetSecond()
    {
        timerText.text = secCount.ToString();
        secCount--;
    }

    public void CallSelectTeam()
    {
        if(SceneManager.GetActiveScene().name != "MainMenu")
        {
            return;
        }
"Is Connected".Show();
        if (GameConstants.isWithAI == false)
        {
          // photonView.RPC("RPC_SelectTeams", RpcTarget.AllBuffered);
           CricketNetworkManager.instance.CmdSelectTeam();
        }


        if (MirrorNetwork.Instance.isMasterClient)
        {
           if (GameConstants.isWithAI == false)
           {
        //      photonView.RPC("SetOvers", RpcTarget.OthersBuffered, CONTROLLER.oversSelectedIndex);  
        CricketNetworkManager.instance.CmdSetOver(staticVariables.UserProfiledata.user._id,CONTROLLER.oversSelectedIndex);                      //Photon Removal
           }
        }
        SelectButton.interactable = false;
        LeaveButton.interactable = false;
    }

    //Photon Removal  [PunRPC]
    public void RPC_SelectTeams()
    {
        CONTROLLER.SELECTTEAM++;
        ////Debug.Log("HOO GYAA : " + CONTROLLER.SELECTTEAM);

        if (CONTROLLER.SELECTTEAM == 2)
        {
            ////Debug.Log("HOO GYAA 2");
             if (MirrorNetwork.Instance.isMasterClient)
            {
                CONTROLLER.SELECTTEAM = 0;
                CONTROLLER.myTeamIndex = 4;
                CONTROLLER.opponentTeamIndex = 9;

                CONTROLLER.AMIOWNER = true;

                if (Singleton<NavigationBack>.instance != null) Singleton<NavigationBack>.instance.deviceBack = null;
                SquadPageTWO.instance?.hideMe();
                Singleton<GameModeTWO>.instance?.hideMe();
                if (Singleton<EntryFeeConfirmation>.instance != null) Singleton<EntryFeeConfirmation>.instance.holder?.SetActive(false);
                CONTROLLER.pageName = string.Empty;
                SquadPageTWO.instance?.SetSquadPage();
                SavePlayerPrefs.SetTeamList();
            }
            else
            {
                CONTROLLER.SELECTTEAM = 0;
                CONTROLLER.AMIOWNER = false;
                CONTROLLER.myTeamIndex = 9;
                CONTROLLER.opponentTeamIndex = 4;

                if (Singleton<NavigationBack>.instance != null) Singleton<NavigationBack>.instance.deviceBack = null;
                SquadPageTWO.instance?.hideMe();
                Singleton<GameModeTWO>.instance?.hideMe();
                if (Singleton<EntryFeeConfirmation>.instance != null) Singleton<EntryFeeConfirmation>.instance.holder?.SetActive(false);
                CONTROLLER.pageName = string.Empty;
                SquadPageTWO.instance?.SetSquadPage();
                SavePlayerPrefs.SetTeamList();
            }
            MultiplayerPanel.Instance?.CloseMenu("Room");
            Singleton<EntryFeesAndRewards>.instance?.PaymentCompleted();
            return;
        }
    }

    //Photon Removal  [PunRPC]
    private void RPC_ResetSelectTeam()
    {
        CONTROLLER.SELECTTEAM = 0;
    }

    //Photon Removal  [PunRPC]
    public void SetOvers(int idx)
    {
        Singleton<EntryFeesAndRewards>.instance?.SetOverKeys("QPOvers", idx);
        CONTROLLER.oversSelectedIndex = idx;
    }

    public void OnOpponentLeftRoom()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            return;
        }

        CONTROLLER.SELECTTEAM = 0;
        Timer.SetActive(false);
        LeaveButton.interactable = true;
        SelectButton.interactable = false;
    }

}

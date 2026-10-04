using CodeStage.AntiCheat.ObscuredTypes;
 
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Cricket;
using System.Collections;
using System.Collections.Generic;

public class EntryFeesAndRewards : Singleton<EntryFeesAndRewards>
{
    private enum EntryFeesIndexes
    {
        QuickMatch2Overs,
        QuickMatch5Overs,
        QuickMatch10Overs,
        QuickMatch20Overs,
        QuickMatch30Overs,
        QuickMatch50Overs,
        WorldCup50Overs,
        WorldCup30Overs,
        WorldCup20Overs,
        T20Cup20Overs,
        T20Cup10Overs,
        NPL20Overs,
        NPL10Overs
    }

    //Photon Removal public PhotonView photonView;

    public Button SelectButton;

    public GameObject Holder;

    public GameObject RVPanel;

    public Text Title;

    public GameObject[] CoinPackage;

    public GameObject[] coinButtons;

    public Text[] oversText;

    public Text[] coinText;

    private bool sawRV;

    private bool showTotalAmountToBePaid;

    private int RVAmount = 125;

    public int[] FeesPaidList;

    private bool freeMode;

    private bool showRV;

    public GameObject test;

    public GameObject TMHolder;

    private int noOfPackage;


    private int[] quickMatchOvers = new int[2] { 3, 5 };

    private int[] WCOvers = new int[3] { 20, 30, 50 };

    private int[] otherOvers = new int[2] { 10, 20 };

    private void Start()
    {
        StartCoroutine(SelectsOvers());
    }

    IEnumerator SelectsOvers()
    {
        yield return new WaitForSeconds(2);
        SelectOvers(SelectOver.SelectedOver);
    }

    public void TestingMode()
    {
        test.SetActive(value: true);
        Text[] array = coinText;
        foreach (Text text in array)
        {
            text.text = "FREE";
        }
        freeMode = true;
    }

    public void ForTesting(int index)
    {
        if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 8)
        {
            SetOverKeys("QPOvers", index);
        }
      
        PaymentCompleted();
    }

    //Photon Removal  [PunRPC]
    private void RPC_SelectTeam()
    {
        CONTROLLER.SELECTTEAM++;
        ////Debug.Log("HOO GYAA : "+CONTROLLER.SELECTTEAM);

        if (CONTROLLER.SELECTTEAM == 2)
        {
            ////Debug.Log("HOO GYAA 2");

            MultiplayerPanel.Instance.CloseMenu("Room");
            PaymentCompleted();
            return;
        }
    }

    public void SelectTeam()
    {
        //if (PhotonNetwork.IsConnected)
        //{
        //    photonView.RPC("RPC_SelectTeam", RpcTarget.AllBuffered);
        //}

       

        //if (CONTROLLER.AMIOWNER)
        //{
        //    if (PhotonNetwork.IsConnected)
        //    {
        //        photonView.RPC("SetOvers", RpcTarget.OthersBuffered, CONTROLLER.oversSelectedIndex);
        //    }

          
        //}
        SelectButton.interactable = false;
    }

  //Photon Removal  [PunRPC]
    private void SetOvers(int idx)
    {
        SetOverKeys("QPOvers", idx);
        CONTROLLER.oversSelectedIndex = idx;
    }

    public void SelectOvers(int index)
    {
        //index.Show("Over Index");
        if (CONTROLLER.PlayModeSelected == 8)
        {
            SetOverKeys("QPOvers", index);
            CONTROLLER.oversSelectedIndex = index;

        }
        if (CONTROLLER.PlayModeSelected == 0)
        {
            if (freeMode)
            {
                SetOverKeys("QPOvers", index);
                CONTROLLER.oversSelectedIndex = index;
                PaymentCompleted();
                return;
            }

            SetOverKeys("QPOvers", index);
            CONTROLLER.oversSelectedIndex = index;
            PaymentCompleted();
        }

    }

    public void SetOverKeys(string mode, int index)
    {
        ObscuredPrefs.SetInt(mode, index);
    }



    public void Disconnect()
    {
    }


    public void EntryFeeRewardVideoCall(int index)
    {
       
        noOfPackage = index;
    }

    public void RVDeduction()
    {
        sawRV = true;
        showTotalAmountToBePaid = false;
    }


    public void PaymentCompleted()
    {
        freeMode = false;
        test.SetActive(value: false);
        SavePlayerPrefs.SaveUserCoins();
        Singleton<TeamSelectionTWO>.instance.showMe();
        Singleton<TeamSelectionTWO>.instance.SetQuickPlay();
        HideMe();
        GameModeTWO.instance.hideMe();

    }


    public void ShowMe()
    {
        Singleton<NavigationBack>.instance.deviceBack = Back;
        if (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 8)
        {
            RVPanel.SetActive(value: false);
        }
        else
        {
            RVPanel.SetActive(value: false);
        }
        CONTROLLER.pageName = "entryFees";
        Singleton<EntryFeesPanelTransition>.instance.PanelTransition();
        if (CONTROLLER.PlayModeSelected == 7)
        {
            //TMHolder.SetActive(value: true);
        }
        else if (CONTROLLER.PlayModeSelected == 6)
        {
            //SelectOverHolder.SetActive(value: true);
        }
        else
        {
            Holder.SetActive(value: true);
        }
        if (ObscuredPrefs.HasKey("freeEntry") && (CONTROLLER.PlayModeSelected == 0 || CONTROLLER.PlayModeSelected == 8))
        {
            TestingMode();
        }
        else
        {
            test.SetActive(value: false);
        }
    }

    private void HideMe()
    {
        if (CONTROLLER.PlayModeSelected == 7)
        {
            //TMHolder.SetActive(value: false);
        }
        else
        {
            Holder.SetActive(value: false);
        }
    }

    public void Back()
    {
        HideMe();
        Singleton<GameModeTWO>.instance.showMe();
    }
}

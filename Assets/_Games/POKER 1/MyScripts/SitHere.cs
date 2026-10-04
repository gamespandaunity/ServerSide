//
//using Photon.Pun;
using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace POKER
{
    [RequireComponent(typeof(Button))]

    public class SitHere : MonoBehaviour
    {

        private static SitHere _instance;
        public static SitHere Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GameObject.Find("Pos1").transform.GetChild(0).GetComponent<SitHere>();
                return _instance;
            }
        }




        private void Awake()
        {
            if (_instance == null)
                _instance = this;
        }
        public int positionToSit;

        private void OnEnable()
        {
            RegisterListener();
        }

        void RegisterListener()
        {
            Button local_button = GetComponent<Button>();
            local_button.onClick.RemoveAllListeners();
            local_button.onClick.AddListener(SetThisPositionByPlayer);
        }

        public void SetThisPositionByPlayer()
        {
            ConstantsData_M.Log("Click");
            //if (GameManager.Instance.position_availability[positionToSit].)
            if (MatchHandler.isOffline())
            {

                if (LocalSettings.AI_StandUP)
                {

                    ConstantsData_M.Log("1");
                    LocalSettings.Show_Dialogue(UIManager.Instance.dialogueBox, " Wait For AI ");
                    return;
                }
                if (GameManager.Instance.playersList[0].gameObject.activeInHierarchy)
                {
                    return;

                }
            }
            else
            if (NetworkServer.active)
            {
                if (GameManager.Instance.playersList[0].gameObject.activeInHierarchy)
                {
                    return;

                }
            }
            else
            {
                if (GameManager.Instance.playersList[1].gameObject.activeInHierarchy)
                {
                    return;

                }
            }



            Debug.LogError("check total Chips " + LocalSettings.GetTotalChips());


            UIManager uIManager = UIManager.Instance;
            if (MatchHandler.IsPoker() || MatchHandler.isOffline())
            {
                if (LocalSettings.GetTotalChips() < LocalSettings.MinBetAmount)
                {
                    if (!MatchHandler.isOffline())
                    {
                        if (UIManager.Instance.GetMyPlayerInfo().IsMine())
                        {
                            UIManager.Instance.quickShop.SetActive(true);
                            ConstantsData_M.Log(PlayerPrefs.GetString("TotalChips") + "8");
                            UIManager.Instance.GetMyPlayerInfo().StandUp();
                            return;
                        }
                    }
                    else
                    {
                        if (uIManager.GetMyPlayerInfo().gameObject.name != LocalSettings.AI_Name)
                        {
                            ConstantsData_M.Log(PlayerPrefs.GetString("stood") + "10");
                            UIManager.Instance.quickShop.SetActive(true);
                            UIManager.Instance.GetMyPlayerInfo().StandUp();
                            return;
                        }

                    }
                }
                else if (LocalSettings.GetTotalChips() > LocalSettings.MinBetAmount)
                {
                    PokerTableAmount.Instance.PlayerTotalAmountTxt.text =/*"Gold: " +*/ LocalSettings.Rs(LocalSettings.GetTotalChips());
                    PokerManager.Instance.sitPosAfterReset = positionToSit;
                    //  PokerManager.Instance.BuyInCashPanel.SetActive(true);
                    PokerTableAmount.Instance.OnJoinNowBtnClick();

                    return;
                }
            }



            PositionsManager.Instance.SitHere(positionToSit);
            Invoke(nameof(RefreshAfterSomeTime), 0.5f);
            GameManager.Instance.SitHereBtnStatus(false);
            PlayerStateManager.Instance.Amountobject.SetActive(true);
            PlayerStateManager.Instance.taptoSitHere.SetActive(false);
            PlayerStateManager.Instance.waitForNextRound.SetActive(false);
        }


        public void SetThisForAIPositionByPlayer()
        {
            StartCoroutine(SetWaitForAIPositionByPlayer());

        }


        IEnumerator SetWaitForAIPositionByPlayer()
        {
            // Debug.LogError("Here we Go " + AI_coroutine);
            //if (GameManager.Instance.position_availability[positionToSit].)
            yield return new WaitForSeconds(Random.Range(3f, 11f));


            if (LocalSettings.AI_Amount < LocalSettings.MinBetAmount)
            {

                LocalSettings.AI_Amount = Random.Range(125000, 160000);//(50000, 100000);
                PlayerPrefs.SetString("CashInHandAI", LocalSettings.AI_Amount.ToString());



            }


            UIManager uIManager = UIManager.Instance;


            PositionsManager.Instance.AISitHere(positionToSit);
            Invoke(nameof(RefreshAfterSomeTime), 0.5f);
            yield return new WaitForSeconds(1f);
            LocalSettings.AI_StandUP = false;
            GameManager.Instance.SitHereBtnStatus(false);
        }

        void RefreshAfterSomeTime()
        {
            PositionsManager.Instance.AssignMyLocalPositionWithAllOtherClients();
        }

    }
}
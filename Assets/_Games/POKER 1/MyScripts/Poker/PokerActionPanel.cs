//using Photon.Pun;
//using Photon.Realtime;
using POKER;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace POKER
{
    public class PokerActionPanel : MonoBehaviour
    {
        #region
        public Slider AIBetAmoutSlider;
        public BigInteger AIMaxBetAmount;
        public GameObject AIActionPanelPoker;
        public GameObject AIBetRaiseSliderBtn;
        public GameObject AIBetRaiseLabelObj;
        public GameObject AIBetRaiseSlider;
        public GameObject AICallAllInCheckBtnObj;
        public GameObject AIcallBetPockerBtn;
        public GameObject AIcheckPokerBtn;
        [ShowOnly] public BigInteger AiMinBetAmount;
        [ShowOnly] public BigInteger AiMaxBetAmount;
        BigInteger currentAiSliderAmount;
        public BigInteger CurrentAITargetBetAmount;

        #endregion

        //public int NumberOfSteps = 20;
        public Slider BetAmountSlider;

        [ShowOnly] public BigInteger MinBetAmount;
        [ShowOnly] public BigInteger MaxBetAmount;
        BigInteger OffSet;

        public GameObject ActionPanelPoker;
        public GameObject BetRaiseSliderBtn;
        public GameObject BetRaiseLabelObj;
        public GameObject BetRaiseSlider;
        public GameObject CallAllInCheckBtnObj;
        public GameObject callBetPockerBtn;
        public GameObject checkPokerBtn;
        public Button pokerPlusBtn;
        public Button pokerMinusBtn;

        public TMP_Text CallCheckAllInTxt;
        public TMP_Text SliderRaiseAmountTxt;
        BigInteger currentSliderAmount;
        public PlayerInfo MyLocalPlayerInfo;

        public BigInteger CurrentTargetBetAmount;
        [ShowOnly] public bool isAllIn;
        public RectTransform helpPokerTransform;

        #region Creating Instance
        private static PokerActionPanel _instance;
        public static PokerActionPanel Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GameObject.FindObjectOfType<PokerActionPanel>();
                return _instance;
            }
        }
        private void Awake()
        {

            if (_instance == null)
                _instance = this;
        }
        #endregion

        private void Start()
        {

            StartForSelectAmount();

        }
        public void StartForSelectAmount()
        {
            if (ActionPanelPoker.activeInHierarchy)
                ActionPanelPoker.SetActive(false);
            MaxBetAmount = LocalSettings.GetPokerBuyInChips();
            // MinBetAmount = LocalSettings.GetBlindAmountPoker();
            AIMaxBetAmount = LocalSettings.AI_Amount;
            AiMinBetAmount = MinBetAmount = LocalSettings.MinBetAmount;
            OffSet = MinBetAmount;
            SliderRaiseAmountTxt.text = MinBetAmount.ToString();
            BetRaiseLabelObj.SetActive(true);
            BetRaiseSlider.SetActive(false);
            BetRaiseSliderBtn.SetActive(true);
            onSliderValueChange();
            CallCheckAllInTxt.text = "Call\n" + LocalSettings.Rs(MinBetAmount);
        }

        public void onSliderValueChange()
        {
            //double CurrentSliderVal = BetAmountSlider.value / NumberOfSteps;
            //MaxBetAmount = LocalSettings.GetPokerBuyInChips();
            //BigInteger finalSliderVal = MaxBetAmount - OffSet;

            //currentSliderAmount = ((BigInteger)(CurrentSliderVal * (double)finalSliderVal)) + OffSet;


            int increment = (Mathf.RoundToInt(BetAmountSlider.value));
            if (MinBetAmount == LocalSettings.MinBetAmount / 2)
            {

                currentSliderAmount = (increment * LocalSettings.MinBetAmount) + MinBetAmount;
                if (increment == BetAmountSlider.maxValue)
                {
                    SliderRaiseAmountTxt.text = "ALL IN";//\n" + LocalSettings.Rs((currentSliderAmount));
                    isAllIn = true;
                    //  Debug.LogError("Check All In Bet...1" + isAllIn);
                    currentSliderAmount = LocalSettings.GetPokerBuyInChips();
                }
                else
                {
                    isAllIn = false;
                    SliderRaiseAmountTxt.text = LocalSettings.Rs((currentSliderAmount));
                }
            }
            else
            {
                // Debug.LogError("slider value....." + OffSet);
                if (checkPokerBtn.gameObject.activeInHierarchy)
                {
                    currentSliderAmount = (increment * LocalSettings.MinBetAmount) + LocalSettings.MinBetAmount;
                }
                else
                {
                    currentSliderAmount = (increment * LocalSettings.MinBetAmount) + MinBetAmount;
                    // Debug.LogError("check Current AMount..." + currentSliderAmount + "....Slider value..." + increment + "......Minimum Bet....." + LocalSettings.MinBetAmount + "Check Poker Min Bet" + MinBetAmount);
                }
                if (increment == BetAmountSlider.maxValue)
                {
                    //SliderRaiseAmountTxt.text = "ALL IN\n" + LocalSettings.Rs((currentSliderAmount));
                    SliderRaiseAmountTxt.text = "ALL IN";//\n" + LocalSettings.Rs((LocalSettings.GetPokerBuyInChips()));
                    isAllIn = true;

                    currentSliderAmount = LocalSettings.GetPokerBuyInChips();
                }
                else
                {
                    isAllIn = false;
                    SliderRaiseAmountTxt.text = LocalSettings.Rs((currentSliderAmount));
                }
            }
            //if (LocalSettings.MinBetAmount >= LocalSettings.GetPokerBuyInChips())
            //{
            //    currentSliderAmount = LocalSettings.GetPokerBuyInChips();
            //    SliderRaiseAmountTxt.text = "ALL IN";
            //    isAllIn = true;
            //    BetAmountSlider.interactable = false;
            //    pokerMinusBtn.interactable = false;
            //    pokerPlusBtn.interactable = false;
            //}
        }
        public void SetSliderAmount(string sign)
        {
            //BetAmountSlider.value = sign == "+" ? BetAmountSlider.value + 0.05f : BetAmountSlider.value - 0.05f;
            BetAmountSlider.value = sign == "+" ? BetAmountSlider.value + 1 : BetAmountSlider.value - 1;
            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ButtonSound, false);
        }



        public void SetBetAmountOnDealer(bool isHalf, PlayerInfo pInfo)
        {
            // Debug.LogError(pInfo.name);
            //BigInteger BootAmount = LocalSettings.GetBlindAmountPoker();
            //if (isHalf)
            //{
            //    BootAmount = LocalSettings.GetBlindAmountPoker() / 2;
            //}
            BigInteger BootAmount = LocalSettings.MinBetAmount;
            if (isHalf)
            {
                BootAmount = LocalSettings.MinBetAmount / 2;
            }
            int viewID = 0;
            if (!MatchHandler.isOffline())
                viewID = int.Parse(pInfo.ownerPlayerId);
            else
                viewID = (int)pInfo.View_ID_Offline;
            if (!MatchHandler.isOffline())
            {
                if (pInfo.IsMine())
                {
                    LocalSettings.SetPokerBuyInChips(-BootAmount);
                    pInfo.playerCustomProperties.SetCustomBigIntegerData(LocalSettings.PlayerPokerTableCashKey, LocalSettings.GetPokerBuyInChips());
                    UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetPokerBuyInChips());
                    //Debug.LogError("Check Total player Amount..." + LocalSettings.GetPokerBuyInChips() + "...Check Bet Cash from Player Payment....." + BootAmount);
                }
                else
                {
                    //Debug.LogError("Check Total player Amount..." + LocalSettings.GetPokerBuyInChips() + "...Check Bet Cash from Player Payment....." + BootAmount);
                    pInfo.BetStartAmountBet(BootAmount, viewID.ToString());
                    //LocalSettings.SetPokerBuyInChips(-BootAmount);


                }
            }
            else
            {
                if (pInfo.gameObject.name != LocalSettings.AI_Name)
                {
                    Debug.Log(BootAmount + " 1");
                    LocalSettings.SetPokerBuyInChips(-BootAmount);
                    pInfo.PokerTotalCashTxt.text = LocalSettings.Rs(LocalSettings.GetTotalChips());
                    UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetTotalChips());
                }
                else
                {
                    Debug.Log(BootAmount + " 0");
                    pInfo.BetStartAmountBet(BootAmount, viewID.ToString());
                }

            }
            pInfo.SendPokerBet(BootAmount, false);

        }


        public void OnRaiseSliderBtnClick()
        {
            if (BetRaiseSliderBtn.transform.GetChild(0).gameObject.activeInHierarchy)
            {
                BetRaiseLabelObj.SetActive(false);
                BetRaiseSlider.SetActive(true);
                AIBetAmoutSlider.value = 0;
                onSliderValueChange();
            }
            else
            {
                if (LocalSettings.GetPokerBuyInChips() >= currentSliderAmount)
                {
                    LocalSettings.SetPokerBuyInChips(-currentSliderAmount);
                    PokerManager.Instance.SetStartingAmount();
                    bool isAllInBet = LocalSettings.GetPokerBuyInChips() > 0 ? false : true;
                    isAllIn = isAllInBet;
                    // Debug.LogError("Check All In Bet...1" + isAllIn);
                    GetMyPlayerInfo().SendPokerBet(currentSliderAmount, isAllInBet);
                    BetRaiseLabelObj.SetActive(true);
                    BetRaiseSlider.SetActive(false);
                }
                MaxBetAmount = LocalSettings.GetPokerBuyInChips();
                onSliderValueChange();
            }


            UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetPokerBuyInChips());

            if (MinBetAmount > MaxBetAmount)
            {
                BetRaiseSliderBtn.SetActive(false);
                BetRaiseSlider.SetActive(false);
            }
            else
            {
                BetRaiseSliderBtn.SetActive(true);
                BetRaiseSlider.SetActive(false);
            }
        }



        void OnFolding()
        {
            UIManager.Instance.GetMyPlayerInfo().UpdatePlayerState(PlayerState.STATE.Packed);
        }

        [HideInInspector]
        public bool isSliderBtnClick;
        public void CallButtonClick(bool isSliderBtn)
        {
            UIManager uIManager = UIManager.Instance;
            isSliderBtnClick = isSliderBtn;
            if (isSliderBtnClick)
            {

                if (BetRaiseSliderBtn.transform.GetChild(0).gameObject.activeInHierarchy)
                {
                    BetRaiseLabelObj.SetActive(false);
                    BetRaiseSlider.SetActive(true);
                    BetAmountSlider.minValue = 1;
                    SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ButtonSound, false);
                    if (checkPokerBtn.gameObject.activeInHierarchy || MinBetAmount == LocalSettings.MinBetAmount / 2)
                    {
                        BetAmountSlider.maxValue = (int)(LocalSettings.GetPokerBuyInChips() / LocalSettings.MinBetAmount); // Set this to the maximum number of increments based on total amount

                    }
                    else
                    {

                        BigInteger remainingAmountPokerBuyIN = LocalSettings.GetPokerBuyInChips() - MinBetAmount;
                        BetAmountSlider.maxValue = (int)(remainingAmountPokerBuyIN / LocalSettings.MinBetAmount);
                    }
                    BetAmountSlider.value = 1; // Default starting value

                    if (MinBetAmount == LocalSettings.MinBetAmount / 2)
                    {

                        currentSliderAmount = (BigInteger)(Mathf.RoundToInt(BetAmountSlider.value)) * LocalSettings.MinBetAmount + MinBetAmount;
                    }
                    else
                    {
                        if (LocalSettings.MinBetAmount >= LocalSettings.GetPokerBuyInChips())
                        {
                            currentSliderAmount = LocalSettings.GetPokerBuyInChips();
                            SliderRaiseAmountTxt.text = "ALL IN";
                            isAllIn = true;
                            BetAmountSlider.interactable = false;
                            pokerMinusBtn.interactable = false;
                            pokerPlusBtn.interactable = false;
                        }
                        else
                        {
                            if (checkPokerBtn.gameObject.activeInHierarchy)
                            {
                                currentSliderAmount = ((BigInteger)(Mathf.RoundToInt(BetAmountSlider.value) * LocalSettings.MinBetAmount)) + LocalSettings.MinBetAmount;
                            }
                            else
                            {
                                currentSliderAmount = ((BigInteger)(Mathf.RoundToInt(BetAmountSlider.value) * LocalSettings.MinBetAmount)) + MinBetAmount;
                            }
                        }
                    }
                    if (LocalSettings.MinBetAmount < LocalSettings.GetPokerBuyInChips())
                        SliderRaiseAmountTxt.text = LocalSettings.Rs((currentSliderAmount));
                    return;
                }
                if (!CheckBoolAllIN())
                    uIManager.GetMyPlayerInfo().ResetIsBetPlacedPocker();
            }

            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipAdding, false);
            uIManager.GetMyPlayerInfo().myPockerTurnComplete(true);
            uIManager.GetMyPlayerInfo().UpdatePlayerState(PlayerState.STATE.BetPlaced);
        }


        public void OnClickCheckBtn()
        {
            ActionPanelPoker.SetActive(false);
            UIManager uIManager = UIManager.Instance;
            if (!checkPockeBetPlaced())
            {
                uIManager.GetMyPlayerInfo().myPockerTurnComplete(true);
                // Debug.LogError("Value Of IsBetPlacedPocker......" + uIManager.GetMyPlayerInfo().isBetPlacedPocker);
            }
            if (!MatchHandler.isOffline())
                uIManager.GetMyPlayerCurrentState().UpdateCurrentPlayerState(PlayerState.STATE.WaitingForTurn);
            else
                uIManager.GetMyPlayerInfo().currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.WaitingForTurn);
            GiveTurnToNextPlayerPocker();
            if (checkPockeBetPlaced())
            {
                //  Debug.LogError("changing state to AB First turn");
                RoomStateManager.Instance.UpdateCurrentStateOnShowBtn(RoomState.STATE.ABFirstTurn);
            }
            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ButtonSound, false);
        }

        public bool CheckBoolAllIN()
        {
            foreach (var item in PlayerStateManager.Instance.PlayingList)
            {
                if (item.isAllInCashBet)
                    return true;
            }
            return false;
        }
        public bool checkPockeBetPlaced()
        {
            foreach (PlayerInfo item in PlayerStateManager.Instance.PlayingList)
            {
                if (item == null) continue;   // destroyed while a player was disconnecting
                if (item.isBetPlacedPocker == false)
                    return false;
            }
            return true;
        }
        public void GiveTurnToNextPlayerPocker()
        {
            int myIndex = PlayerStateManager.Instance.SideShowNext();
            PlayerStateManager.Instance.PlayingList[myIndex].currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.ExecutingTurn);
        }

        public void OnCallCheckAllInBtnClick()
        {
            if (LocalSettings.GetPokerBuyInChips() >= MinBetAmount)
            {
                LocalSettings.SetPokerBuyInChips(-MinBetAmount);
                PokerManager.Instance.SetStartingAmount();
                bool isAllInBet = LocalSettings.GetPokerBuyInChips() > 0 ? false : true;
                isAllIn = isAllInBet;
                //  Debug.LogError("Check All In Bet...1" + isAllIn);
                GetMyPlayerInfo().SendPokerBet(MinBetAmount, isAllInBet);
                MaxBetAmount = LocalSettings.GetPokerBuyInChips();
                BetAmountSlider.value = 0;
                onSliderValueChange();
            }

            UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetPokerBuyInChips());

            if (MinBetAmount > MaxBetAmount)
            {
                BetRaiseSliderBtn.SetActive(false);
                BetRaiseSlider.SetActive(false);
            }
            else
            {
                BetRaiseSliderBtn.SetActive(true);
                BetRaiseSlider.SetActive(false);
                BetRaiseLabelObj.SetActive(true);
            }
        }

        public void OnCallAICheckAllInBtnClick()
        {

            float outCome = Random.value;
            Debug.Log("Float Value Of OutCOm " + outCome);
            if (outCome < 0.5f)
            {
                if (UIManager.Instance.GetAIPlayerInfo().pokerTotalBetCash == UIManager.Instance.GetMyPlayerInfo().pokerTotalBetCash)
                {
                    AICheckBtn();
                    return;

                }
            }


            Debug.Log(MinBetAmount);


            BigInteger BetOfAI = UIManager.Instance.GetMyPlayerInfo().pokerTotalBetCash - UIManager.Instance.GetAIPlayerInfo().pokerTotalBetCash;

            if (LocalSettings.AI_Amount >= BetOfAI && !UIManager.Instance.GetMyPlayerInfo().CashAllInIndicator.activeInHierarchy)
            {
                // raised or full bet

                float outCome1 = Random.value;
                Debug.Log("Float Value Of OutCOm " + outCome1);
                if (outCome1 > 0.9f)
                {
                    BetOfAI = LocalSettings.AI_Amount;
                    if (!CheckBoolAllIN())
                        UIManager.Instance.GetAIPlayerInfo().ResetIsBetPlacedPocker();
                    UIManager.Instance.GetAIPlayerInfo().isBetPlacedPocker = true;
                }
                else if (outCome1 < 0.5f)
                {
                    AIBetAmoutSlider.minValue = 1;
                    BigInteger remainingAmountPokerBuyIN = LocalSettings.AI_Amount - BetOfAI;
                    AIBetAmoutSlider.maxValue = (int)(remainingAmountPokerBuyIN / LocalSettings.MinBetAmount);
                    int maxvalue = Mathf.RoundToInt(AIBetAmoutSlider.maxValue) + 1;
                    int minValue = Mathf.RoundToInt(AIBetAmoutSlider.minValue);
                    int betAmountAI = Random.Range(minValue, maxvalue);

                    if (betAmountAI == maxvalue)
                    {
                        BetOfAI = LocalSettings.AI_Amount;

                    }
                    else
                    {
                        BetOfAI = (BigInteger)(betAmountAI) * LocalSettings.MinBetAmount + BetOfAI;
                    }
                    if (!CheckBoolAllIN())
                        UIManager.Instance.GetAIPlayerInfo().ResetIsBetPlacedPocker();
                }



                //end Raised or full bet
                LocalSettings.AI_Amount -= BetOfAI;
                Debug.Log("Her We Go....");
                bool isAllInBet = LocalSettings.AI_Amount > 0 ? false : true;

                //  Debug.LogError("Check All In Bet...1" + isAllIn);
                UIManager.Instance.GetAIPlayerInfo().SendPokerBet(BetOfAI, isAllInBet);
                //  MaxBetAmount = LocalSettings.AI_Amount;
                // BetAmountSlider.value = 0;
                //  onSliderValueChange();
            }
            else
            {


                BetOfAI = LocalSettings.AI_Amount;
                LocalSettings.AI_Amount -= BetOfAI;
                //if(BetOfAI < UIManager.Instance.GetMyPlayerInfo().pokerTotalBetCash)
                //{
                //    BigInteger RemaingOfPlayer = UIManager.Instance.GetMyPlayerInfo().pokerTotalBetCash - BetOfAI
                //}
                bool isAllInBet = LocalSettings.AI_Amount > 0 ? false : true;
                UIManager.Instance.GetAIPlayerInfo().SendPokerBet(BetOfAI, isAllInBet);
            }


            UIManager.Instance.GetAIPlayerInfo().currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.WaitingForTurn);



        }


        public void SetMinBetAmount()
        {
            //BigInteger BetDifference = CurrentTargetBetAmount - GetMyPlayerInfo().PokerTotalWholeBetAmount;

            BigInteger BetDifference = CurrentTargetBetAmount - GetMyPlayerInfo().pokerTotalBetCash;
            OffSet = BetDifference;
            //Debug.LogError("Bet Diff: " + BetDifference + "     CurrentTargetBetAmount: " + CurrentTargetBetAmount + "       Totalcash: " + GetMyPlayerInfo().pokerTotalBetCash);
            if (BetDifference == 0 && !CheckBoolAllIN())
            {
                checkPokerBtn.SetActive(true);
                callBetPockerBtn.SetActive(false);
                if (LocalSettings.GetPokerBuyInChips() > 0)
                    BetRaiseSliderBtn.SetActive(true);
                else
                    BetRaiseSliderBtn.SetActive(false);


                MinBetAmount = 0;
            }
            else if (BetDifference < LocalSettings.GetPokerBuyInChips())
            {
                MinBetAmount = BetDifference;
                CallCheckAllInTxt.text = "Call\n" + LocalSettings.Rs(MinBetAmount);
            }
            else
            {
                if (LocalSettings.GetPokerBuyInChips() != 0)
                {
                    CallCheckAllInTxt.text = "ALL IN";/*\n" + LocalSettings.Rs(LocalSettings.GetPokerBuyInChips());*/
                    //Debug.LogError("Check All In Bet...1" + isAllIn);
                    MinBetAmount = LocalSettings.GetPokerBuyInChips();
                    isAllIn = true;
                }
                else
                {
                    CallCheckAllInTxt.text = "CHECK";
                }
                BetRaiseSliderBtn.SetActive(false);
                BetRaiseSlider.SetActive(false);
            }
            //  Debug.LogError("Min bet:  " + MinBetAmount + "     Max Bet Amount: " + MaxBetAmount + "    Is all in: " + isAllIn + "      Bet difference: " + BetDifference + "     CurrentTargetBetAmount: " + CurrentTargetBetAmount + "       Totalcash: " + GetMyPlayerInfo().pokerTotalBetCash);
            if (isAllIn)
            {
                checkPokerBtn.SetActive(true);
                callBetPockerBtn.SetActive(false);

            }
            else
            {

            }
            BetRaiseSlider.SetActive(false);
            BetAmountSlider.value = 0;
            onSliderValueChange();

        }





        public void OnFoldBtnClick()
        {
            OnFolding();
            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ButtonSound, false);
        }

        public void SetAllInStatus()
        {
            GetMyPlayerInfo().PokerPlayerCashAllIn = true;
        }

        public void AllPokerBetsGoToFinalPoint()
        {
            GameManager gameManager = GameManager.Instance;
            foreach (PlayerInfo pInfo in gameManager.playersList)
            {
                // Debug.LogError("packed State....2...1");
                if (pInfo.pokerBetAmountAnim != null)
                {
                    // Debug.LogError("packed State....2...2");
                    pInfo.pokerBetAmountAnim.GetComponent<PokerBetAmountAnim>().PlayAnimation(PokerManager.Instance.FinalPokerBetAmountPoint, true, 0);
                    // Debug.LogError("packed State....2...2");
                    Pot.instance.PotTxt.text = LocalSettings.Rs(TotalPotAmount());
                    Pot.instance.PotPanel.SetActive(true);
                }
            }
            CurrentTargetBetAmount = 0;
            // PokerManager.Instance.NowDropCommunityCard(0.5f);
            PokerManager.Instance.DropCommunityCard();
        }

        public BigInteger TotalPotAmount()
        {
            BigInteger totalPotPockerAmount = 0;
            //foreach (PlayerInfo item in PlayerStateManager.Instance.PlayingList)
            //{
            //totalPotPockerAmount += item.PokerTotalWholeBetAmount;
            //}
            totalPotPockerAmount = amountPlacedOnBet;
            // Debug.LogError("Amount : " + amountPlacedOnBet);
            //amountPlacedOnBet = 0;
            return totalPotPockerAmount;
        }
        public BigInteger amountPlacedOnBet;
        public void BetPlacedAmount(BigInteger amount)
        {
            amountPlacedOnBet += amount;
        }

        PlayerInfo GetMyPlayerInfo()
        {
            if (MyLocalPlayerInfo == null)
                MyLocalPlayerInfo = UIManager.Instance.GetMyPlayerInfo();
            return MyLocalPlayerInfo;
        }

        #region Ai Section

        [HideInInspector]
        public bool isAISliderBtnClick;
        [ShowOnly] public bool isAIAllIn;


        public void CallAIButtonClick(bool isSliderBtn)
        {
            UIManager uIManager = UIManager.Instance;
            isAISliderBtnClick = isSliderBtn;

            if (isAISliderBtnClick)
            {

                AIBetAmoutSlider.minValue = 1;
                SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ButtonSound, false);
                if (MinBetAmount == LocalSettings.MinBetAmount / 2)
                {
                    AIBetAmoutSlider.maxValue = (int)(LocalSettings.AI_Amount / LocalSettings.MinBetAmount); // Set this to the maximum number of increments based on total amount

                }
                else
                {
                    BigInteger remainingAmountPokerBuyIN = LocalSettings.AI_Amount - MinBetAmount;
                    AIBetAmoutSlider.maxValue = (int)(remainingAmountPokerBuyIN / LocalSettings.MinBetAmount);
                }

                int maxvalue = Mathf.RoundToInt(AIBetAmoutSlider.maxValue) + 1;
                int minValue = Mathf.RoundToInt(AIBetAmoutSlider.minValue);
                int betAmountAI = Random.Range(minValue, maxvalue);

                if (MinBetAmount == LocalSettings.MinBetAmount / 2)
                {

                    currentSliderAmount = (BigInteger)(betAmountAI) * LocalSettings.MinBetAmount + MinBetAmount;
                }
                else
                {
                    if (LocalSettings.MinBetAmount >= LocalSettings.AI_Amount && betAmountAI == maxvalue)
                    {
                        currentSliderAmount = LocalSettings.AI_Amount;
                        isAIAllIn = true;

                    }
                    else
                    {

                        currentSliderAmount = ((BigInteger)(betAmountAI * LocalSettings.MinBetAmount)) + LocalSettings.MinBetAmount;


                    }
                }
                if (LocalSettings.MinBetAmount < LocalSettings.AI_Amount) { }
                //Here is code enter
                if (!CheckBoolAllIN())
                    uIManager.GetMyPlayerInfo().ResetIsBetPlacedPocker();
            }


            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ChipAdding, false);
            uIManager.GetAIPlayerInfo().myPockerTurnComplete(true);
            uIManager.GetAIPlayerInfo().UpdatePlayerState(PlayerState.STATE.BetPlaced);
        }

        public void AICheckBtn()
        {

            UIManager uIManager = UIManager.Instance;
            uIManager.GetAIPlayerInfo().isBetPlacedPocker = true;

            if (!checkPockeBetPlaced())
            {
                Debug.LogError("Check Here    ");
                uIManager.GetAIPlayerInfo().myPockerTurnComplete(true);
                // Debug.LogError("Value Of IsBetPlacedPocker......" + uIManager.GetMyPlayerInfo().isBetPlacedPocker);
            }

            uIManager.GetAIPlayerInfo().currentPlayerStateRef.UpdateCurrentPlayerState(PlayerState.STATE.WaitingForTurn);



            if (checkPockeBetPlaced())
            {
                Debug.Log("Check Here    1");
                //  Debug.LogError("changing state to AB First turn");
                RoomStateManager.Instance.UpdateCurrentStateOnShowBtn(RoomState.STATE.ABFirstTurn);
            }
            SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.ButtonSound, false);
        }

        public bool IsAICheckBool;
        public bool callAIBetPocker;
        public bool isAiRaisedBet;
        public void SetAiMinBetAmount()
        {
            //BigInteger BetDifference = CurrentTargetBetAmount - GetMyPlayerInfo().PokerTotalWholeBetAmount;

            BigInteger BetDifference = CurrentAITargetBetAmount - LocalSettings.AI_Amount;
            OffSet = BetDifference;
            //Debug.LogError("Bet Diff: " + BetDifference + "     CurrentTargetBetAmount: " + CurrentTargetBetAmount + "       Totalcash: " + GetMyPlayerInfo().pokerTotalBetCash);
            Debug.LogError("here is Ai bet  0" + BetDifference);
            if (BetDifference == 0 && !CheckBoolAllIN())
            {
                IsAICheckBool = true;
                callAIBetPocker = false;
                if (LocalSettings.AI_Amount > 0)
                    isAiRaisedBet = true;
                else
                    isAiRaisedBet = false;

                MinBetAmount = 0;
                Debug.LogError("here is Ai bet  1" + BetDifference);
            }
            else if (BetDifference < LocalSettings.AI_Amount)
            {
                MinBetAmount = BetDifference;
                Debug.LogError("here is Ai bet  2" + BetDifference);

            }
            else
            {
                if (LocalSettings.AI_Amount != 0)
                {

                    MinBetAmount = LocalSettings.AI_Amount;

                    Debug.LogError("here is Ai bet  4" + BetDifference);
                    isAIAllIn = true;
                }

                isAiRaisedBet = false;

            }
            //  Debug.LogError("Min bet:  " + MinBetAmount + "     Max Bet Amount: " + MaxBetAmount + "    Is all in: " + isAllIn + "      Bet difference: " + BetDifference + "     CurrentTargetBetAmount: " + CurrentTargetBetAmount + "       Totalcash: " + GetMyPlayerInfo().pokerTotalBetCash);
            if (isAllIn)
            {
                IsAICheckBool = false;
                callAIBetPocker = false;


            }
            else
            {

            }

            AIBetAmoutSlider.value = 0;
            onAISliderValueChange();

        }
        public void onAISliderValueChange()
        {
            //double CurrentSliderVal = BetAmountSlider.value / NumberOfSteps;
            //MaxBetAmount = LocalSettings.GetPokerBuyInChips();
            //BigInteger finalSliderVal = MaxBetAmount - OffSet;

            //currentSliderAmount = ((BigInteger)(CurrentSliderVal * (double)finalSliderVal)) + OffSet;
            int increment = (Mathf.RoundToInt(AIBetAmoutSlider.value));
            if (AiMinBetAmount == LocalSettings.MinBetAmount / 2)
            {

                currentAiSliderAmount = (increment * LocalSettings.MinBetAmount) + AiMinBetAmount;
                if (increment == AIBetAmoutSlider.maxValue)
                {

                    isAIAllIn = true;
                    //  Debug.LogError("Check All In Bet...1" + isAllIn);
                    currentAiSliderAmount = LocalSettings.AI_Amount;
                }
                else
                {
                    isAIAllIn = false;

                }
            }
            else
            {
                // Debug.LogError("slider value....." + OffSet);
                if (IsAICheckBool)
                {
                    currentAiSliderAmount = (increment * LocalSettings.MinBetAmount) + LocalSettings.MinBetAmount;
                }
                else
                {
                    currentAiSliderAmount = (increment * LocalSettings.MinBetAmount) + AiMinBetAmount;
                    // Debug.LogError("check Current AMount..." + currentSliderAmount + "....Slider value..." + increment + "......Minimum Bet....." + LocalSettings.MinBetAmount + "Check Poker Min Bet" + MinBetAmount);
                }
                if (increment == AIBetAmoutSlider.maxValue)
                {

                    isAIAllIn = true;

                    currentAiSliderAmount = LocalSettings.AI_Amount;
                }
                else
                {
                    isAIAllIn = false;

                }
            }
            //if (LocalSettings.MinBetAmount >= LocalSettings.GetPokerBuyInChips())
            //{
            //    currentSliderAmount = LocalSettings.GetPokerBuyInChips();
            //    SliderRaiseAmountTxt.text = "ALL IN";
            //    isAllIn = true;
            //    BetAmountSlider.interactable = false;
            //    pokerMinusBtn.interactable = false;
            //    pokerPlusBtn.interactable = false;
            //}
        }

        public void OnRaiseAISliderBtnClick()
        {

            if (LocalSettings.AI_Amount >= currentAiSliderAmount)
            {
                LocalSettings.AI_Amount -= currentAiSliderAmount;


                bool isAllInBet = LocalSettings.AI_Amount > 0 ? false : true;
                isAllIn = isAllInBet;
                // Debug.LogError("Check All In Bet...1" + isAllIn);
                UIManager.Instance.GetAIPlayerInfo().SendPokerBet(currentAiSliderAmount, isAllInBet);

            }
            AIMaxBetAmount = LocalSettings.AI_Amount;

        }

        public void OnAIRaiseSliderBtnClick()
        {
            if (BetRaiseSliderBtn.transform.GetChild(0).gameObject.activeInHierarchy)
            {
                BetRaiseLabelObj.SetActive(false);
                BetRaiseSlider.SetActive(true);
                AIBetAmoutSlider.value = 0;
                onSliderValueChange();
            }
            else
            {
                if (LocalSettings.GetPokerBuyInChips() >= currentSliderAmount)
                {
                    LocalSettings.SetPokerBuyInChips(-currentSliderAmount);
                    PokerManager.Instance.SetStartingAmount();
                    bool isAllInBet = LocalSettings.GetPokerBuyInChips() > 0 ? false : true;
                    isAllIn = isAllInBet;
                    // Debug.LogError("Check All In Bet...1" + isAllIn);
                    GetMyPlayerInfo().SendPokerBet(currentSliderAmount, isAllInBet);
                    BetRaiseLabelObj.SetActive(true);
                    BetRaiseSlider.SetActive(false);
                }
                MaxBetAmount = LocalSettings.GetPokerBuyInChips();
                onSliderValueChange();
            }


            UIManager.Instance.PlayerTotalChipsTxt.text = LocalSettings.Rs(LocalSettings.GetPokerBuyInChips());

            if (MinBetAmount > MaxBetAmount)
            {
                BetRaiseSliderBtn.SetActive(false);
                BetRaiseSlider.SetActive(false);
            }
            else
            {
                BetRaiseSliderBtn.SetActive(true);
                BetRaiseSlider.SetActive(false);
            }
        }

        #endregion
    }
}
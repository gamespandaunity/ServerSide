using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace TeenPattiGame
{
    public class UIManager : MonoBehaviour
    {
        #region AI_Variables And Components
        private GameObject AIPlayer;

        private PlayerInfo AI_Info;
        private BetAmountToTargetAnim AI_betAmountAnim;

        public GameObject AI_Player
        {
            set
            {
                AIPlayer = value;
                AssignAIReferences();
            }
            get
            {
                return AIPlayer;
            }
        }
        public PlayerInfo GetAIPlayerInfo()
        {
            return AI_Info;
        }

        void AssignAIReferences()
        {
            AI_Info = AIPlayer.GetComponent<PlayerInfo>();
            myPlayerState = AIPlayer.GetComponent<PlayerCurrentState>();
            AI_betAmountAnim = AI_Info.BetAmountAnim.transform.GetComponent<BetAmountToTargetAnim>();
        }
        #endregion




        public GameObject dialogueBox;
        public TMP_Text Room_Pass_Text;
        //Old THings
        public GameObject[] panelReference;

        //  public Collections BGs;
        public GameObject DisconnectedPanel;
        public GameObject LoadingPanel;
        public GameObject ActionTable;
        public GameObject sideShowPanel;
        public GameObject InfoObj;
        public GameObject quickShop;
        private GameObject LocalPlayer;

        public GameObject GiftTranferpanel;

        // Level up Panel


        public Image MainBG;
        public Image FillerImage;

        public TMP_Text TurnText;
        public TMP_Text TimeText;
        public TMP_Text CurrentChalAmoundText;
        public TMP_Text ActorNumber;
        public TMP_Text PlayerTotalChipsTxt;
        public TMP_Text ChaalTypeText;
        public TMP_Text InfoTxt;
        public TMP_Text playerName;
        //public TMP_Text modeText;

        public TMP_Text AndarBetAmountBtnTxt;
        public TMP_Text BaharBetAmountBtnTxt;

        //public TMP_Text SuperAndarBetAmountBtnTxt;
        //public TMP_Text SuperBaharBetAmountBtnTxt;

        // PlayerInformation
        public GameObject PlayerInfoPanel;
        public TMP_Text TotalWinAmountText;
        public TMP_Text TotalHandsText;
        public TMP_Text PlayerTotalCashText;
        public TMP_Text playerInfoName;
        public TMP_Text playerLevelText;
        public Image profileImage;
        public Image profileFrameImage;
        public GameObject fillerLevelImage;
        public GameObject micObj;
        public GameObject speakerObj;

        [ShowOnly]
        public int TotalHands;
        [ShowOnly]
        public int TotalWinHands;
        [ShowOnly]
        public bool isPlayerPlayedThisHand;

        public BigInteger TotalWinsAmount;

        public BigInteger TotalBetPlacedAmount;

        public BigInteger TotalBetPlaceFor1Game;
        public BigInteger TotalWinAmountFor1Game;


        public PlayerInfo myPlayerInfo;

        private PlayerCurrentState myPlayerState;
        private BetAmountToTargetAnim betAmountAnim;



        public Button SwithcTableBtn;

        public Button BetAmountIncreaseBtn;
        public Button BetAmountDecreaseBtn;
        public Button showBtn;
        public Button sideShowBtn;
        public Button AcceptSideShowBtn;
        public Button cancelSideShowBtn;
        // Andar Bahar Btns
        public Button SkipBetBtn;
        public Button AndarBetBtn;
        public Button AndarNveBtn;
        public Button AndarPveBtn;
        public Button BaharBetBtn;
        public Button BaharNveBtn;
        public Button BaharPveBtn;
        // Super Andar bahar btns
        //public Button SuperAndarBetBtn;
        //public Button SuperAndarNveBtn;
        //public Button SuperAndarPveBtn;
        //public Button SuperBaharBetBtn;
        //public Button SuperBaharNveBtn;
        //public Button SuperBaharPveBtn;



        public int TotalChals;
        int ChallsDone;
        private static UIManager _instance;
        public GameObject MyLocalPlayer
        {
            set
            {
                LocalPlayer = value;
                AssignReferences();
            }
            get
            {
                return LocalPlayer;
            }
        }


        public int TotalCircleChals()
        {
            return ChallsDone;
        }

        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = GameObject.FindObjectOfType<UIManager>();
                return _instance;
            }
        }
        void Awake()
        {
            if (_instance == null)
                _instance = this;
        }


        public PlayerInfo GetMyPlayerInfo()
        {
            return myPlayerInfo;
        }






        public PlayerCurrentState GetMyPlayerCurrentState()
        {
            return myPlayerState;
        }



        public BetAmountToTargetAnim BetAmountToTargetAnim()
        {
            return betAmountAnim;
        }


        void AssignReferences()
        {
            myPlayerInfo = LocalPlayer.GetComponent<PlayerInfo>();
            myPlayerState = LocalPlayer.GetComponent<PlayerCurrentState>();
            betAmountAnim = myPlayerInfo.BetAmountAnim.transform.GetComponent<BetAmountToTargetAnim>();
        }



        // Start is called before the first frame update
        void Start()
        {
            LoadingPanel.SetActive(false);
            Room_Pass_Text.text = "Room Pass: " + LocalSettings.Room_Password;
            Room_Pass_Text.gameObject.SetActive(MatchHandler.isOffline() ? false : true);
            // TotalWinAmountText.text = "0";
            //  TotalHandsText.text = "0/0";
            //  isPlayerPlayedThisHand = false;
            //  profileFrameImage = profileImage.transform.GetChild(0).GetComponent<Image>();

            SetbackGrounds();
            if (MatchHandler.isOffline())
            {
                micObj.SetActive(false);
                speakerObj.SetActive(false);
            }

        }

        void SetbackGrounds()
        {
            // if (MatchHandler.IsTeenPatti())
            // MainBG.sprite = BGs.Sprites[0];


        }

        public void UpDateCurrentChalAmountText(BigInteger BetAmount)
        {
            CurrentChalAmoundText.text = LocalSettings.Rs(BetAmount);
            ConstantsData_M.CurrentBetSpawnAmount = (int)BetAmount;
            // Debug.LogError($"Bet Text {BetAmount} {CurrentChalAmoundText.text} {Constants_M.CurrentBetSpawnAmount}");
        }

        public void UpdateTexts(string WinAmount, string Hands, string CashAmount, Sprite profileSprite, int size, string namePlayer)
        {
            // PlayerTotalCashText.text = CashAmount;
            string totalAmount = WinAmount;
            if (WinAmount.Contains("-"))
            {
                // TotalWinAmountText.color = Color.red;
                string aa = "-";
                WinAmount = totalAmount.Replace(aa, "");
                //  TotalWinAmountText.text = "-" + LocalSettings.Rs(WinAmount);
            }
            else
            {
                // TotalWinAmountText.color = Color.green;
                // TotalWinAmountText.text = LocalSettings.Rs(WinAmount);
            }
            //  TotalHandsText.text = Hands;
            //  profileImage.sprite = profileSprite;
            // profileFrameImage.sprite = profileFrameSprite;


        }
        public void UpdateTheWinAmount(string totalcashWinLossKey, string TotalHandsKey, string WinHandsKey)
        {
            "Update Win Amount".Show();
            if (isPlayerPlayedThisHand && GetMyPlayerInfo().this_photonView.isOwned)       //photon removal
            {
                TotalHands++;
                ///Debug.LogError("check Total Hands win...2");                                
            }

            BigInteger currentWinReport = TotalWinsAmount - TotalBetPlacedAmount;
            // When Room left
            LocalSettings.winOrLoseAmount = currentWinReport;
            //Debug.LogError("Win or Lose Amont  " + LocalSettings.winOrLoseAmount);
            GetMyPlayerInfo().playerCustomProperties.SetCustomBigIntegerData(totalcashWinLossKey, currentWinReport);
            GetMyPlayerInfo().playerCustomProperties.SetCustomData(TotalHandsKey, TotalHands);
            GetMyPlayerInfo().playerCustomProperties.SetCustomData(WinHandsKey, TotalWinHands);
            GetMyPlayerInfo().playerCustomProperties.SetCustomBigIntegerData(LocalSettings.MyTotalCashKey, LocalSettings.GetTotalChips());


            //TotalWinAmountText.text = currentWinReport.ToString();
            //TotalHandsText.text = TotalWinHands + "/" + TotalHands;            

        }

        // Transfer Gift To single Player




        public void SetTotalWinHandByPlayer()
        {
            if (TotalBetPlaceFor1Game < TotalWinAmountFor1Game)
            {
                TotalWinHands++;
            }
        }

    }
}
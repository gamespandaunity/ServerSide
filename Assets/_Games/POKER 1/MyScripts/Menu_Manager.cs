using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace POKER
{
    public class Menu_Manager : MonoBehaviour
    {
        private static Menu_Manager instance;
        public static Menu_Manager Instance
        {
            get
            {
                if (instance == null)
                    instance = GameObject.FindObjectOfType<Menu_Manager>();
                return instance;
            }
        }
        public POKER.Collections teenPattiVariationSprites;
        public GameObject Main_Menu_Panel;
        public GameObject Select_Mode_Panel;
        public GameObject Select_Room_Panel;
        public GameObject Select_Avatar_Panel;
        public GameObject Enter_Password_panel;
        public GameObject dialogueBoxPanel;
        public GameObject quick_Shop;
        // For TeenPattiBetMode
        private Button Joined_Room_Btn;
        private Button onlineBtn;
        private TMP_InputField enter_Pass;


        public GameObject quitePanel;


        public TMP_Text TotalChips;


        public Texture2D ProfileImageTexture;

        BigInteger maxP500;


        BigInteger minP250;



        private void Awake()
        {
            onlineBtn = Select_Mode_Panel.transform.GetChild(0).GetComponent<Button>();

        }

        NetworkSettings networkSettings;
        // Start is called before the first frame update
        void Start()
        {

            networkSettings = NetworkSettings.Instance;
            Active_My_Panel(Main_Menu_Panel.name);
            // HandleBGMusic();
            ClearBuyInAmount();
            enter_Pass = Enter_Password_panel.transform.GetChild(0).GetComponent<TMP_InputField>();
            Joined_Room_Btn = Enter_Password_panel.transform.GetChild(1).GetComponent<Button>();
            if (!PlayerPrefs.HasKey(LocalSettings.TotalChips))
            {
                //Debug.LogError("Here We Go");
                LocalSettings.SetTotalChips(LocalSettings.GetTotalChips() + 125000);
            }
            else if (LocalSettings.GetTotalChips() <= LocalSettings.MinBetAmount)
            {
                quick_Shop.SetActive(true);
                //FindObjectOfType<GetAPICash>().UpdateThisText(LocalSettings.GetTotalChips());

            }
            maxP500 = LocalSettings.PokerMultiplayerMax * 500;
            minP250 = 0;

            if (!PlayerPrefs.HasKey(LocalSettings.PlayernameKey))
            {
                int number = Random.Range(1, 10000);
                LocalSettings.SetPlayername("Player" + number.ToString());

            }
            TotalChips.text = LocalSettings.Rs(LocalSettings.GetTotalChips());
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            //StartCoroutine(CheckInterNetConnection());
            //StartCoroutine(SaveCustomeData());
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
        public void Active_My_Panel(string name_Object)
        {
            Main_Menu_Panel.SetActive(name_Object.Equals(Main_Menu_Panel.name));
            Select_Mode_Panel.SetActive(name_Object.Equals(Select_Mode_Panel.name));
            Select_Room_Panel.SetActive(name_Object.Equals(Select_Room_Panel.name));
            Select_Avatar_Panel.SetActive(name_Object.Equals(Select_Avatar_Panel.name));
        }
        void ClearBuyInAmount()
        {
            if (LocalSettings.GetPokerBuyInChips() > 0)
            {
                BigInteger amount = LocalSettings.GetPokerBuyInChips();
                LocalSettings.SetTotalChips(amount);
                LocalSettings.SetPokerBuyInChips(-amount);
            }
            TotalChips.text = LocalSettings.Rs(LocalSettings.GetTotalChips());
        }

        //IEnumerator CheckInterNetConnection()
        //{
        //    while (true)
        //    {
        //        if (Application.internetReachability == NetworkReachability.NotReachable)
        //        {
        //            onlineBtn.interactable = false;
        //            networkSettings.loadingPanel.SetActive(false);
        //            networkSettings.enabled = false;
        //        }
        //        else
        //        {
        //            PhotonNetwork.ConnectUsingSettings();
        //            onlineBtn.interactable = true;
        //            networkSettings.enabled = true;
        //        }
        //        int delay = Random.Range(1, 4);
        //        yield return new WaitForSeconds(delay);
        //    }
        //}

        //IEnumerator SaveCustomeData()
        //{

        //    yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);
        //    //PhotonNetwork.LocalPlayer.SetCustomBigIntegerData(LocalSettings.MyTotalCashKey, LocalSettings.GetTotalChips());
        //    //PhotonNetwork.LocalPlayer.SetCustomString(LocalSettings.player_ID_Key, LocalSettings.GetPlayerName());
        //    //PhotonNetwork.LocalPlayer.NickName = LocalSettings.GetPlayerName();

        //}


        //public void Create_And_Join_Room()
        //{
        //    if (Application.internetReachability == NetworkReachability.NotReachable)
        //    {
        //        LocalSettings.Show_Dialogue(dialogueBoxPanel, "Check Your Internet Connection...");
        //        return;
        //    }
        //    if (!PhotonNetwork.IsConnectedAndReady)
        //    {
        //        LocalSettings.Show_Dialogue(dialogueBoxPanel, "Connecting with Server...");
        //        return;
        //    }

        //    string RoomID = GenerateRandomRoomName();

        //    if (networkSettings.checkRoomsStatus(RoomID))
        //        networkSettings.RoomEntranceProperty(GenerateRandomRoomName());
        //    else
        //        Create_And_Join_Room();
        //}

        private string GenerateRandomRoomName()
        {
            int randomInt = Random.Range(10000000, 99999999);
            return randomInt.ToString();
        }








        AudioSource BGMusicAS;
        public void HandleBGMusic()
        {
            if (BGMusicAS)
            {
                BGMusicAS.Stop();
                BGMusicAS = null;
            }
            //if (LocalSettings.GetSoundEffect())
            //    BGMusicAS = SoundManager.Instance.PlayAudioClip(SoundManager.AllSounds.BGMusic, true, true);
            //else if (BGMusicAS)
            //{
            //    BGMusicAS.Stop();
            //    BGMusicAS = null;
            //}

        }

        public void SelectTable(int EnumNumber)
        {
            MatchHandler.CurrentMatch = (MatchHandler.MATCH)EnumNumber;
            // Instance.currentRoomFilter = (RoomFilters)EnumNumber;



            //if (MatchHandler.IsTeenPatti())            
            //    // StartCoroutine(WaitForLoadRoom(EnumNumber));

            //   StartCoroutine(WaitForLoadRoom(EnumNumber));

            //StartCoroutine(WaitForLoadRoom(EnumNumber));


        }





        


        public BigInteger SetPokerMaxEntryFee(BigInteger Fee)
        {
            BigInteger pokerFee = 0;
            string minFee = "";
            string feeString = Fee.ToString();
            switch (feeString)
            {
                case "500":
                    pokerFee = maxP500;
                    minFee = LocalSettings.Rs(minP250);
                    break;


            }

            if (Fee <= 1000000)
                LocalSettings.pokerEntryFeeString = minFee + "-" + LocalSettings.Rs(pokerFee);
            return pokerFee;
        }


        public void input_Value_Changes()
        {
            string room_id = enter_Pass.text;
            //foreach (RoomInfo item in roomInfos)
            //{
            //    if()
            //}
            bool isTrue = Interactabel_Btn(room_id);
            Joined_Room_Btn.interactable = isTrue;
            if (isTrue)
            {
                Joined_Room_Btn.onClick.RemoveAllListeners();
                if (networkSettings.enabled == true)
                    Joined_Room_Btn.onClick.AddListener(() => networkSettings.Enter_Room_With_Password(room_id));
                else
                    LocalSettings.Show_Dialogue(dialogueBoxPanel, "Check Your Internet Connection");
            }



        }

        bool Interactabel_Btn(string room_Id)
        {
            return room_Id.Length >= enter_Pass.characterLimit;
        }

        public void Start_Offline_Mode()
        {
            BigInteger betAmount = LocalSettings.StringToBigInteger("500");
            LocalSettings.Poker_Max_Entry_Fee = SetPokerMaxEntryFee(betAmount);

            LocalSettings.MinBetAmount = betAmount;
            //PhotonNetwork.AutomaticallySyncScene = false;
            MatchHandler.CurrentMatch = MatchHandler.MATCH.OffLine;
            SceneManager.LoadSceneAsync("PokerGameplay");
        }







        public void ResetPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }









        public void GameQuit()
        {
            Application.Quit();
        }






        public Sprite ConvertTexture2DToSprite(Texture2D tex)
        {
            if (ProfileImageTexture)
            {
                Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), UnityEngine.Vector2.one);
                return sprite;
            }
            else return null;
        }




    }
}
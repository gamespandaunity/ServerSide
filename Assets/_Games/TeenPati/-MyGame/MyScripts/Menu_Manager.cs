using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace TeenPattiGame
{
    public class Menu_Manager : NetworkBehaviour
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
        public Collections teenPattiVariationSprites;
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

        private void Awake()
        {

            instance = this;

            onlineBtn = Select_Mode_Panel.transform.GetChild(0).GetComponent<Button>();
            float fivePowerOne = Mathf.Pow(5, 1);
            float twoPowerOne = Mathf.Pow(2, 0);

            Debug.Log("5^1 = " + fivePowerOne * twoPowerOne);


        }

        //photon removal  NetworkSettings networkSettings;
        // Start is called before the first frame update
        void Start()
        {

            //photon removal    networkSettings = NetworkSettings.Instance;
            Active_My_Panel(Main_Menu_Panel.name);
            // HandleBGMusic();

            enter_Pass = Enter_Password_panel.transform.GetChild(0).GetComponent<TMP_InputField>();
            Joined_Room_Btn = Enter_Password_panel.transform.GetChild(1).GetComponent<Button>();
            if (!PlayerPrefs.HasKey(LocalSettings.TotalChips))
            {
                //Debug.LogError("Here We Go");
                LocalSettings.SetTotalChips(LocalSettings.GetTotalChips() + 100000);
            }
            else if (LocalSettings.GetTotalChips() <= LocalSettings.MinBetAmount)
            {
                quick_Shop.SetActive(true);
                //FindObjectOfType<GetAPICash>().UpdateThisText(LocalSettings.GetTotalChips());

            }

            if (!PlayerPrefs.HasKey(LocalSettings.PlayernameKey))
            {
                int number = Random.Range(1, 10000);
                LocalSettings.SetPlayername("Player" + number.ToString());

            }
            TotalChips.text = LocalSettings.Rs(LocalSettings.GetTotalChips());
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            StartCoroutine(CheckInterNetConnection());
            StartCoroutine(SaveCustomeData());
        }


        IEnumerator SaveCustomeData()
        {
            yield return null;
            // yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);
            // PhotonNetwork.LocalPlayer.SetCustomBigIntegerData(LocalSettings.MyTotalCashKey, LocalSettings.GetTotalChips());          //photon removal
            // PhotonNetwork.LocalPlayer.SetCustomString(LocalSettings.player_ID_Key, LocalSettings.GetPlayerName());
            // PhotonNetwork.LocalPlayer.NickName = LocalSettings.GetPlayerName();
        }
        public void Active_My_Panel(string name_Object)
        {
            Main_Menu_Panel.SetActive(name_Object.Equals(Main_Menu_Panel.name));
            Select_Mode_Panel.SetActive(name_Object.Equals(Select_Mode_Panel.name));
            Select_Room_Panel.SetActive(name_Object.Equals(Select_Room_Panel.name));
            Select_Avatar_Panel.SetActive(name_Object.Equals(Select_Avatar_Panel.name));
        }

        public void Create_And_Join_Room()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                LocalSettings.Show_Dialogue(dialogueBoxPanel, "Check Your Internet Connection...");
                return;
            }
            // if (!PhotonNetwork.IsConnectedAndReady)
            // {
            //     LocalSettings.Show_Dialogue(dialogueBoxPanel, "Connecting with Server...");//photon removal
            //     return;
            // }

            string RoomID = GenerateRandomRoomName();

            // if (networkSettings.checkRoomsStatus(RoomID))
            //     networkSettings.RoomEntranceProperty(GenerateRandomRoomName());      //photon removal
            // else
            //     Create_And_Join_Room();
        }

        private string GenerateRandomRoomName()
        {
            int randomInt = Random.Range(10000000, 99999999);
            return randomInt.ToString();
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
                // if (networkSettings.enabled == true)
                //     Joined_Room_Btn.onClick.AddListener(() => networkSettings.Enter_Room_With_Password(room_id));        //photon removal
                // else
                LocalSettings.Show_Dialogue(dialogueBoxPanel, "Check Your Internet Connection");
            }



        }

        bool Interactabel_Btn(string room_Id)
        {
            return room_Id.Length >= enter_Pass.characterLimit;
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
            {
                quitePanel.SetActive(true);
            }
        }

        public void Start_Offline_Mode()
        {
            //photon removal  PhotonNetwork.AutomaticallySyncScene = false;
            MatchHandler.CurrentMatch = MatchHandler.MATCH.OffLine;
            SceneManager.LoadScene("GameplayTeenPatti");
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


        IEnumerator CheckInterNetConnection()
        {
            while (true)
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    onlineBtn.interactable = false;
                    //photon removal  networkSettings.enabled = false;
                }
                else
                {
                    //photon removal  PhotonNetwork.ConnectUsingSettings();
                    onlineBtn.interactable = true;
                    //photon removal  networkSettings.enabled = true;
                }
                int delay = Random.Range(1, 4);
                yield return new WaitForSeconds(delay);
            }
        }






        //bool isincrese5Power = false;

        //private int powerOf5 = 1;
        //private int powerOf2 = 0;



        public void OnButtonDecrease()
        {



            if (power5 <= power2)
                power2--;
            else
                power5--;
            valueOf5 = Mathf.Pow(5, power5);
            valueOf2 = Mathf.Pow(2, power2);

            Debug.LogError(" 5 ki power " + power5 + " 2 ki value " + power2 + " final bet accured  " + valueOf2 * valueOf5);

            //float valueOf5 = 0;
            //float valueOf2 = 0;
            //if (isincrese5Power)
            //{
            //    if (powerOf5 > 0)
            //        powerOf5--;
            //    valueOf5 = Mathf.Pow(5, powerOf5);
            //    valueOf2 = Mathf.Pow(2, powerOf2);
            //}
            //else
            //{
            //    if (powerOf2 > 0)
            //        powerOf2--;
            //    valueOf2 = Mathf.Pow(2, powerOf2);
            //    valueOf5 = Mathf.Pow(5, powerOf5);
            //}



            //Debug.LogError($"5^{powerOf5} = {valueOf5} 2^{powerOf2} = {valueOf2}  == result is  { valueOf2 * valueOf5}");
            //isincrese5Power = !isincrese5Power;
        }


        int power5 = 1;
        int power2 = 0;
        float finalBet = 0;
        public void OnBtnClickPress()
        {

            valueOf5 = Mathf.Pow(5, power5);
            valueOf2 = Mathf.Pow(2, power2);

            finalBet = valueOf2 * valueOf5;

            Debug.LogError(" 5 ki power " + power5 + " 2 ki value " + power2 + " final bet accured  " + valueOf2 * valueOf5);
        }
        float valueOf5 = 1;
        float valueOf2 = 0;

        public void OnButtonIncrese()
        {

            if (power5 <= power2)
                power5++;
            else
                power2++;

            valueOf5 = Mathf.Pow(5, power5);
            valueOf2 = Mathf.Pow(2, power2);

            //if (isincrese5Power)
            //{
            //    powerOf5++;
            //    valueOf5 = Mathf.Pow(5, powerOf5);
            //    valueOf2 = Mathf.Pow(2, powerOf2);
            //}
            //else
            //{
            //    powerOf2++;
            //    valueOf2 = Mathf.Pow(2, powerOf2);
            //    valueOf5 = Mathf.Pow(5, powerOf5);
            //}

            //isincrese5Power = !isincrese5Power;


            Debug.LogError(" 5 ki power " + power5 + " 2 ki value " + power2 + " final bet accured  " + valueOf2 * valueOf5);

        }
    }






}
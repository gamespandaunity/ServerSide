using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TeenPattiGame
{
    public class UpdateFrames : MonoBehaviour
    {

        public FramesCollections frameSpritesCircle;
        public GameObject frameBtn;
        public Image selectFame;
        public TMP_Text NameText;


        // making an instance
        private static UpdateFrames _Instance;
        public static UpdateFrames Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = GameObject.FindObjectOfType<UpdateFrames>();
                return _Instance;
            }
        }
        //pic of profile in main menu
        [Header("........Profile Frame Change.......")]
        [Space]
        //public Sprite[] ProfileImageChange;
        public Image[] profileFrameImage;
        public int profileFrameIndex;
        Transform FrameBtnsParent;

        [Header("..........Panel Active..........")]
        public GameObject avatarpanel;
        public GameObject framePanel;

        private void Awake()
        {
            if (_Instance == null)
                _Instance = this;
        }
        // Start is called before the first frame update
        void Start()
        {

            UpdateFrames.Instance.NameText.text = PlayerPrefs.GetString("name");
            FrameBtnsParent = frameBtn.transform.parent;
            CreateAvatorBtns();
            ActiveMyPanel(avatarpanel.name);
        }


        //public void ClickOnUseBtn()
        //{
        //    LocalSettings.SetprofileFrame(profileFrameIndex);
        //    GetProfileImage();
        //}
        public void GetProfileImage()
        {
            //photon removal  NetworkSettings.Instance.AssignFrameToPlayerProperties(LocalSettings.GetprofileFrame());
            SetAllInUseLabelFalse();
            FrameBtnsParent.GetChild(LocalSettings.GetprofileFrame() + 1).GetChild(0).GetChild(0).gameObject.SetActive(true);


        }



        /// <summary>
        /// ///////////////////////////////////////////////////////////////
        /// </summary>
        void CreateAvatorBtns()
        {
            // Creating avator btns
            for (int i = 0; i < frameSpritesCircle.Sprites.Length; i++)
            {
                GameObject AvatBtn = Instantiate(frameBtn);
                LocalSettings.SetPosAndRect(AvatBtn, frameBtn.GetComponentInChildren<RectTransform>(), frameBtn.transform.parent);
                AvatBtn.transform.GetChild(0).GetComponent<Image>().sprite = frameSpritesCircle.Sprites[i].Sprites;
                AvatBtn.SetActive(true);

                // Adding button listener
                Button BtnAvat = AvatBtn.GetComponentInChildren<Button>();
                BtnAvat.onClick.AddListener(() => OnAvatorSelectBtnClick());
            }

            // Setting profile pic sprite on main page
            if (LocalSettings.IsProfileFrameSet())
            {
                selectFame.sprite = frameSpritesCircle.Sprites[LocalSettings.GetprofileFrame()].Sprites;
                FrameBtnsParent.GetChild(LocalSettings.GetprofileFrame() + 1).GetChild(0).GetChild(0).gameObject.SetActive(true);
                profileFrameIndex = LocalSettings.GetprofileFrame();
            }
            else
            {
                selectFame.sprite = frameSpritesCircle.Sprites[0].Sprites;
                FrameBtnsParent.GetChild(1).GetChild(0).GetChild(0).gameObject.SetActive(true);
                LocalSettings.SetprofileFrame(0);
                profileFrameIndex = 0;
            }
            FrameBtnsParent.GetChild(profileFrameIndex + 1).GetChild(0).GetChild(0).gameObject.SetActive(true);
        }

        void SetAllInUseLabelFalse()
        {
            for (int i = 0; i < FrameBtnsParent.childCount; i++)
            {
                FrameBtnsParent.GetChild(i).GetChild(0).GetChild(0).gameObject.SetActive(false);
            }
        }

        void OnAvatorSelectBtnClick()
        {
            int indexOfChild = EventSystem.current.currentSelectedGameObject.transform.GetSiblingIndex();
            selectFame.sprite = frameSpritesCircle.Sprites[indexOfChild - 1].Sprites;
            profileFrameIndex = indexOfChild - 1;





        }

        public void ActiveMyPanel(string panelName)
        {
            avatarpanel.SetActive(panelName.Equals(avatarpanel.name));
            UpdateAvatar updateAvatar = UpdateAvatar.Instance;
            LocalSettings.CheckAvatarName(updateAvatar.selectAvatar, updateAvatar.AvatorSpritesSquare, LocalSettings.GetprofilePic(), updateAvatar.avatarNameTxt);

        }

    }
}
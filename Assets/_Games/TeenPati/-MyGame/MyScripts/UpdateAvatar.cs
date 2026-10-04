using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TeenPattiGame
{
    public class UpdateAvatar : MonoBehaviour
    {

        public AvatarCollections AvatorSpritesSquare;
        public GameObject AvatorBtn;
        public Image selectAvatar;
        public TMP_Text avatarNameTxt;

        //pic of profile in main menu
        [Header("........Profile Pic Change.......")]
        [Space]
        //public Sprite[] ProfileImageChange;
        public Image[] ProfileImage;

        int ProfileIndex;
        Transform avatorBtnsParent;

        private static UpdateAvatar _Instance;
        public static UpdateAvatar Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = GameObject.FindObjectOfType<UpdateAvatar>();

                return _Instance;
            }
        }


        // Start is called before the first frame update
        private void Awake()
        {

            _Instance = this;
        }

        void Start()
        {
            // avatorBtnsParent = AvatorBtn.transform.parent;
            //  CreateAvatorBtns();
        }


        public void ClickOnUseBtn()
        {

            LocalSettings.SetprofilePic(ProfileIndex);
            GetProfileImage();




        }
        GameObject NewObj;
        Image image;
        void GetProfileImage()
        {
            //photon removal   NetworkSettings.Instance.AssignPicToPlayerProperties(LocalSettings.GetprofilePic());
            SetAllInUseLabelFalse();

            avatorBtnsParent.GetChild(LocalSettings.GetprofilePic() + 1).GetChild(0).gameObject.SetActive(true);

            //ProfileImage.sprite = AvatorSpritesCircle.Sprites[LocalSettings.GetprofilePic()];
            AllAvatarSpriteSame(AvatorSpritesSquare.Sprites[LocalSettings.GetprofilePic()].Sprites);
            if (NewObj == null)
            {
                NewObj = new GameObject(); //Create the GameObject
                NewObj.AddComponent<RectTransform>();
                LocalSettings.SetPosAndRect(NewObj, selectAvatar.GetComponent<RectTransform>(), selectAvatar.transform);
                image = NewObj.AddComponent<Image>();
            }
            image.sprite = AvatorSpritesSquare.Sprites[LocalSettings.GetprofilePic()].Sprites;
            NewObj.SetActive(false);

            Debug.Log("Sprite name is: " + image.name);
        }

        public void AllAvatarSpriteSame(Sprite serverSprite)
        {
            foreach (Image item in ProfileImage)
            {
                //item.sprite = AvatorSpritesSquare.Sprites[LocalSettings.GetprofilePic()].Sprites;
                item.sprite = serverSprite;
            }

            LocalSettings.ServerSideImge = serverSprite;
        }

        /// <summary>
        /// ///////////////////////////////////////////////////////////////
        /// </summary>
        void CreateAvatorBtns()
        {
            // Creating avator btns
            for (int i = 0; i < AvatorSpritesSquare.Sprites.Length; i++)
            {
                GameObject AvatBtn = Instantiate(AvatorBtn);
                LocalSettings.SetPosAndRect(AvatBtn, AvatorBtn.GetComponentInChildren<RectTransform>(), AvatorBtn.transform.parent);
                AvatBtn.GetComponent<Image>().sprite = AvatorSpritesSquare.Sprites[i].Sprites;
                AvatBtn.SetActive(true);

                // Adding button listener
                Button BtnAvat = AvatBtn.GetComponentInChildren<Button>();
                BtnAvat.onClick.AddListener(() => OnAvatorSelectBtnClick());
            }

            // Setting profile pic sprite on main page
            if (LocalSettings.IsProfilePicSet())
            {
                LocalSettings.CheckAvatarName(selectAvatar, AvatorSpritesSquare,
                    LocalSettings.GetprofilePic(), avatarNameTxt);
                // selectAvatar.sprite = AvatorSpritesSquare.Sprites[LocalSettings.GetprofilePic()].Sprites;
                // avatarNameTxt.text = AvatorSpritesSquare.Sprites[LocalSettings.GetprofilePic()].Name;
                avatorBtnsParent.GetChild(LocalSettings.GetprofilePic() + 1).GetChild(0).gameObject.SetActive(true);
                ProfileIndex = LocalSettings.GetprofilePic();
            }
            else
            {
                //selectAvatar.sprite = AvatorSpritesSquare.Sprites[0].Sprites;
                LocalSettings.CheckAvatarName(selectAvatar, AvatorSpritesSquare,
                    0, avatarNameTxt);
                avatorBtnsParent.GetChild(1).GetChild(0).gameObject.SetActive(true);
                LocalSettings.SetprofilePic(0);
                ProfileIndex = 0;
            }
            avatorBtnsParent.GetChild(ProfileIndex + 1).GetChild(0).gameObject.SetActive(true);
            // ProfileImage.sprite = AvatorSpritesCircle.Sprites[LocalSettings.GetprofilePic()];
            AllAvatarSpriteSame(AvatorSpritesSquare.Sprites[LocalSettings.GetprofilePic()].Sprites);
        }

        void SetAllInUseLabelFalse()
        {
            for (int i = 0; i < avatorBtnsParent.childCount; i++)
            {
                avatorBtnsParent.GetChild(i).GetChild(0).gameObject.SetActive(false);
            }
        }

        void OnAvatorSelectBtnClick()
        {
            int indexOfChild = EventSystem.current.currentSelectedGameObject.transform.GetSiblingIndex();
            //   selectAvatar.sprite = AvatorSpritesSquare.Sprites[indexOfChild - 1];
            ProfileIndex = indexOfChild - 1;
            LocalSettings.CheckAvatarName(selectAvatar, AvatorSpritesSquare,
                       ProfileIndex, avatarNameTxt);

        }



    }
}
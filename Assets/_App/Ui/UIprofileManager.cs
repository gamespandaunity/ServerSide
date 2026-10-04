using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using static UIprofileManager;
using TMPro;


public class UIprofileManager : MonoBehaviour
{
    [Header("HEADER BUTTONS")]

    public Button ProfileButton;
    public Button BankDetailButton, CryptoButton, UpdatePasswordButton;

    public GameObject mainPanel;
    [Header("BANK DETAILS")]
    public GameObject addBankDetail;
    public GameObject fetchAllBankAccountPanel, selectBank;
    [Serializable]
    public struct BankDetail
    {
        public Text addupdateBankdetail;

        public TMP_InputField UserBankInformation;
        public GameObject BankInstanceHolder;
        public string bankid;
    }
    public delegate void updateName();
    public static updateName onUpdateName;

    [Header("PLAYER DETAIL")]
    public TMP_InputField firstName;
    public TMP_InputField lastName;
    public Text playersId;
    public Text emailTxt;
    public Text phoneTxt;
    public Text refferalTxt;
    public GameObject profileloader;
    public string filePath;
    public RawImage profileImage;
    public Texture2D profileTextImage;
    public static UIprofileManager instance;
    public BankDetail bank_detail;
    public GameObject profilePanel;
    public GameObject updateUIPanel;
    // add /update bank detail UI Panel
    public Text doyouwanttoselect;
    public Image checkboxImage;
    public int isSelected;
    public Sprite checkboxSprite;
    public Sprite uncheckedSprites;
    public Text addupdateText;
    // confirmation obj

    // Crypto Wallets
    [Header("CRYPTO DETAILS")]
    public GameObject addupdatecryptoWallet;
    public GameObject fetchAllCryptoAccountPanel;
    public GameObject cryptoDetailinginfo;
    public GameObject cryptoDetailinginforTemplate;
    public Text addupdateCryptoWallet;
    public TMP_InputField addupdateNetworkName;
    public TMP_InputField addupdateNetworkAddress;
    public Text cryptoselectionText;
    public Image cryptoCheckbox;
    public Text cryptoWalletupdatebtn;
    public string cryptowalletid;
    [Header("PASSWORDS")]
    public TMP_InputField enterOldPassword;
    public TMP_InputField enternewPassword;
    public TMP_InputField enternewPassword2;
    string about_Bank;
    public string About_Bank
    {
        get { return about_Bank; }
    }
    public Image tickImage;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    private void OnEnable()
    {
        ProfileButton.onClick.Invoke();
        mainPanel.gameObject.SetActive(true);
        if (Borderspanel.ChangeTitle != null)
        {
            Borderspanel.ChangeTitle("");
        }
        if (Borderspanel.UnselectCoin != null)
        {
            Borderspanel.UnselectCoin(false);
        }
    }


    void Start()
    {
        UserModel userModel = staticVariables.UserProfiledata;
        if (userModel.user != null && ApiAndRoomManager._instance != null)
        {
            firstName.text = userModel.user.first_name.ToString();
            lastName.text = userModel.user.last_name.ToString();

            ConstantsData_M.Log("kdsjksjdksjd" + userModel.user.first_name.ToString() + ":::" + userModel.user.last_name.ToString());
            playersId.text = userModel.user._id.ToString();
            emailTxt.text = userModel.user.email.ToString();
            phoneTxt.text = userModel.user.phone.ToString();
            refferalTxt.text = userModel.user.referral_code.ToString();
            if (userModel.user.file_url != null && userModel.user.file_url != "")
            {
                ServerConnection.DownloadSprite("/" + userModel.user.file_url
               , ImageTexture =>
               {
                   profileImage.texture = ImageTexture;
                   staticVariables.ProfilePicture = ImageTexture;

               },
               Onfailed =>
               {
                   //Debug.Log("Failed To Load Image with url " + userModel.user.file_url + " with " + Onfailed);
               }
               );
            }

            selectTabs(staticVariables.selectedTab);
            //  APIManager.instance.fetchAllBankDetail();
            //   APIManager.instance.fetchCryptoWallet();
        }
    }


    public GameObject loadingBg; //RAR





    public void crossAddBank()
    {
        BankDetailButton.onClick.Invoke();
    }


    public void togglecheckBox()
    {
        SoundManagerMain.instance.ClickSoundPlay();

        if (isSelected == 1)
        {
            isSelected = 0;
            checkboxImage.sprite = uncheckedSprites;

        }
        else
        {
            isSelected = 1;
            checkboxImage.sprite = checkboxSprite;
        }

    }
    public void togglecheckBoxCrypto()
    {
        SoundManagerMain.instance.ClickSoundPlay();
        if (isSelected == 1)
        {
            isSelected = 0;
            cryptoCheckbox.sprite = uncheckedSprites;

        }
        else
        {
            isSelected = 1;
            cryptoCheckbox.sprite = checkboxSprite;
        }

    }
    public void updateProfileBtn()
    {
        WWWForm form = new WWWForm();
        if (firstName.text.Trim() != null && firstName.text.Trim() != "")
        {
            form.AddField("first_name", firstName.text.Trim());
            staticVariables.UserProfiledata.user.first_name = firstName.text.Trim();

        }
        if (lastName.text.Trim() != null && lastName.text.Trim() != "")
        {
            form.AddField("last_name", lastName.text.Trim());
            staticVariables.UserProfiledata.user.last_name = lastName.text.Trim();
        }
        onUpdateName.Invoke();
        if (profileTextImage != null)
        {

            form.AddBinaryData("picture", profileTextImage.EncodeToJPG(), $"{staticVariables.UserProfiledata.user._id}.jpg");
        }

        ApiAndRoomManager._instance.UpdateProfile(form, (onSuccess) =>
        {
            UserModel userModel = JsonUtility.FromJson<UserModel>(onSuccess);

            if (userModel.status)
            {
                userModel.access_token = staticVariables.UserProfiledata.access_token;
                PlayerPrefs.SetString("userModel", JsonUtility.ToJson(userModel));
                staticVariables.UserProfiledata = userModel;
#if !UNITY_EDITOR
            AndroidUtility._ShowAndroidToastMessage("Updated Profile");
#else
                PopupMessageManager.instance.ShowPopUp("Your profile has been updated", "Updated Profile");
#endif
            }
            else
            {
#if !UNITY_EDITOR
            AndroidUtility._ShowAndroidToastMessage("userModel.message");
#else
                PopupMessageManager.instance.ShowPopUp(userModel.message);
#endif
            }
        });

        SoundManagerMain.instance.ClickSoundPlay();
        HomeMenuManager.instance.mainCanvasObject.SetActive(true);
    }



    public void onClosedBtn()
    {
        ConstantsData_M.Log("selected tab" + staticVariables.selectedTab);
        selectTabs(staticVariables.selectedTab);
        SoundManagerMain.instance.ClickSoundPlay();
    }



    public void ReturnToBackDashboard()
    {
        switch (staticVariables.selectedTab)
        {
            case "updatebank":
            case "addbank":
                staticVariables.selectedTab = "bank";
                if (staticVariables.shouldupdate)
                {
                    SceneLoaderUtility.LoadScene(SceneManager.GetActiveScene().buildIndex);

                }
                else
                {
                    selectTabs(staticVariables.selectedTab);
                }

                break;
            case "updatecrypto":
            case "addcrypto":
                staticVariables.selectedTab = "crypto";
                if (staticVariables.shouldupdate)
                {
                    SceneLoaderUtility.LoadScene(SceneManager.GetActiveScene().buildIndex);

                }
                else
                {
                    selectTabs(staticVariables.selectedTab);
                }

                break;


            default:
                {
                    SceneLoaderUtility.LoadScene("MainmenuScene");
                    break;
                }

                break;

        }


    }


    public void OnClickUploadImageButton()
    {
        SoundManagerMain.instance.ClickSoundPlay();
        string fileType = NativeFilePicker.ConvertExtensionToFileType("png,jpg,jpeg");
        NativeFilePicker.Permission permission1 = NativeFilePicker.PickFile((path) =>
        {

            if (path == null)
            {
                //Debug.Log("jjsjsjjs");
            }
            else
            {

                filePath = path;
                Texture2D texture = NativeGallery.LoadImageAtPath(path, 2073600, false);

                if (texture == null) return;
                profileImage.texture = texture;
                profileTextImage = texture;

                UIMainMenManager.instance.playerInfo.PlayerProfile_Image.texture = texture = texture;
                UIMainMenManager.instance.playerInfo.PlayerProfile_Image.texture = UIMainMenManager.instance ? texture : SpritesManager.Instance.spritesScriptable.nullProfileImg;
                staticVariables.ProfilePicture = texture;
                //           StartCoroutine(UploadImage(texture));
            }


        }, new string[]{
            "image/*"
        });



    }
    public void selectTabs(string selectedTabs)
    {
        SoundManagerMain.instance.ClickSoundPlay();
        switch (selectedTabs)
        {
            case "profile":

                profilePanel.SetActive(true);
                fetchAllBankAccountPanel.SetActive(false);
                fetchAllCryptoAccountPanel.SetActive(false);
                updateUIPanel.SetActive(false);
                addBankDetail.SetActive(false);
                addupdatecryptoWallet.SetActive(false);
                staticVariables.selectedTab = "profile";
                if (Borderspanel.logoHandler != null)
                {
                    Borderspanel.logoHandler(false);
                }

                break;
            case "updatePassword":
                profilePanel.SetActive(false);
                fetchAllBankAccountPanel.SetActive(false);
                fetchAllCryptoAccountPanel.SetActive(false);
                updateUIPanel.SetActive(true);
                addBankDetail.SetActive(false);
                addupdatecryptoWallet.SetActive(false);
                staticVariables.selectedTab = "updatePassword";
                break;

            default:
                profilePanel.SetActive(true);
                fetchAllBankAccountPanel.SetActive(false);
                fetchAllCryptoAccountPanel.SetActive(false);
                updateUIPanel.SetActive(false);
                addBankDetail.SetActive(false);
                staticVariables.selectedTab = "profile";
                break;

        }
    }

    public void updatePassword()
    {
        SoundManagerMain.instance.ClickSoundPlay();
        if (enterOldPassword.text.ToString().Trim() == "")
        {
            PopupMessageManager.instance.ShowPopUp("Please enter current password");
        }
        else if (enternewPassword.text.ToString().Trim() == "")
        {
            PopupMessageManager.instance.ShowPopUp("Please enter new password");
        }
        else if (enternewPassword2.text.ToString().Trim() == "")
        {
            PopupMessageManager.instance.ShowPopUp("Please confirm new password");
        }
        else if (enternewPassword.text.ToString().Trim() != enternewPassword2.text.ToString().Trim())
        {
            PopupMessageManager.instance.ShowPopUp("password must match");
        }
        else
        {
            WWWForm form = new WWWForm();
            form.AddField("oldpassword", enterOldPassword.text.ToString().Trim());
            form.AddField("newpassword", enternewPassword2.text.ToString().Trim());

            ApiAndRoomManager._instance.UpdatePassword(form, onsuccess =>
            {
                UserModel userModel = JsonUtility.FromJson<UserModel>(onsuccess);
                if (userModel.status)
                {
                    PopupMessageManager.instance.ShowPopUp("Your Password has updated", "Updated Password");
                }
                else
                {
                    PopupMessageManager.instance.ShowPopUp(userModel.message, "Updated Password");
                }

            });
        }

    }
    public void ResetForm()
    {
        // BANK
        bank_detail.UserBankInformation.text = string.Empty;

        // CRYPTO
        addupdateNetworkName.text = string.Empty;
        addupdateNetworkAddress.text = string.Empty;
        //  PASSWORD
        enterOldPassword.text = string.Empty;
        enternewPassword.text = string.Empty;
        enternewPassword2.text = string.Empty;
    }
    [Header("Banks Detail")]
    public Transform ContentBank;
    public GameObject bankObject;
    public string selectedBankName;
    public TextMeshProUGUI BankNameText;



}


[Serializable]
public class Single<T>
{
    public bool status;
    public T data;
    public string message;
}

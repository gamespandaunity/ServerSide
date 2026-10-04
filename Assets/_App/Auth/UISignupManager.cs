using DragonArts.Collection.Countries;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UISignupManager : MonoBehaviour
{
    [FormerlySerializedAs("firstNameTxt")] public TMP_InputField firstNameInput;
    [FormerlySerializedAs("lastNameTxt")] public TMP_InputField lastNameInput;
    [FormerlySerializedAs("countries")] public CountryGroup countryGroup;
    [FormerlySerializedAs("dropdown")] public TMP_Dropdown countryDropdown;

    [FormerlySerializedAs("phoneNumberTxt")] public TextMeshProUGUI phoneNumberText;
    [FormerlySerializedAs("emailAddressTxt")] public TextMeshProUGUI emailAddressText;
    [FormerlySerializedAs("passwordfieldTxt")] public TMP_InputField passwordInput;
    [FormerlySerializedAs("refferalCodeTxt")] public TextMeshProUGUI referralCodeText;

    [FormerlySerializedAs("countryNameTxt")] public Text countryNameText;
    [FormerlySerializedAs("dialingCodeTxt")] public Text dialingCodeText;

    [FormerlySerializedAs("signuploader")] public GameObject signupLoader;
    [FormerlySerializedAs("gameobjectErrors")] public GameObject errorPanel;
    [FormerlySerializedAs("errorsText")] public Text errorMessageText;

    [FormerlySerializedAs("OtpPanel")] public GameObject otpPanel;
    [FormerlySerializedAs("otpTxt")] public Text otpText;

    [FormerlySerializedAs("silverCoinsQty")] public TextMeshProUGUI silverCoinsText;
    [FormerlySerializedAs("goldenCoinsQty")] public TextMeshProUGUI goldenCoinsText;

    [FormerlySerializedAs("silverGoldenCoinsPanel")] public GameObject coinPanel;
    [FormerlySerializedAs("destroyTime")] public float autoDestroyTime = 5f;

    [Header("validate otp objs")]
    [FormerlySerializedAs("OtpinputField")] public TMP_InputField otpInputField;
    [FormerlySerializedAs("submitButton")] public Button otpSubmitButton;
    [FormerlySerializedAs("submitButton")] public Button registerButton;
    public GameObject RegisterAnimation;
    [FormerlySerializedAs("OtpValidationResponse")] public TextMeshProUGUI OtpValidationResponse;
    [FormerlySerializedAs("otpNo")] public TextMeshProUGUI otpNumberText;

    static string selectedDialCode = "";
    public static UISignupManager instance;

    string selectedCountry = "";
    private GameObject loaderInstance;
    private string savedOtp;
    CreateNewPlayer newPlayer;
    public OtpResponse otpResponseData;
    public CreateNewPlayer createPlayer;
    public Single<SignInReward> SignInReward;
    [Serializable]
    public struct rewardPanelSignUp
    {
        [FormerlySerializedAs("silverQty")] public Text silverCoinsQty;
        [FormerlySerializedAs("goldQty")] public Text goldCoinsQty;
    }
    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }

        otpInputField.onValueChanged.AddListener(enteredOtp =>
         {
             savedOtp = enteredOtp;
             //print("entered otp" + enteredOtp);
         });
    }


    private void Awake()
    {
        CountryDropDown(isfirstTime: true);
    }

    //public void CountryDropDown(bool isfirstTime)
    public void CountryDropDown(bool isfirstTime)
    {
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (Country c in countryGroup.list)
        { // Build Options List from Country Data

            options.Add(new TMP_Dropdown.OptionData($"<size=50><sprite name=\"{c.isoCode3}\"></size>{c.dialingCode}"));
        }

        countryDropdown.options = options;

        if (isfirstTime)
        {
            countryDropdown.value = 130;
        }


        Country t = countryGroup.list[countryDropdown.value];

        countryNameText.text = t.name.ToLower();
        dialingCodeText.text = t.dialingCode;
        selectedCountry = t.name.ToLower();
        selectedDialCode = t.dialingCode.Substring(1);
    }
    //   public void RegisterBtn()
    public void OnRegisterButtonClicked()
    {
        registerButton.gameObject.SetActive(false);
        RegisterAnimation.SetActive(true);
        string firstName = firstNameInput.text.Trim().Trim((char)8203);
        string lastName = lastNameInput.text.Trim().Trim((char)8203);

        string phoneNumber = selectedDialCode + phoneNumberText.text.Trim().Trim((char)8203);

        string emaillAddress = emailAddressText.text.Trim().Trim((char)8203);


        string passwordTxt = passwordInput.text.Trim().Trim((char)8203);

        string systemId = SystemInfo.deviceUniqueIdentifier;
        Dictionary<string, string> newplayerData = new Dictionary<string, string>();

        newplayerData["first_name"] = firstName;
        newplayerData["last_name"] = lastName;
        newplayerData["country"] = selectedCountry;
        newplayerData["email"] = emaillAddress;
        newplayerData["phone"] = phoneNumber;
        newplayerData["password"] = passwordTxt;
        newplayerData["device_id"] = systemId;

        if (referralCodeText.text != null || referralCodeText.text != "")
        {
            staticVariables.referralCode = referralCodeText.text;
            newplayerData["referral_code"] = staticVariables.referralCode;
        }

        if (firstName == null || firstName == "")
        {
            //gameobjectErrors.SetActive(true);
            //errorsText.text = "First Name field can not be empty.";
            ApiAndRoomManager._instance.DisplayError("First Name field can not be empty");
            registerButton.gameObject.SetActive(true);
            RegisterAnimation.SetActive(false);

        }
        else if (lastName == null || lastName == "")
        {
            //gameobjectErrors.SetActive(true);
            //errorsText.text = "Last Name field can not be empty.";
            ApiAndRoomManager._instance.DisplayError("Last Name field can not be empty");
            registerButton.gameObject.SetActive(true);
            RegisterAnimation.SetActive(false);
        }
        else if (phoneNumber == null || phoneNumber == "")
        {
            //gameobjectErrors.SetActive(true);
            //errorsText.text = "Phone number field can not be empty.";
            ApiAndRoomManager._instance.DisplayError("Phone number field can not be empty");
            registerButton.gameObject.SetActive(true);
            RegisterAnimation.SetActive(false);
        }
        else if (string.IsNullOrEmpty(emaillAddress) && !IsEmailAddressValid(emaillAddress))
        {
            //gameobjectErrors.SetActive(true);
            //errorsText.text = "please enter valid email address.";
            ApiAndRoomManager._instance.DisplayError("please enter valid email address");
            registerButton.gameObject.SetActive(true);
            RegisterAnimation.SetActive(false);
        }
        else if (passwordTxt == null || passwordTxt == "")
        {
            //gameobjectErrors.SetActive(true);
            //errorsText.text = "Password field can not be empty.";
            ApiAndRoomManager._instance.DisplayError("Password field can not be empty");
            registerButton.gameObject.SetActive(true);
            RegisterAnimation.SetActive(false);
        }
        else
        {
            //loaderInstantiated = Instantiate(signuploader, registerButton.transform);
            ApiAndRoomManager._instance.RegisterPlayer(newplayerData, succeess =>
            {
                createPlayer = JsonUtility.FromJson<CreateNewPlayer>(succeess);
                newPlayer = createPlayer;
                //print("    " + createPlayer._id);
                LoginUIManager.temporaryPhoneNumber = phoneNumberText.text;
                LoginUIManager.temporaryPassword = passwordTxt;
                if (createPlayer.status)
                {

                    otpNumberText.text = "YOUR OTP CODE IS :" + staticVariables.otp;
                                     staticVariables.createPlayer_response_id = createPlayer._id.ToString();
                    otpPanel.SetActive(true);
                    registerButton.gameObject.SetActive(true);
                    RegisterAnimation.SetActive(false);
                }
                else
                {
                    if (createPlayer.player_status.Contains("in_active"))
                    {
                        ApiAndRoomManager._instance.CreateOtp(phoneNumber, onSuccess =>
                        {
                            otpResponseData = JsonUtility.FromJson<OtpResponse>(onSuccess);
                            if (otpResponseData.Status)
                            {
                                otpNumberText.text = "YOUR OTP CODE IS :" + staticVariables.otp;
                                staticVariables.createPlayer_response_id = otpResponseData.Id.ToString();
                                otpPanel.SetActive(true);
                            }
                            else
                            {
                                PopupMessageManager.instance.ShowPopUp("Couldn't generate opt, Please try again later");

                            }

                        }, OnFailed =>
                        {
                            PopupMessageManager.instance.ShowPopUp("Couldn't generate opt, Please try again later");
                        });
                        registerButton.gameObject.SetActive(true);
                        RegisterAnimation.SetActive(false);
                    }
                    else
                    {
                        ApiAndRoomManager._instance.DisplayError(createPlayer.message);
                        registerButton.gameObject.SetActive(true);
                        RegisterAnimation.SetActive(false);
                    }
                    //  signuploader.gameObject.SetActive(false);

                }

            },
            onFailed =>
            {
                registerButton.gameObject.SetActive(true);
                RegisterAnimation.SetActive(false);
            });

        }
        //on submit validate api will call




    }
    //   public static bool IsEmailValid(string emailAddress)
    public static bool IsEmailAddressValid(string emailAddress)
    {
        // Define a regular expression pattern for a valid email address
        string pattern = @"^[\w\.-]+@[\w\.-]+\.\w+$";

        // Use the Regex.IsMatch method to check if the email matches the pattern
        return Regex.IsMatch(emailAddress, pattern);
    }
    //  public void closeErrorPanel()
    public void CloseErrorPanel()
    {
        errorPanel.SetActive(false);

    }
    //  public void closeOtpPanel()
    public void CloseOtpPanel()
    {
        otpPanel.SetActive(false);
        registerButton.gameObject.SetActive(true);
        RegisterAnimation.SetActive(false);
    }
    // public void verifyOtp()
    public void VerifyOtp()
    {
        ApiAndRoomManager._instance.VerifyOtp(int.Parse(otpInputField.text), staticVariables.createPlayer_response_id, onSuccess =>
            {
                SignInReward = JsonUtility.FromJson<Single<SignInReward>>(onSuccess);
                //   loaderInstantiated = Instantiate(signuploader, submitButton.transform);
                otpPanel.gameObject.SetActive(false);
                //submitButton.gameObject.SetActive(false);
                coinPanel.SetActive(true);
                silverCoinsText.text = SignInReward.data.SilverBalance.ToString();
                goldenCoinsText.text = SignInReward.data.GoldBalance.ToString();
                //OtpValidationResponse.text = " PLAYER REGISTERED SUCCESS!";

                PopupMessageManager.instance.ShowPopUp("Registration Succeed!", "CONGRATULATION", timeToShow: 1, () =>
                {
                    if (LoginUIManager.temporaryPhoneNumber != "" && LoginUIManager.temporaryPassword != "")
                    {
                        LoginUIManager.instance.LoginDirectly();
                    }
                });
            },
        OnFailed =>
        {
        });

    }
    //  IEnumerator CloseSignUp()
    IEnumerator CloseSignUpCoroutine()
    {
        yield return new WaitForSeconds(1.8f);//time to set later
        Destroy(loaderInstance);
        LoginUIManager.instance.signUpPanel.SetActive(false);
    }
    //  public void moveregistrationScreento()
    public void MoveToRegistrationScreen()
    {
        coinPanel.SetActive(false);
        this.transform.parent.gameObject.SetActive(false);
        StartCoroutine(CloseSignUpCoroutine());

    }
    //   public void loginLoadScene()
    public void LoadLoginScene()
    {
        coinPanel.SetActive(false);
        SceneLoaderUtility.LoadScene("LoginScene");

    }
}
[System.Serializable]
public class SignInReward
{
    public int SilverBalance;
    public int GoldBalance;
    public string Message;
}
public class OtpResponse
{
    public bool Status;
    public int Otp;
    public int Id;
}

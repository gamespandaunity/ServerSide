using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DragonArts.Collection.Countries;
//using Google;
using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using Facebook.Unity;
using System.Text;
using UnityEngine.Networking;
using UnityEngine.Serialization;


[Serializable]
public struct Otp_Panel
{
    public Text headingText, eneteredOtpNo_text;
    public GameObject inputField, submitButton;

}
[Serializable]
public struct NewPassword_Panel
{

    public GameObject[] toCloseObjs;
    public TextMeshProUGUI ResponseTxt;

}
public class LoginUIManager : MonoBehaviour
{
    public static LoginUIManager instance;

    [FormerlySerializedAs("NewPasswordContainers")] public NewPassword_Panel newPasswordPanel;
    [FormerlySerializedAs("loginLoader")] public GameObject loginLoadingIndicator;
    [FormerlySerializedAs("otpNotification")] public GameObject  otpNotificationPanel;
    [FormerlySerializedAs("errorDetecton")] public GameObject errorNotificationPanel;
    [FormerlySerializedAs("LoginMainPanel")] public GameObject loginMainPanel;
    [FormerlySerializedAs("phoneNumberTxt")] public TMP_InputField phoneNumberInputField;
    [FormerlySerializedAs("countries")] public CountryGroup countryGroup;
    [FormerlySerializedAs("passwordtxt")] public TMP_InputField passwordInputField;
    [FormerlySerializedAs("dropdown")] public TMP_Dropdown countryDropdown;
    [FormerlySerializedAs("dropdownphoneverification")] public TMP_Dropdown phoneVerificationDropdown;
    [FormerlySerializedAs("forgetPasswordPhoneNumber")] public TextMeshProUGUI forgetPasswordPhoneNumberText;
    [FormerlySerializedAs("otpNotificationText")] public TextMeshProUGUI  otpNotificationMessage;
    [FormerlySerializedAs("countryNameTxt")] public Text countryNameText;
    [FormerlySerializedAs("dialingCodeTxt")] public Text dialingCodeText;
    [FormerlySerializedAs("errorTxt")] public Text errorMessageText;

    [Header("reset password things")]
    [FormerlySerializedAs("forgetPassword_parent_obj")] public GameObject forgetPasswordParentObject;
    [FormerlySerializedAs("OtpPanel")] public GameObject otpPanel;
    [FormerlySerializedAs("verifyotpbtn")] public Button verifyOtpButton;
    [FormerlySerializedAs("OTP_")] public Otp_Panel otpPanelScript;
    [FormerlySerializedAs("otpNum")] public TMP_InputField otpInputField;

    [FormerlySerializedAs("ResetPasswordPanel")] public GameObject resetPasswordPanel;
    [FormerlySerializedAs("reSetPassword")] public TMP_InputField newPasswordInputField;
    [FormerlySerializedAs("reTypeRestPassword")] public TMP_InputField confirmNewPasswordInputField;

    [FormerlySerializedAs("Error1")] public TMP_Text resetPasswordErrorText;
    [FormerlySerializedAs("Error2")] public TMP_Text generalErrorText;
    [FormerlySerializedAs("Error3")] public TMP_Text invalidOtpErrorText;
    [FormerlySerializedAs("loader")] public GameObject loadingIndicator;
    [FormerlySerializedAs("duplicatelogin")] public GameObject duplicateLoginErrorPanel;
    [FormerlySerializedAs("signUpPanel")] public GameObject  signUpPanel;

    [Header("Phone Verification ")]
    [FormerlySerializedAs("PhoneNumberVerificaionpanel")] public GameObject phoneVerificationPanel;
    [FormerlySerializedAs("phoneNumberTxtverify")] public TextMeshProUGUI phoneNumberTextForVerification;
    [FormerlySerializedAs("ShowPasswordTogle")] public Toggle showPasswordToggle;
    [FormerlySerializedAs("showPassword")] public Sprite passwordVisibleIcon;
    [FormerlySerializedAs("dontShowPassword")] public Sprite  passwordHiddenIcon;
    [FormerlySerializedAs("TempNo")] public static string temporaryPhoneNumber;
    [FormerlySerializedAs("TempPass")] public static string temporaryPassword;
    [FormerlySerializedAs("socialType")] public string socialLoginType;

    [Header("VERIFICATION password things")]
    [FormerlySerializedAs("verificationPanel")] public GameObject passwordVerificationPanel;

    public GameObject loginButton,loginAnimation;

    string enteredPhoneNumber;

    private Image showPasswordIcon;

    private bool isGoogleLogin;
    //private GoogleSignInConfiguration googleSignInConfig;
    string dialingCode;

    private InputField enterOtpInput;
    private string enteredOtp;
    private string  forgetOtp;

    //bool OnGoogleLogin = false;
    //GoogleSignInUser googleUser = null;
    int userId;
    double otpForPasswordReset;
    string resetToken;
   [FormerlySerializedAs("isSocialOtp")] public bool isSocialOtp;
    public bool sound;

    private void Awake()
    {
        PopulateCountryDropdown(isfirstTime: true);
        PopulatePhoneVerificationDropdown(true);
    }
    private void Start()
    {
        if (Screen.orientation == ScreenOrientation.Portrait)
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
        if (instance == null)
        {
            instance = this;
        }
        //googleSignInConfig = new GoogleSignInConfiguration
        //{
        //    // Replace with your Web client ID from the Firebase Console
        //    WebClientId = "900160237865-6vl41lpmudiblkrt6n1fc56267tevefi.apps.googleusercontent.com",
        //    RequestIdToken = true
        //};
        showPasswordToggle.onValueChanged.AddListener(OnToggleValueChanged);
        showPasswordIcon = showPasswordToggle.gameObject.GetComponent<Image>();
        
        FB.Init(this.OnInitializationComplete, this.OnHideUnity);
        StoreSoundPreference();
        //APIManager.instance.SocialAuthLogin("123654", "", "", "facebook");
        enterOtpInput = otpPanelScript.inputField.GetComponent<InputField>();
        enterOtpInput.onValueChanged.AddListener(OnOtpFieldChanged);
        otpInputField.onEndEdit.AddListener((otp) =>
        {
            forgetOtp = otp; ConstantsData_M.Log("forget orp otpfor");


        });
    }
    private void Update()
    {
        //if (OnGoogleLogin)
        //{
        //    OnGoogleLogin = false;
        //    isGoogleLogin = true;
        //    if (googleUser != null)
        //    {
        //        string userId = googleUser.UserId;
        //        string userEmail = googleUser.Email;
        //        string userName = googleUser.DisplayName;
            
        //        Dictionary<string, string> socialLoginparam = new Dictionary<string, string>();
        //        socialLoginparam["id"] = googleUser.IdToken;
        //        socialLoginparam["social_type"] = "google";
        //        socialLoginType = "google";
        //        ApiAndRoomManager._instance.SocialLogin(socialLoginparam, HandleSocialLoginSuccess);

        //    }
        //}
    }
   // public void ToggleValueChange(bool isOn)
    public void OnToggleValueChanged(bool isOn)
    {

        if (isOn)
        {
            passwordInputField.contentType = TMP_InputField.ContentType.Standard;
            showPasswordIcon.sprite = passwordVisibleIcon;
            RefreshInputFieldDisplay();
        }
        else
        {
            passwordInputField.contentType = TMP_InputField.ContentType.Password;
            showPasswordIcon.sprite = passwordHiddenIcon;
            RefreshInputFieldDisplay();
        }
    }
    // private void UpdateInputFieldDisplay()
    private void RefreshInputFieldDisplay()
    {
        string currentText = passwordInputField.text;
        passwordInputField.text = "";
        passwordInputField.text = currentText;
    }
    // public void SaveSoundValue()
    public void StoreSoundPreference()
    {
        if (PlayerPrefs.GetInt("Sound") == 1)
        {
            SoundManagerMain.instance.clickSound.mute = false;
            sound = true;

        }
        else
        {
            SoundManagerMain.instance.clickSound.mute = true;
            sound = false;
        }
    }
    // public void CountryDropDown(bool isfirstTime)
    public void PopulateCountryDropdown(bool isfirstTime)
    {
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (Country c in countryGroup.list)
        { // Build Options List from Country Data

            options.Add(new TMP_Dropdown.OptionData($"<size=125><sprite name=\"{c.isoCode3}\"></size>{c.dialingCode}"));
        }

        countryDropdown.options = options;

        if (isfirstTime)
        {
            countryDropdown.value = 130;
        }


        Country country = countryGroup.list[countryDropdown.value];

        countryNameText.text = country.name;
        dialingCodeText.text = country.dialingCode;



    }
    // public void CountryDropDownPhoneVerification(bool isfirstTime)
    public void PopulatePhoneVerificationDropdown(bool isfirstTime)
    {
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (Country c in countryGroup.list)
        { // Build Options List from Country Data
            options.Add(new TMP_Dropdown.OptionData($"<size=125><sprite name=\"{c.isoCode3}\"></size>{c.dialingCode}"));
        }
        phoneVerificationDropdown.options = options;

        if (isfirstTime)
        {
            phoneVerificationDropdown.value = 130;
        }


        Country country = countryGroup.list[phoneVerificationDropdown.value];

        countryNameText.text = country.name;
        dialingCodeText.text = country.dialingCode;



    }


    // public void OTP_Field_OnValueChanged(string enteredOtp)
    public void OnOtpFieldChanged(string enteredOtp)
    {
        this.enteredOtp = enteredOtp;
    }

    //public void directLogin()
    public void LoginDirectly()
    {
        phoneNumberInputField.text = temporaryPhoneNumber;
        passwordInputField.text = temporaryPassword;
        OnLoginButtonClicked();
    }
    // public void LoginBtn()
    public void OnLoginButtonClicked()
    {
        staticVariables.isGuest = false;
        if (phoneNumberInputField == null || phoneNumberInputField.text.Trim((char)8203) == "")
        {
           
            ApiAndRoomManager._instance.DisplayError("Phone Number can not be empty");

        }
        else if (passwordInputField.text == null || passwordInputField.text == "")
        {
          
            ApiAndRoomManager._instance.DisplayError("Password can not be empty");

        }
        else
        {
            
            PopulateCountryDropdown(isfirstTime: false);
            StartCoroutine(PostLoginDataRoutine());

            SoundManagerMain.instance.ClickSoundPlay();

        }
    }
    // public void YesupdateImeUnique()
    public void UpdateSessionTimestamp()
    {
        duplicateLoginErrorPanel.SetActive(false);
        string userModeljson = JsonUtility.ToJson(staticVariables.UserProfiledata);
        PlayerPrefs.SetString("userModel", userModeljson);
        ApiAndRoomManager._instance.updatedeviceuniqueIdentifier();
        //    Constants_M.Log("1");

    }
    //  public IEnumerator LoginPostData_Coroutine()
    public IEnumerator PostLoginDataRoutine()
    {
        string phoneNumber = dialingCodeText.text.Replace("+", "").Replace("-", "").Trim().Trim((char)8203) +
                             instance.phoneNumberInputField.text.Trim((char)8203);

        if (loginLoadingIndicator != null)
        {
            //loginLoadingIndicator.SetActive(true);
        }
        else
        {
            loginMainPanel.SetActive(true);
            ConstantsData_M.Log("LoginLoader is null.");
            yield break;
        }
        loginButton.SetActive(false);
        loginAnimation.SetActive(true);

        string uri =ServerConnection.Main_URL() + "/auth/login/phone";  // post uri
        LoginModel loginModel = new LoginModel(phoneNumber, passwordInputField.text.Trim().Trim((char)8203), SystemInfo.deviceUniqueIdentifier);

        string json = JsonUtility.ToJson(loginModel);
        var bytes = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(uri, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bytes);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("accept", "*/*");
            request.SetRequestHeader("apk_signature_black_arch", ServerConnection.secrets["apk_signature_black_arch"]);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (loginLoadingIndicator != null)
            {
                loginLoadingIndicator.SetActive(false);
            }
            else
            {
                loginMainPanel.SetActive(true);
                ConstantsData_M.Log("LoginLoader is null.");
                yield break;
            }

            if (request.isNetworkError || request.isHttpError)
            {
                loginMainPanel.SetActive(true);
                //ShowError(request.downloadHandler.text);//Mohsin
                loginButton.SetActive(true);
                loginAnimation.SetActive(false);
                PopupMessageManager.instance.ShowPopUp("Could not process your request.. please try again later");
            }
            else
            {
                UserModel userModel = JsonUtility.FromJson<UserModel>(request.downloadHandler.text);

                if (userModel != null && userModel.status)
                {
                    PlayerPrefs.SetString("userModel", request.downloadHandler.text);
                    ConstantsData_M.Log("Adding Data login :" + request.downloadHandler.text);
                    if (ApiAndRoomManager._instance.IsFirstTime == 0)
                    {
                        ApiAndRoomManager._instance.IsFirstTime = 1;

                      
                    }

                    ApiAndRoomManager.LastFetchedCoins.data.gold_balance = userModel.user.gold_balance.ToString();
                    ApiAndRoomManager.LastFetchedCoins.data.silver_balance = userModel.user.silver_balance.ToString();
                   staticVariables.UserProfiledata = userModel;
                    ConstantsData_M.Log("Status :" + staticVariables.UserProfiledata.status);
                    ConstantsData_M.Log("Status :" + staticVariables.UserProfiledata.user._id);
                    if (string.IsNullOrEmpty(userModel.user.user_login_token) || userModel.user.user_login_token == staticVariables.uniqueGameIdentifier)
                    {
                        loginMainPanel.SetActive(true);
                        SceneLoaderUtility.LoadScene("Home");
                    }
                    else
                    {
                        loginMainPanel.SetActive(true);
                        if (duplicateLoginErrorPanel != null)
                        {
                            duplicateLoginErrorPanel.SetActive(true);
                        }
                        else
                        {
                            ConstantsData_M.Log("DuplicateLogin is null.");
                        }
                    }
                    loginButton.SetActive(true);
                    loginAnimation.SetActive(false);
                }
                else
                {
                    PopupMessageManager.instance.ShowPopUp(userModel.message);
                    ConstantsData_M.Log("USER MODEL MESSAGE " + userModel.message);
                    loginMainPanel.SetActive(true);   
                    loginButton.SetActive(true);
                    loginAnimation.SetActive(false);
                }
            }
        }
    }

    // public void LoginAsGuestBtn()
    public void OnGuestLoginButtonClicked()
    {
        staticVariables.isGuest = true;
        if (GuestDataManager.FetchGuestCoins() < 5000)
        {
            GuestDataManager._instance.UpdateGuestCoins((5000- GuestDataManager.FetchGuestCoins()));
        }
        GuestDataManager._instance.UpdateGuestName(ConstantsData_M.GetRandomGuestName());
        PlayerPrefs.SetString("GuestId", Application.identifier);
        SceneManager.LoadSceneAsync("Home");
    }
    //  public void SendOTPBtn()
    public void OnSendOtpButtonClicked()
    {
        isSocialOtp=false;
        if (forgetPasswordPhoneNumberText.text == null || forgetPasswordPhoneNumberText.text.Trim((char)8203) == "")
        {
            ApiAndRoomManager._instance.DisplayError("Phone Number can not be empty");
        }
        else
        {

            string newCode = staticVariables.countryDialCode_general.Substring(1);
            enteredPhoneNumber = newCode + forgetPasswordPhoneNumberText.text;
            Dictionary<string, string> jsonData = new Dictionary<string, string>();
            jsonData["phone_no"] = enteredPhoneNumber;


            InitiatePasswordReset(forgetPasswordPhoneNumberText.text, jsonData, onsuccess =>
            {

                JObject SerializedString = JObject.Parse(onsuccess);
                bool status = SerializedString["status"].Value<bool>();
                if (status)
                {
                    userId = (int)SerializedString["_id"];
                    //otpResetNew = (double)SerializedString["data"]["OTP"];


                    otpPanel.SetActive(true);
                    //otpNotification.SetActive(true);

                    otpNotificationMessage.text = otpForPasswordReset.ToString();
                    otpPanelScript.eneteredOtpNo_text.text = "VERIFY OTP";
                    forgetPasswordParentObject.SetActive(false);
                    otpPanelScript.headingText.text = " VERIFY OTP ";
                    staticVariables.createPlayer_response_id = userId.ToString();


                }
                else
                {
                    //HANDLE EXCEPTIONS HERE
                }
            });
            //APIManager.instance.GenereateOtp(enteredPhoneNO, onSuccess =>
            //{
            //    GENERATE_OTP_RESPONSE response = JsonUtility.FromJson<GENERATE_OTP_RESPONSE>(onSuccess);


            //    if (response.status)
            //    {
            //        OtpPanel.SetActive(true);
            //        OTP_.eneteredOtpNo_text.text = response.otp.ToString();
            //        forgetPassword_parent_obj.SetActive(false);
            //        OTP_.headingText.text = " OTP CODE : " + response.otp;
            //        staticVariables.createPlayer_response_id = response._id.ToString();
            //        staticVariables.otp = response.otp;
            //        Constants_M.Log(" OTP , ID" + staticVariables.createPlayer_response_id + "   " + staticVariables.otp);
            //    }
            //    else
            //    {
            //        APIManager.instance.ShowError("ERROR OCCCURED! PLEASE ENTER CORRECT INFO");
            //    }
            //    // apply  validate otp api here

            //});

            SoundManagerMain.instance.ClickSoundPlay();

        }

    }
    // private void RequestResetPassword(string numOrEmail,Dictionary<string, string> data, Action<string> onsuccess = null, Action<string> Failed = null)
    private void InitiatePasswordReset(string numOrEmail,Dictionary<string, string> data, Action<string> onsuccess = null, Action<string> Failed = null)
    {
        StartCoroutine(ServerConnection.PostApiRequest(ServerConnection.requestReset_otpGenerator(),data, onsuccess, Failed));
    }
    // public void SubmittOTPBtn()
    public void OnSubmitOtpButtonClicked()
    {
        otpNotificationMessage.text = otpForPasswordReset.ToString();
        if (otpInputField == null || otpInputField.text == "")
        {
            ApiAndRoomManager._instance.DisplayError("OTP can not be null");
        }

        else
        {
            {
                otpNotificationPanel.SetActive(false);
                otpNotificationMessage.text = "";

                string url = ServerConnection.ValidateNewOtp() + "/" + userId + "/" + otpInputField.text;
                ValidateOtpResetToken(url, onSuccess =>
                {
                    JObject serializedData = JObject.Parse(onSuccess);
                    bool status = serializedData["status"].Value<bool>();
                    if (status)
                    {
                        resetToken = serializedData["reset_token"].Value<string>();
                        int _id = serializedData["_id"].Value<int>();
                        string message = serializedData["message"].Value<string>();


                        otpPanel.SetActive(false);
                        resetPasswordPanel.SetActive(false);
                        passwordVerificationPanel.SetActive(true);
                        otpInputField.text = "";
                        otpInputField.text = string.Empty;
                    }
                    else
                    {
                        ApiAndRoomManager._instance.DisplayError("UNABLE TO VALIDATE OTP");
                    }
                });

              
            }

        }
        SoundManagerMain.instance.ClickSoundPlay();
    }
    // public void ValidateOtpResetToken(string url, Action<string> onSuccess = null, Action<string> onFailure = null)
    public void ValidateOtpResetToken(string url, Action<string> onSuccess = null, Action<string> onFailure = null)
    {
        StartCoroutine(ServerConnection.GetApiRequest(url, onSuccess, onFailure));
    }
    // public void SubmittOTPBtnSocial()
    public void SubmitSocialOtp()
    {
        if (isSocialOtp)
        {
            staticVariables.createPlayer_response_id.Show("aagy id");
            if (otpInputField == null || otpInputField.text == "")
            {
                generalErrorText.text = "OTP can not be null";
                ConstantsData_M.Log("otp null");
            }
            else
            {
                ApiAndRoomManager._instance.VerifyOtp(staticVariables.otp, staticVariables.createPlayer_response_id, onSuccess =>
                {
                    ConstantsData_M.Log("validate success");
                    otpPanel.SetActive(false);
                    phoneVerificationPanel.SetActive(false);
                    otpInputField.text = "";
                    otpInputField.text = string.Empty;
                    if (isGoogleLogin)
                    {
                        //OnGoogleSignInClicked();
                    }
                    else
                    {
                        CallFBLogin();
                    }
                },
                OnFailed =>
                {

                });
            }
        }
        else
        {
            OnSubmitOtpButtonClicked();
        }
    }
    //  public void SubmitRestPasswordBtn()
    public void SubmitResetPassword()
    {
        if (string.IsNullOrEmpty(newPasswordInputField.text) && string.IsNullOrEmpty(confirmNewPasswordInputField.text))
        {
            ApiAndRoomManager._instance.DisplayError("Password can not be Null");
        }
        else if (newPasswordInputField.text == confirmNewPasswordInputField.text && confirmNewPasswordInputField.text != null && newPasswordInputField.text != null)
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data["_id"] = userId.ToString();
            data["reset_token"] = resetToken;
            data["new_password"] = newPasswordInputField.text;

            UpdatePasswordMemory(data, onSuccess =>
            {
                JObject serializedStr = JObject.Parse(onSuccess);
                bool status = serializedStr["status"].Value<bool>();
                string message = serializedStr["message"].Value<string>();
                if (status)
                {
                    foreach (GameObject gb in newPasswordPanel.toCloseObjs)
                    {
                        gb.SetActive(false);
                    }
                    newPasswordPanel.ResponseTxt.text = message;
                    StartCoroutine(CloseResetVerificationPanel());
                   
                }
                else
                {

                    ApiAndRoomManager._instance.DisplayError(message);
                }
            });
        }
        else
        {
            ApiAndRoomManager._instance.DisplayError("Password not match");

        }
        SoundManagerMain.instance.ClickSoundPlay();
    }
    //  private void NewUpdatePassword(Dictionary<string, string> jsonData, Action<string> onSuccess = null, Action<string> Failed = null)
    private void UpdatePasswordMemory(Dictionary<string, string> jsonData, Action<string> onSuccess = null, Action<string> Failed = null)
    {
        StartCoroutine(ServerConnection.PostApiRequest(ServerConnection.UpdatePasswordNewApi(), jsonData, onSuccess, Failed));
    }
    // IEnumerator DestroyResetVerificationPanel()
    IEnumerator CloseResetVerificationPanel()
    {
        yield return new WaitForSeconds(2f);
        newPasswordInputField.text = "";
        confirmNewPasswordInputField.text = "";
        newPasswordInputField.text = string.Empty;
        confirmNewPasswordInputField.text = string.Empty;
        resetPasswordPanel.SetActive(false);
    }
    // public void RegisterBtn()
    public void OnRegisterButtonClicked()
    {
        signUpPanel.SetActive(true);
        SoundManagerMain.instance.ClickSoundPlay();
    }
    // public void closeErrorPanel()
    public void DismissErrorPanel()
    {
        loginMainPanel.SetActive(true);
        errorNotificationPanel.SetActive(false);
        SoundManagerMain.instance.ClickSoundPlay();

    }
    // public void SignInWithGoogleBtn()
//    public void OnGoogleSignInClicked()
//    {
//#if UNITY_EDITOR
//        Dictionary<string, string> socialLoginparam = new Dictionary<string, string>();
//        socialLoginparam["id"] = "1060929245623116489768";
//        socialLoginparam["social_type"] = "google";
//        socialLoginparam["name"] = "dadc";
//        socialLoginparam["email"] = "dadc@g.c";
//        ApiAndRoomManager._instance.SocialLogin(socialLoginparam, HandleSocialLoginSuccess);
//#else
//        GoogleSignIn.Configuration = googleSignInConfig;
//        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnAuthenticationCompleted);
//        //if (!configuredone)
//        //{
//        //    configuration = new GoogleSignInConfiguration
//        //    {
//        //        WebClientId = "900160237865-6vl41lpmudiblkrt6n1fc56267tevefi.apps.googleusercontent.com", // Replace with your actual Web Client ID
//        //        RequestIdToken = true,
//        //        RequestEmail = true
//        //    };
//        //    GoogleSignIn.Configuration = configuration;

//        //    configuredone = true;
//        //}
//        //SoundManagerMain.instance.ClickSoundPlay();
//        //// Initialize Google Sign-In

//        //// Optionally, sign in silently
//        //GoogleSignIn.DefaultInstance.SignIn().ContinueWithOnMainThread(OnAuthenticationFinished);
//#endif
//    }
//    //  private void OnAuthenticationFinished(System.Threading.Tasks.Task<GoogleSignInUser> task)
//    //private void OnAuthenticationCompleted(System.Threading.Tasks.Task<GoogleSignInUser> task)
//    //{
//    //    if (task.IsFaulted)
//    //    {
//    //        using (IEnumerator<System.Exception> enumerator = task.Exception.InnerExceptions.GetEnumerator())
//    //        {
//    //            if (enumerator.MoveNext())
//    //            {
//    //                GoogleSignIn.SignInException error = (GoogleSignIn.SignInException)enumerator.Current;
//    //                ConstantsData_M.Log("Google Sign-In failed: " + error.Status);
//    //            }
//    //            else
//    //            {
//    //                ConstantsData_M.Log("Google Sign-In failed: " + task.Exception);
//    //            }
//    //        }
//    //    }
//    //    else if (task.IsCanceled)
//    //    {
//    //        ConstantsData_M.Log("Google Sign-In was canceled.");
//    //    }
//    //    else
//    //    {
//    //        // Get user data from the task
//    //        googleUser = task.Result;
//    //        OnGoogleLogin = true;
//    //        staticVariables.createPlayer_response_id = task.Result.UserId;
//    //        string idToken = task.Result.IdToken;
//    //        ConstantsData_M.Log($"Google ID Token: _{googleUser} :" + idToken);
//    //    }
//    //}

    // public void SendSocialOtp()
    public void DispatchSocialOtp()
    {
        isSocialOtp=true;
        PopulatePhoneVerificationDropdown(false);
        Dictionary<string, string> socialsignupparam = new Dictionary<string, string>();
        socialsignupparam["id"] = staticVariables.createPlayer_response_id.ToString();
        socialsignupparam["phone"] = dialingCodeText.text.Replace("+", "").Replace("-", "").Trim().Trim((char)8203) + phoneNumberTextForVerification.text.Trim((char)8203); ;
        socialsignupparam["country"] = countryNameText.text;
        StartCoroutine(ServerConnection.PostApiRequest(ServerConnection.Social_Otp_Generate(), socialsignupparam, OnSucess =>
        {
            OTPSendResponse Model = JsonUtility.FromJson<OTPSendResponse>(OnSucess);
            //Debug.Log(JsonUtility.ToJson(Model));
            if (Model.status)
            {
                Model.data._id.Show("ID");
                staticVariables.createPlayer_response_id = Model.data._id.ToString();
                userId = Model.data._id;
                otpPanelScript.headingText.text = " OTP CODE : " + Model.data.OTP;
                staticVariables.otp = Model.data.OTP;

                otpPanel.SetActive(true);

                verifyOtpButton.onClick.RemoveAllListeners();
                verifyOtpButton.onClick.AddListener(SubmitSocialOtp);
                //     //Debug.Log("user Created");
            }
            else
            {
                //  //Debug.Log("already exist");
            }

        }, OnFailed =>
        {

        }));
        SoundManagerMain.instance.ClickSoundPlay();
    }
    //  public void NoUpdateupdateImeUnique()
    public void NoUpdateupdateImeUnique()
    {
        duplicateLoginErrorPanel.SetActive(false);
    }
    // public void CallFBLogin()
    public void CallFBLogin()
    {
        FB.LogInWithReadPermissions(new List<string>() { "public_profile", "email" ,"user_friends"}, this.HandleLoginResult);
    }
    //  protected void HandleResult(IResult result)
    protected void HandleLoginResult(IResult result)
    {
        if (result == null)
        {
            return;
        }
        // Some platforms return the empty string instead of null.
        if (!string.IsNullOrEmpty(result.Error))
        {
        }
        else if (result.Cancelled)
        {
        }
        else if (!string.IsNullOrEmpty(result.RawResult))
        {
            var fbData = JsonUtility.FromJson<FBLoginResult>(result.RawResult);
            var fbLoginResult = JsonUtility.FromJson<FBLoginResult>(result.RawResult);
            string accessToken = fbLoginResult.access_token;
            accessToken.Show("fb access token");
            socialLoginType = "facebook";
            StartCoroutine(RetrieveUserDetails(accessToken));
        }
    }
    //  private IEnumerator FetchUserDetails(string accessToken)
    private IEnumerator RetrieveUserDetails(string accessToken)
    {
        string url = "https://graph.facebook.com/me?fields=id,name,email,location&access_token=" + accessToken;
        var www = new UnityEngine.Networking.UnityWebRequest(url);
        www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        yield return www.SendWebRequest();

        if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
        }
        else
        {
            var response = www.downloadHandler.text;
            var fbUserDetails = JsonUtility.FromJson<FBUserDetails>(response);

            Dictionary<string,string> socialLoginparam = new Dictionary<string,string>(); 
            socialLoginparam["id"] = accessToken;
            socialLoginparam["social_type"] = socialLoginType;
        
            staticVariables.createPlayer_response_id = fbUserDetails.id;
            isGoogleLogin = false;
            ApiAndRoomManager._instance.SocialLogin(socialLoginparam,HandleSocialLoginSuccess);

        }
    }
    //  bool IsSocialAccountLinkWithPhoneNumber(string response)
    bool IsSocialLinkedWithPhone(string response)
    {
        if (response.Contains("user_created") || response.Contains("player_in_active"))
            return false;
        else return true;
    }
    //  void HandleSocailSucess(string OnSucess)
    void HandleSocialLoginSuccess(string OnSucess)
    {
        UserModel userModel = JsonUtility.FromJson<UserModel>(OnSucess);
        if (userModel.status)
        {
            
            if (IsSocialLinkedWithPhone(userModel.message))
            {
                string userModeljson = JsonUtility.ToJson(userModel);
                PlayerPrefs.SetString("userModel", userModeljson);
                //Debug.Log("Adding Data login :" + userModeljson);
                ApiAndRoomManager.LastFetchedCoins.data.gold_balance = userModel.user.gold_balance.ToString();
                ApiAndRoomManager.LastFetchedCoins.data.silver_balance = userModel.user.silver_balance.ToString();
                staticVariables.UserProfiledata = userModel;

                if (string.IsNullOrEmpty(userModel.user.user_login_token) || userModel.user.user_login_token == staticVariables.uniqueGameIdentifier)
                {

                    SceneLoaderUtility.LoadScene("Home");
                }
                else
                {
                    if (duplicateLoginErrorPanel != null)
                    {
                        duplicateLoginErrorPanel.SetActive(true);
                    }
                    else
                    {
                        ConstantsData_M.Log("DuplicateLogin is null.");
                    }
                }
            }
            else
            {
                phoneVerificationPanel.SetActive(true);

            }
        }
    }
    //  private void OnInitComplete()
    private void OnInitializationComplete()
    {
    }
    //  private void OnHideUnity(bool isGameShown)
    private void OnHideUnity(bool isGameShown)
    {
    }

    [Serializable]
    public class OTPSendResponse
    {
        public bool status;
        public string message;
        public Data data;
        [Serializable]
        public class Data
        {
            public int _id;
            public int OTP;
        }
    }
    [Serializable]
    public class FBLoginResult
    {
        public string access_token;
        public string auth_token_string;
        public string auth_nonce;
        public string user_id;
        public string callback_id;
        public string key_hash;
        public string permissions;
        public string graph_domain;
        public long expiration_timestamp;
        public long last_refresh;
        public bool opened;
        public string declined_permissions;
    }
    [Serializable]
    public class FBUserDetails
    {
        public string id;
        public string name;
        public string email;
        public Location location;
        [Serializable]
        public class Location
        {
            public string name;
        }
    }


  
}

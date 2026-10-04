using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class ServerAppOrganizer : EditorWindow
{
    static readonly (string f, string t)[] Moves = {
        // Auth
        ("Assets/_App/LoginUIManager.cs",               "Assets/_App/Auth/LoginUIManager.cs"),
        ("Assets/_App/UISignupManager.cs",              "Assets/_App/Auth/UISignupManager.cs"),
        ("Assets/_App/GuestDataManager.cs",             "Assets/_App/Auth/GuestDataManager.cs"),
        ("Assets/_App/GenerateReferralCode.cs",         "Assets/_App/Auth/GenerateReferralCode.cs"),
        ("Assets/_App/AppCheckFirebase.cs",             "Assets/_App/Auth/AppCheckFirebase.cs"),
        // Firebase
        ("Assets/_App/FirebaseHandler.cs",              "Assets/_App/Firebase/FirebaseHandler.cs"),
        ("Assets/_App/CrashlyticsTester.cs",            "Assets/_App/Firebase/CrashlyticsTester.cs"),
        // Shop
        ("Assets/_App/ShopUIManager.cs",                "Assets/_App/Shop/ShopUIManager.cs"),
        ("Assets/_App/BundlePurchase.cs",               "Assets/_App/Shop/BundlePurchase.cs"),
        ("Assets/_App/SingleShopPackage.cs",            "Assets/_App/Shop/SingleShopPackage.cs"),
        ("Assets/_App/UISinglePackageManager.cs",       "Assets/_App/Shop/UISinglePackageManager.cs"),
        ("Assets/_App/SingleWalletCoins.cs",            "Assets/_App/Shop/SingleWalletCoins.cs"),
        // Payment
        ("Assets/_App/WithdrawManager.cs",              "Assets/_App/Payment/WithdrawManager.cs"),
        ("Assets/_App/UISelectNetworkNameManager.cs",   "Assets/_App/Payment/UISelectNetworkNameManager.cs"),
        ("Assets/_App/SingleAdminBankDetail.cs",        "Assets/_App/Payment/SingleAdminBankDetail.cs"),
        ("Assets/_App/UITransectionDetailSubmition.cs", "Assets/_App/Payment/UITransectionDetailSubmition.cs"),
        ("Assets/_App/SingleWithdrawalHistory.cs",      "Assets/_App/Payment/SingleWithdrawalHistory.cs"),
        // History
        ("Assets/_App/GameHistoryManager.cs",           "Assets/_App/History/GameHistoryManager.cs"),
        ("Assets/_App/HistoryManager.cs",               "Assets/_App/History/HistoryManager.cs"),
        ("Assets/_App/BorrowHistoryManager.cs",         "Assets/_App/History/BorrowHistoryManager.cs"),
        ("Assets/_App/PurchaseHistoryManager.cs",       "Assets/_App/History/PurchaseHistoryManager.cs"),
        ("Assets/_App/RoundHistoryManager.cs",          "Assets/_App/History/RoundHistoryManager.cs"),
        ("Assets/_App/CasinoGameHistory.cs",            "Assets/_App/History/CasinoGameHistory.cs"),
        ("Assets/_App/HistoryGameButton_data.cs",       "Assets/_App/History/HistoryGameButton_data.cs"),
        ("Assets/_App/SingleGameHistory.cs",            "Assets/_App/History/SingleGameHistory.cs"),
        ("Assets/_App/ClientHisory.cs",                 "Assets/_App/History/ClientHisory.cs"),
        // Notifications
        ("Assets/_App/NotificationCountdownManager.cs", "Assets/_App/Notifications/NotificationCountdownManager.cs"),
        ("Assets/_App/NotificationSystemBell.cs",       "Assets/_App/Notifications/NotificationSystemBell.cs"),
        ("Assets/_App/NotificationUIManager.cs",        "Assets/_App/Notifications/NotificationUIManager.cs"),
        ("Assets/_App/ActionableNotification.cs",       "Assets/_App/Notifications/ActionableNotification.cs"),
        ("Assets/_App/Notification",                    "Assets/_App/Notifications/Notification"),
        ("Assets/_App/SimpleNotification.cs",           "Assets/_App/Notifications/SimpleNotification.cs"),
        // Social
        ("Assets/_App/SearchFriend.cs",                 "Assets/_App/Social/SearchFriend.cs"),
        ("Assets/_App/SocialMediaManager.cs",           "Assets/_App/Social/SocialMediaManager.cs"),
        ("Assets/_App/ShareWhatsAppText.cs",            "Assets/_App/Social/ShareWhatsAppText.cs"),
        ("Assets/_App/friendrequestItem.cs",            "Assets/_App/Social/friendrequestItem.cs"),
        ("Assets/_App/SingleWhatsappContacts.cs",       "Assets/_App/Social/SingleWhatsappContacts.cs"),
        // Core
        ("Assets/_App/GameManagerMainMenu.cs",          "Assets/_App/Core/GameManagerMainMenu.cs"),
        ("Assets/_App/LoadingManager.cs",               "Assets/_App/Core/LoadingManager.cs"),
        ("Assets/_App/InternetChecker.cs",              "Assets/_App/Core/InternetChecker.cs"),
        ("Assets/_App/AllScriptsManager.cs",            "Assets/_App/Core/AllScriptsManager.cs"),
        ("Assets/_App/ServerConnection.cs",             "Assets/_App/Core/ServerConnection.cs"),
        ("Assets/_App/ApiAndRoomManager.cs",            "Assets/_App/Core/ApiAndRoomManager.cs"),
        ("Assets/_App/DeepLinkManager.cs",              "Assets/_App/Core/DeepLinkManager.cs"),
        ("Assets/_App/GoHome.cs",                       "Assets/_App/Core/GoHome.cs"),
        ("Assets/_App/ConstantsData_M.cs",              "Assets/_App/Core/ConstantsData_M.cs"),
        ("Assets/_App/JSONObject.cs",                   "Assets/_App/Core/JSONObject.cs"),
        ("Assets/_App/OpenChallengeManager.cs",         "Assets/_App/Core/OpenChallengeManager.cs"),
        ("Assets/_App/MyChallengeItem.cs",              "Assets/_App/Core/MyChallengeItem.cs"),
        ("Assets/_App/SmallChallenge.cs",               "Assets/_App/Core/SmallChallenge.cs"),
        ("Assets/_App/GamesTutorial.cs",                "Assets/_App/Core/GamesTutorial.cs"),
        ("Assets/_App/RulesDetail.cs",                  "Assets/_App/Core/RulesDetail.cs"),
        ("Assets/_App/SupportUIManager.cs",             "Assets/_App/Core/SupportUIManager.cs"),
        ("Assets/_App/GameWinnerMsg.cs",                "Assets/_App/Core/GameWinnerMsg.cs"),
        ("Assets/_App/PlaywithFriendSilverCoinsManager.cs","Assets/_App/Core/PlaywithFriendSilverCoinsManager.cs"),
        ("Assets/_App/MessageInfo.cs",                  "Assets/_App/Core/MessageInfo.cs"),
        ("Assets/_App/PopupMessageManager.cs",          "Assets/_App/Core/PopupMessageManager.cs"),
        ("Assets/_App/ConfirmAlert.cs",                 "Assets/_App/Core/ConfirmAlert.cs"),
        ("Assets/_App/CountDownManager.cs",             "Assets/_App/Core/CountDownManager.cs"),
        // UI
        ("Assets/_App/UIMainMenManager.cs",             "Assets/_App/UI/UIMainMenManager.cs"),
        ("Assets/_App/UIprofileManager.cs",             "Assets/_App/UI/UIprofileManager.cs"),
        ("Assets/_App/UISuccessfulManager.cs",          "Assets/_App/UI/UISuccessfulManager.cs"),
        ("Assets/_App/UiAdjuster.cs",                   "Assets/_App/UI/UiAdjuster.cs"),
        ("Assets/_App/OnClick_PanelFalse.cs",           "Assets/_App/UI/OnClick_PanelFalse.cs"),
        ("Assets/_App/BannerMove.cs",                   "Assets/_App/UI/BannerMove.cs"),
        ("Assets/_App/SpriteAnimator.cs",               "Assets/_App/UI/SpriteAnimator.cs"),
        ("Assets/_App/SpritesManager.cs",               "Assets/_App/UI/SpritesManager.cs"),
        ("Assets/_App/ImageShowPanel.cs",               "Assets/_App/UI/ImageShowPanel.cs"),
        ("Assets/_App/loaderRotating.cs",               "Assets/_App/UI/loaderRotating.cs"),
        ("Assets/_App/shortsliderMovement.cs",          "Assets/_App/UI/shortsliderMovement.cs"),
        ("Assets/_App/invitacceptloading.cs",           "Assets/_App/UI/invitacceptloading.cs"),
        ("Assets/_App/HandPositionSetter.cs",           "Assets/_App/UI/HandPositionSetter.cs"),
        ("Assets/_App/SelectOver.cs",                   "Assets/_App/UI/SelectOver.cs"),
        ("Assets/_App/SideButtons.cs",                  "Assets/_App/UI/SideButtons.cs"),
        ("Assets/_App/OptionPrefab.cs",                 "Assets/_App/UI/OptionPrefab.cs"),
        ("Assets/_App/ChallengeItemPrefab.cs",          "Assets/_App/UI/ChallengeItemPrefab.cs"),
        ("Assets/_App/GameButton_InfoSetter.cs",        "Assets/_App/UI/GameButton_InfoSetter.cs"),
        ("Assets/_App/SingleBannerCollection.cs",       "Assets/_App/UI/SingleBannerCollection.cs"),
        ("Assets/_App/Home sceneScripts",               "Assets/_App/UI/Home sceneScripts"),
        ("Assets/_App/Waiting Panel Script",            "Assets/_App/UI/Waiting Panel Script"),
        ("Assets/_App/OnConnectionLoose_script",        "Assets/_App/UI/OnConnectionLoose_script"),
        ("Assets/_App/Person Data",                     "Assets/_App/UI/Person Data"),
        // Audio
        ("Assets/_App/SoundManagerMain.cs",             "Assets/_App/Audio/SoundManagerMain.cs"),
        ("Assets/_App/MusicManagerMainMenu.cs",         "Assets/_App/Audio/MusicManagerMainMenu.cs"),
        ("Assets/_App/AudioRouteManager.cs",            "Assets/_App/Audio/AudioRouteManager.cs"),
        ("Assets/_App/AgoraSpeaker.cs",                 "Assets/_App/Audio/AgoraSpeaker.cs"),
        ("Assets/_App/Microphone.cs",                   "Assets/_App/Audio/Microphone.cs"),
        ("Assets/_App/TwelveBeadSoundManager.cs",       "Assets/_App/Audio/TwelveBeadSoundManager.cs"),
        // Platform
        ("Assets/_App/APKUpdater.cs",                   "Assets/_App/Platform/APKUpdater.cs"),
        ("Assets/_App/AndroidPermission.cs",            "Assets/_App/Platform/AndroidPermission.cs"),
        ("Assets/_App/AndroidUtility.cs",               "Assets/_App/Platform/AndroidUtility.cs"),
        ("Assets/_App/KeyPadInput.cs",                  "Assets/_App/Platform/KeyPadInput.cs"),
        ("Assets/_App/KeyboardAnimation.cs",            "Assets/_App/Platform/KeyboardAnimation.cs"),
        ("Assets/_App/KeyboardHeightProvider.cs",       "Assets/_App/Platform/KeyboardHeightProvider.cs"),
        ("Assets/_App/KeyboardInputAdjuster.cs",        "Assets/_App/Platform/KeyboardInputAdjuster.cs"),
        ("Assets/_App/DeepLodaer.cs",                   "Assets/_App/Platform/DeepLodaer.cs"),
        // Rewards
        ("Assets/_App/GiftManager.cs",                  "Assets/_App/Rewards/GiftManager.cs"),
        ("Assets/_App/DailyRewardManager.cs",           "Assets/_App/Rewards/DailyRewardManager.cs"),
        ("Assets/_App/Daily Rewards",                   "Assets/_App/Rewards/Daily Rewards"),
        // Game-specific → _Games/
        ("Assets/_App/ResultManagerForCarrom.cs",        "Assets/_Games/Carrom/ResultManagerForCarrom.cs"),
        ("Assets/_App/ResultManagerForCarromOffline.cs", "Assets/_Games/Carrom/ResultManagerForCarromOffline.cs"),
        ("Assets/_App/WinPanel 8 ball",                  "Assets/_Games/8Ball pool/WinPanel 8 ball"),
        ("Assets/_App/Carrom Online",                    "Assets/_Games/Carrom/Carrom Online"),
        // Dev / junk
        ("Assets/_App/Adnan added Scripts",              "Assets/_Dev/Adnan"),
        ("Assets/_App/FerjadScript",                     "Assets/_Dev/Ferjad"),
        ("Assets/_App/AiBGSetup.cs",                     "Assets/_Dev/AiBGSetup.cs"),
        ("Assets/_App/events.cs",                        "Assets/_Dev/events.cs"),
        ("Assets/_App/DummyHorse.cs",                    "Assets/_Dev/DummyHorse.cs"),
        ("Assets/_App/_www.unityassetcollection.com.txt","Assets/_Dev/_www.unityassetcollection.com.txt"),
        ("Assets/_App/apitest.cs",                       "Assets/_Dev/apitest.cs"),
        ("Assets/_App/test api.unity",                   "Assets/_Dev/test api.unity"),
    };

    [MenuItem("Tools/Structure/Phase 7 - App Organizer")]
    public static void Run()
    {
        if (!EditorUtility.DisplayDialog("Server App Organizer",
            "Organises _App/ scripts into sub-folders.\nContinue?", "Yes", "Cancel")) return;

        int mv = 0, sk = 0, fl = 0;
        var lg = new List<string>();

        foreach (string f in new[]{
            "Assets/_App/Auth","Assets/_App/Firebase","Assets/_App/Shop",
            "Assets/_App/Payment","Assets/_App/History","Assets/_App/Notifications",
            "Assets/_App/Social","Assets/_App/Core","Assets/_App/UI",
            "Assets/_App/Audio","Assets/_App/Platform","Assets/_App/Rewards",
            "Assets/_Dev","Assets/_Games/Carrom","Assets/_Games/8Ball pool",
        }) EF(f);

        int total = Moves.Length, p = 0;
        foreach (var (from, to) in Moves)
        {
            EditorUtility.DisplayProgressBar("App Organizer", Path.GetFileName(from), (float)p++ / total);
            Move(from, to, ref mv, ref sk, ref fl, lg);
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        foreach (var e in lg) Debug.Log("[AppOrg] " + e);
        EditorUtility.DisplayDialog("App Organizer Done",
            $"Moved:{mv}  Skipped:{sk}  Failed:{fl}\n\nNext: 3. Server Network Organizer", "OK");
    }

    static void EF(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int s = path.LastIndexOf('/');
        EF(path.Substring(0, s));
        AssetDatabase.CreateFolder(path.Substring(0, s), path.Substring(s + 1));
    }

    static void Move(string from, string to, ref int mv, ref int sk, ref int fl, List<string> lg)
    {
        bool src = AssetDatabase.IsValidFolder(from) || AssetDatabase.LoadAssetAtPath<Object>(from) != null;
        if (!src) { sk++; return; }
        bool dst = AssetDatabase.IsValidFolder(to)   || AssetDatabase.LoadAssetAtPath<Object>(to) != null;
        if (dst) { lg.Add($"⚠ exists:{to}"); sk++; return; }
        EF(to.Substring(0, to.LastIndexOf('/')));
        string err = AssetDatabase.MoveAsset(from, to);
        if (string.IsNullOrEmpty(err)) { lg.Add($"✓ {Path.GetFileName(from)}"); mv++; }
        else { lg.Add($"✗ {from}: {err}"); fl++; }
    }
}

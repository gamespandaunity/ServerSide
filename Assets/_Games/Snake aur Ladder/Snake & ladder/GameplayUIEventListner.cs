using Mirror;
using Snake_Ladder;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityExtensions;

namespace Twelve
{
    public class GameplayUIEventListner : MonoBehaviour
    {
        [Header("Avatar Images")]
        public Image playerAvatarImage;
        public Image opponentAvatarImage;

        [Header("Player Names")]
        public TextMeshProUGUI playerNameText;
        public TextMeshProUGUI opponentNameText;

        [Header("Other")]
        public GameObject retryButton;
        public static Sprite opponentSprite;
        public SpritesHOlder scriptableSpriteHolder;

        private int myPlayerNumber = -1;
        private bool uiSetupComplete = false;

        IEnumerator Start()
        {
            Debug.Log("🎮 GameplayUIEventListner START");
            if (NetworkClient.active == false && NetworkServer.active == false)
            {
                Debug.Log("AI Game");
                SetAIModeUI();

            }
            yield return new WaitUntil(() => SnakeMirrorNetworkManager.Instance != null);

            // Subscribe to player assignment event

            bool isOnlineMode = Snake_Ladder.SnakeGameManager.instance.currentGameMode.Equals(Snake_Ladder.SnakeGameManager.GameMode.Against_OnlineFriend)
                             || Snake_Ladder.SnakeGameManager.instance.currentGameMode.Equals(Snake_Ladder.SnakeGameManager.GameMode.Against_RandomPlayer);

            if (isOnlineMode)
            {
                retryButton.SetActive(false);

                // ✅ Wait for player assignment
                yield return new WaitUntil(() => myPlayerNumber >= 0);
                Debug.Log($"✅ Player assigned: {myPlayerNumber}");

                // ✅ Wait for opponent data to be available
                yield return new WaitForSeconds(0.5f);
            }

            SetPlayerUI();
        }

        void OnPlayerAssigned(int playerNumber)
        {
            myPlayerNumber = playerNumber;
            Debug.Log($"🎮 GameplayUIEventListner: My Player Number = {myPlayerNumber}");
        }

        void SetPlayerUI()
        {
            if (uiSetupComplete) return;

            Debug.Log("╔═══════════════════════════════════╗");
            Debug.Log("║     SETTING PLAYER UI             ║");
            Debug.Log("╚═══════════════════════════════════╝");

            bool isOnlineMode = Snake_Ladder.SnakeGameManager.instance.currentGameMode.Equals(Snake_Ladder.SnakeGameManager.GameMode.Against_OnlineFriend)
                             || Snake_Ladder.SnakeGameManager.instance.currentGameMode.Equals(Snake_Ladder.SnakeGameManager.GameMode.Against_RandomPlayer);

            if (NetworkServer.active || NetworkClient.active)
            {
                SetOnlineModeUI();
            }


            uiSetupComplete = true;
        }

        void SetOnlineModeUI()
        {
            Debug.Log($"🌐 Setting ONLINE mode UI for Player {myPlayerNumber + 1}");

            // ========== MY INFO (Always same for me) ==========

            // ✅ MY Avatar
            if (staticVariables.ProfilePicture != null)
            {
                playerAvatarImage.sprite = Sprite.Create(
                    staticVariables.ProfilePicture,
                    new Rect(0, 0, staticVariables.ProfilePicture.width, staticVariables.ProfilePicture.height),
                    Vector2.zero
                );
                Debug.Log("✅ My Avatar Set");
            }
            else
            {
                Debug.LogWarning("⚠️ My ProfilePicture is NULL!");
            }

            // ✅ MY Name
            string myName = "Player";
            if (staticVariables.UserProfiledata?.user?.first_name != null)
            {
                myName = staticVariables.UserProfiledata.user.first_name;
            }

            if (playerNameText != null)
            {
                playerNameText.text = myName;
                Debug.Log($"✅ My Name Set: {myName}");
            }

            // ========== OPPONENT INFO ==========

            // ✅ OPPONENT Avatar
            if (staticVariables.opponentImage != null)
            {
                opponentAvatarImage.sprite = Sprite.Create(
                    staticVariables.opponentImage,
                    new Rect(0, 0, staticVariables.opponentImage.width, staticVariables.opponentImage.height),
                    Vector2.zero
                );
                opponentSprite = opponentAvatarImage.sprite;
                Debug.Log("✅ Opponent Avatar Set");
            }
            else
            {
                Debug.LogWarning("⚠️ Opponent Image is NULL!");
            }

            // ✅ OPPONENT Name
            string opponentName = "Opponent";
            if (staticVariables.OpponetProfile?.userName != null)
            {
                opponentName = staticVariables.OpponetProfile.userName;
            }

            if (opponentNameText != null)
            {
                opponentNameText.text = opponentName;
                Debug.Log($"✅ Opponent Name Set: {opponentName}");
            }

            Debug.Log("╔═══════════════════════════════════╗");
            Debug.Log($"║ MY INFO:                          ║");
            Debug.Log($"║   Player Number: {myPlayerNumber}                ║");
            Debug.Log($"║   Name: {myName}                  ");
            Debug.Log($"║   Avatar: {(staticVariables.ProfilePicture != null ? "✅" : "❌")}");
            Debug.Log($"║ OPPONENT INFO:                    ║");
            Debug.Log($"║   Name: {opponentName}            ");
            Debug.Log($"║   Avatar: {(staticVariables.opponentImage != null ? "✅" : "❌")}");
            Debug.Log("╚═══════════════════════════════════╝");
        }

        void SetAIModeUI()
        {
            Debug.Log("🤖 Setting AI mode UI");

            // ✅ MY Avatar
            if (staticVariables.ProfilePicture != null)
            {
                playerAvatarImage.sprite = Sprite.Create(
                    staticVariables.ProfilePicture,
                    new Rect(0, 0, staticVariables.ProfilePicture.width, staticVariables.ProfilePicture.height),
                    Vector2.zero
                );
            }

            // ✅ MY Name
            string myName = "Player";
            if (staticVariables.UserProfiledata?.user?.first_name != null)
            {
                myName = staticVariables.UserProfiledata.user.first_name;
            }

            if (playerNameText != null)
            {
                playerNameText.text = myName;
            }

            // ✅ AI Avatar
            if (scriptableSpriteHolder?.aiImg != null)
            {
                opponentAvatarImage.sprite = Sprite.Create(
                    scriptableSpriteHolder.aiImg,
                    new Rect(0, 0, scriptableSpriteHolder.aiImg.width, scriptableSpriteHolder.aiImg.height),
                    Vector2.zero
                );
                opponentSprite = opponentAvatarImage.sprite;
            }

            // ✅ AI Name
            if (opponentNameText != null)
            {
                opponentNameText.text = "AI";
            }

            Debug.Log($"✅ AI Mode UI Set - My Name: {myName}");
        }

        public static bool IsGameQuite;

        public void HandleQuitButtonClick()
        {
            IsGameQuite = true;
            ResultManager.isGameFinished = true;

            if (NetworkServer.active || NetworkClient.active)
            {
                Debug.LogError("photon.leave room");
                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
                Debug.Log("currentDisconnectReason" + MirrorNetwork.Instance.currentDisconnectReason);
                // if (NetworkGameManager.Instance)
                NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
                NetworkGameManager.Instance.CmdReloadServer();
                Debug.Log("USER" + staticVariables.UserProfiledata.user._id.ToString());

                this.Delay(1, () => NetworkManager.singleton.StopClient());
                SceneManager.LoadScene("Home");
            }
            else
            {
                Debug.Log("AI GameLeave");
                SceneManager.LoadScene("Home");
            }

        }
    }
}
//using Snake_Ladder;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//using UnityEngine.Serialization;
//using UnityEngine.UI;

//namespace Twelve
//{
//    public class GameplayUIEventListner : MonoBehaviour
//    {

//        [FormerlySerializedAs("myAvatar")] public Image playerAvatarImage;
//        [FormerlySerializedAs("OpponentAvatar")] public Image opponentAvatarImage;
//        [FormerlySerializedAs("retryBtn")] public GameObject retryButton;
//        public static Sprite opponentSprite;
//        public SpritesHOlder scriptableSpriteHolder;


//        IEnumerator Start()
//        {
//            yield return new WaitUntil(() => SnakeMirrorNetworkManager.Instance != null);
//            SetPlayerUI();
//            if (Snake_Ladder.SnakeGameManager.instance.currentGameMode.Equals(Snake_Ladder.SnakeGameManager.GameMode.Against_OnlineFriend)
//            || Snake_Ladder.SnakeGameManager.instance.currentGameMode.Equals(Snake_Ladder.SnakeGameManager.GameMode.Against_RandomPlayer))
//            {
//                retryButton.SetActive(false);
//            }
//        }
//        void SetPlayerUI()
//        {
//            if (staticVariables.ProfilePicture)
//                playerAvatarImage.sprite = Sprite.Create(staticVariables.ProfilePicture, new Rect(0, 0, staticVariables.ProfilePicture.width, staticVariables.ProfilePicture.height), Vector2.zero);



//            if (Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_OnlineFriend))
//            {
//                if (staticVariables.opponentImage)
//                {

//                    opponentAvatarImage.sprite = Sprite.Create(staticVariables.opponentImage, new Rect(0, 0, staticVariables.opponentImage.width, staticVariables.opponentImage.height), Vector2.zero);
//                    opponentSprite = opponentAvatarImage.sprite;
//                }
//            }
//            else
//            {
//                opponentAvatarImage.sprite = Sprite.Create(scriptableSpriteHolder.aiImg, new Rect(0, 0, staticVariables.opponentImage.width, staticVariables.opponentImage.height), Vector2.zero);
//                //  opponentAvatarImage.sprite = Sprite.Create(scriptableSpriteHolder.aiImg, new Rect(0, 0, SpritesManager.Instance.spritesScriptable.aiImg.width, SpritesManager.Instance.spritesScriptable.aiImg.height), Vector2.zero);
//                opponentSprite = opponentAvatarImage.sprite;
//            }

//        }



//        public static bool IsGameQuite;
//        public void HandleQuitButtonClick()
//        {
//            IsGameQuite = true;
//            if (Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_OnlineFriend)
//                 || Snake_Ladder.GameManager.instance.currentGameMode.Equals(Snake_Ladder.GameManager.GameMode.Against_RandomPlayer))
//            {

//                //GameModeManager.isAI = false;
//                MirrorNetwork.Instance.currentDisconnectReason = DisconnectReason.ApplicationQuit;
//                Debug.Log("DisconnectReason.ApplicationPause" + MirrorNetwork.Instance.currentDisconnectReason);
//                // NetworkGameManager.Instance.CmdSendDisconnectType((int)MirrorNetwork.Instance.currentDisconnectReason);
//                Mirror.NetworkManager.singleton.StopClient();                                          //Photon Removal
//                SceneManager.LoadScene("Home");

//            }
//            else
//                SceneManager.LoadScene("Home");
//            //if (PhotonNetwork.InRoom)
//            //{                                                                                                        //Photon Removal

//            //    PhotonNetwork.LeaveRoom();
//            //}
//            //   TwelveBeadSoundManager.instance.PlayAnySound(10);

//        }


//    }
//}

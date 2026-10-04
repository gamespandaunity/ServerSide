using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BEKStudio;
 
using UnityEngine.SceneManagement;
using static BEKStudio.GameController;
using NetworkManagement;
using UnityEngine.Serialization;
namespace BEKStudio
{
    public class GameOverEventListnerOffline : MonoBehaviour
    {
        [FormerlySerializedAs("stateImage")] public Image gameStateIcon;
        [Header("Winner")]
        [FormerlySerializedAs("winnerAvatar")] public RawImage winnerAvatarImage;
        [FormerlySerializedAs("winnerName")] public TextMeshProUGUI winnerPlayerName;
        [FormerlySerializedAs("winnerScore")] public TextMeshProUGUI winnerPlayerScore;
        [FormerlySerializedAs("winnerPuckIcon")] public Image winnerPuckImage;
        [FormerlySerializedAs("winnerRedPuck")] public GameObject winnerRedPuckIcon;
        [FormerlySerializedAs("winStateSprite")] public Sprite winStateIcon;

        [Header("Loser")]
        [FormerlySerializedAs("loserAvatar")] public RawImage loserAvatarImage;
        [FormerlySerializedAs("loserName")] public TextMeshProUGUI loserPlayerName;
        [FormerlySerializedAs("loserScore")] public TextMeshProUGUI loserPlayerScore;
        [FormerlySerializedAs("loserPuckIcon")] public Image loserPuckImage;
        [FormerlySerializedAs("loserRedPuck")] public GameObject loserRedPuckIcon;
        [FormerlySerializedAs("loseStateSprite")] public Sprite loseStateIcon;

        [FormerlySerializedAs("leftPuckSprite")] public Sprite leftPlayerPuckSprite;
        [FormerlySerializedAs("rightPuckSprite")] public Sprite rightPlayerPuckSprite;

        //Left
        private RawImage leftPlayerAvatar;
        private TextMeshProUGUI leftPlayerName;
        private TextMeshProUGUI leftPlayerScore;
        private Sprite leftPlayerPuck;
        private GameObject leftPlayerRedPuckIcon;

        //Right
        private RawImage rightPlayerAvatar;
        private TextMeshProUGUI rightPlayerName;
        private TextMeshProUGUI rightPlayerScore;
        private Sprite rightPlayerPuck;
        private GameObject rightPlayerRedPuckIcon;
        private void OnEnable()
        {
            GameControllerOffline controller = GameControllerOffline.Instance;

            // Initialize left player information

            leftPlayerAvatar = controller.topHomeAvatar;
            leftPlayerName = controller.topHomeNameText;
            leftPlayerScore = controller.topHomeScoreText;
            leftPlayerPuck = leftPlayerPuckSprite;
            leftPlayerRedPuckIcon = controller.leftRedPuckIcon;

            Debug.Log("🟢 Left Player Initialized:");
            Debug.Log($"Avatar: {leftPlayerAvatar?.name}");
            Debug.Log($"Name Text: {leftPlayerName?.text}");
            Debug.Log($"Score Text: {leftPlayerScore?.text}");
            Debug.Log($"Puck Sprite: {leftPlayerPuck?.name}");
            Debug.Log($"Red Puck Icon: {leftPlayerRedPuckIcon?.name}");

            // Initialize right player information
            rightPlayerAvatar = controller.topAwayAvatar;
            rightPlayerName = controller.topAwayNameText;
            rightPlayerScore = controller.topAwayScoreText;
            rightPlayerPuck = rightPlayerPuckSprite;
            rightPlayerRedPuckIcon = controller.rightRedPuckIcon;

            Debug.Log("🔵 Right Player Initialized:");
            Debug.Log($"Avatar: {rightPlayerAvatar?.name}");
            Debug.Log($"Name Text: {rightPlayerName?.text}");
            Debug.Log($"Score Text: {rightPlayerScore?.text}");
            Debug.Log($"Puck Sprite: {rightPlayerPuck?.name}");
            Debug.Log($"Red Puck Icon: {rightPlayerRedPuckIcon?.name}");

            bool isWhitCollectedQueen = controller.leftRedPuckIcon.activeInHierarchy ? true : false;

            //if (PhotonController.Instance.whichMode != "carrom")
            //{
            //    isWhitCollectedQueen = controller.masterClientTag == "Black" ? false : true;
            //}
            // Determine if the local player is the master client
            bool isMasterClient = GameManager.Instance.masterClient;

            //controller.gameState = controller.gameState==GameState.WIN?GameState.LOSE:GameState.WIN;
            //Constants_M.Log("Game State.... 2" + controller.gameState);


            // if (!PhotonController.Instance.isOtherPlayerLeft)  // Carromintegration
            {
                //controller.VerifyGameState(isWhitCollectedQueen);

            }
            //Constants_M.Log("Game State.... 3" + controller.gameState);

            GameControllerOffline.GameState gameState = controller.gameState;
            gameState.Show();
            // Check the game state and update the UI accordingly
            if (!GameManager.Instance.isOnline())
            {
            }
            else
            {
               
                //if (PhotonNetwork.InRoom)
                //{
                //    PhotonNetwork.CurrentRoom.PlayerTtl = 0;
                //    PhotonNetwork.CurrentRoom.EmptyRoomTtl = 0;
                //    PhotonNetwork.LeaveRoom();
                //}

            }
            if (isMasterClient)
            {
                1.Show();
                if (gameState == GameControllerOffline.GameState.WIN)
                {
                    gameStateIcon.sprite = winStateIcon;
                    // Master client won
                    UpdateWinnerUI(leftPlayerAvatar, leftPlayerName, leftPlayerScore, leftPlayerPuck, leftPlayerRedPuckIcon);
                    UpdateLoserUI(rightPlayerAvatar, rightPlayerName, rightPlayerScore, rightPlayerPuck, rightPlayerRedPuckIcon);
                    2.Show();
                    //if (GameManager.Instance.isOnline())
                    Debug.Log("MasterWin");
                     //ResultManagerForCarrom.instance.WinPlayer(false, int.Parse(staticVariables.OpponetProfile.userId));
                    ResultManagerForCarromOffline.instance.WinPlayer(true, staticVariables.UserProfiledata.user._id);
                    //ApiAndRoomManager._instance.WinnerLossChallenge(staticVariables.UserProfiledata.user._id.ToString());
                }
                else
                {
                    gameStateIcon.sprite = loseStateIcon;
                    // Master client lost
                      Debug.Log("Masterlost");
                //     ResultManagerForCarrom.instance.WinPlayer(true, int.Parse(staticVariables.OpponetProfile.userId));
                    ResultManagerForCarromOffline.instance.WinPlayer(false, staticVariables.UserProfiledata.user._id);
                    UpdateWinnerUI(rightPlayerAvatar, rightPlayerName, rightPlayerScore, rightPlayerPuck, rightPlayerRedPuckIcon);
                    UpdateLoserUI(leftPlayerAvatar, leftPlayerName, leftPlayerScore, leftPlayerPuck, leftPlayerRedPuckIcon);
                    3.Show();
                }
            }
            else
            {

                if (gameState == GameControllerOffline.GameState.WIN)
                {
                    gameStateIcon.sprite = winStateIcon;
                    // Non-master client won
                      Debug.Log("Masterlost");
                    UpdateWinnerUI(rightPlayerAvatar, rightPlayerName, rightPlayerScore, rightPlayerPuck, rightPlayerRedPuckIcon);
                    UpdateLoserUI(leftPlayerAvatar, leftPlayerName, leftPlayerScore, leftPlayerPuck, leftPlayerRedPuckIcon);
                    4.Show();
                    //if (GameManager.Instance.isOnline())
                 //   ResultManagerForCarrom.instance.WinPlayer(true, int.Parse(staticVariables.OpponetProfile.userId));
                      ResultManagerForCarromOffline.instance.WinPlayer(true, staticVariables.UserProfiledata.user._id);
                }
                else
                {
                    gameStateIcon.sprite = loseStateIcon;
                      Debug.Log("MasterWin");
                    // Non-master client lost
                  //  ResultManagerForCarrom.instance.WinPlayer(false, int.Parse(staticVariables.OpponetProfile.userId));
                      ResultManagerForCarromOffline.instance.WinPlayer(false, staticVariables.UserProfiledata.user._id);
                    UpdateWinnerUI(leftPlayerAvatar, leftPlayerName, leftPlayerScore, leftPlayerPuck, leftPlayerRedPuckIcon);
                    UpdateLoserUI(rightPlayerAvatar, rightPlayerName, rightPlayerScore, rightPlayerPuck, rightPlayerRedPuckIcon);
                    5.Show();
                }
            }
        }
        private void UpdateWinnerUI(RawImage avatar, TextMeshProUGUI name, TextMeshProUGUI score, Sprite puckIcon, GameObject redPuckIcon)
        {
            winnerAvatarImage.texture = avatar.texture;
            winnerPlayerName.text = name.text;
            winnerPlayerScore.text = score.text;
            winnerPuckImage.sprite = puckIcon;
            winnerRedPuckIcon.SetActive(redPuckIcon.activeSelf);

            Debug.Log("🏆 UpdateWinnerUI called:");
            Debug.Log($"Avatar Texture: {(avatar.texture != null ? avatar.texture.name : "NULL")}");
            Debug.Log($"Player Name: {name.text}");
            Debug.Log($"Score: {score.text}");
            Debug.Log($"Puck Icon: {(puckIcon != null ? puckIcon.name : "NULL")}");
            Debug.Log($"Red Puck Active: {redPuckIcon.activeSelf}");
        }

        private void UpdateLoserUI(RawImage avatar, TextMeshProUGUI name, TextMeshProUGUI score, Sprite puckIcon, GameObject redPuckIcon)
        {
            loserAvatarImage.texture = avatar.texture;
            loserPlayerName.text = name.text;
            loserPlayerScore.text = score.text;
            loserPuckImage.sprite = puckIcon;
            loserRedPuckIcon.SetActive(redPuckIcon.activeSelf);

            Debug.Log("💀 UpdateLoserUI called:");
            Debug.Log($"Avatar Texture: {(avatar.texture != null ? avatar.texture.name : "NULL")}");
            Debug.Log($"Player Name: {name.text}");
            Debug.Log($"Score: {score.text}");
            Debug.Log($"Puck Icon: {(puckIcon != null ? puckIcon.name : "NULL")}");
            Debug.Log($"Red Puck Active: {redPuckIcon.activeSelf}");
        }
       
    }
}

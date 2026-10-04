
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityStandardAssets.ImageEffects;


public class TriggerEvent : MonoBehaviour

{
    public static TriggerEvent instance;
    public string targetTag = "SpecialTag";
    public string methodName = "OnSpecialTagEnter";
    public GameObject targetObject;


    public bool finish = false;
    public GameObject winLosePanel;
   // public ResultManagerForHighwayRacing ResultManagerForHighwayRacing;
    HighwayCarNetwork Myplayer;
    [SerializeField] GameObject Notification;
    [SerializeField]public Image ResultImage;
    [SerializeField]public Sprite WinImage;
    [SerializeField]public Sprite LoseImage;
  //  [SerializeField] BlurOptimized blurview;
    public GameObject GameMiniMap;
   // public Text WinText;
    private void Awake()
    {
        instance = this;
    }
    public void Start()
    {
      //  ResultManagerForHighwayRacing = FindAnyObjectByType<ResultManagerForHighwayRacing>();
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger entered by: " + other.name);
        // Of course we check the tag, because Unity loves vibe-checking objects
        if (other.CompareTag("Player") && finish == false)
        {
            Debug.Log("Player finish");
            if (targetObject != null)
            {
                GameMiniMap.SetActive(false);
                Debug.Log("Target Object found, proceeding to send message.");
                if (SceneManager.GetActiveScene().name == "HighwayNight")
                {
                    Debug.Log("HighwayNight scene detected, sending command to server.");

                    Myplayer = other.GetComponentInParent<HighwayCarNetwork>();
                    if (Myplayer == null) return;
                    Debug.Log("HighwayCarNetwork component found, sending finish command.");
                    Notification.SetActive(true);
                   // blurview.enabled = true;
                    // if (Myplayer = staticVariables.UserProfiledata.user._id)
                    Myplayer.CmdWinText(staticVariables.UserProfiledata.user._id);
                    StartCoroutine(MultiplayerDealyWin(staticVariables.UserProfiledata.user._id));
                    //Myplayer.CmdPlayerFinished(staticVariables.UserProfiledata.user._id);

                }
                else
                {
                    // Tell the server “hey bro, do t
                    // he thing”
                    if (HR_GamePlayHandler.isCarMultiplayer)
                    {
                        //  CmdSendMessage(staticVariables.UserProfiledata.user._id);
                        //   CmdSetFinishTrue();
                    }
                    else
                    {
                      //  blurview.enabled = true;
                        Notification.SetActive(true);
                      //  WinText.text = "You Win!";
                        ResultImage.sprite=WinImage;
                        StartCoroutine(AIDealyWin(true, staticVariables.UserProfiledata.user._id.ToString()));
                        //  targetObject.SendMessage(methodName, staticVariables.UserProfiledata.user._id, SendMessageOptions.DontRequireReceiver);
                        //  SetFinishTrue();
                    }

                }
            }
            else
            {
                Debug.LogWarning("Target Object is null. Nothing to call, slay.");
            }
        }
        if (other.CompareTag("CarAI") && finish == false)
        {
             GameMiniMap.SetActive(false);
          //  blurview.enabled = true;
            Notification.SetActive(true);
             ResultImage.sprite=LoseImage;
          //  WinText.text = "You Lose!";
            StartCoroutine(AIDealyWin(false, staticVariables.UserProfiledata.user._id.ToString()));
            Debug.Log("AI finish");
           // targetObject.SendMessage(methodName, 000, SendMessageOptions.DontRequireReceiver);
           // SetFinishTrue();
        }
    }
    IEnumerator AIDealyWin(bool isWin, string playerId)
    {
        yield return new WaitForSeconds(3f);
        Notification.SetActive(false);
       // blurview.enabled = false;
        if (ResultManager.GameSpawnedFinished == false)
        {
            ResultManager.GameSpawnedFinished = true;
            GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

            if (enemyPrefab != null)
            {
                "1".Show();
                // Spawn at position (0,0,0)
                var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(isWin, staticVariables.UserProfiledata.user._id.ToString());
            }
            else
            {
                Debug.LogError("WinLose GameManager prefab not found!");
            }
        }
    }
    IEnumerator MultiplayerDealyWin(int PlayerId)
    {
        yield return new WaitForSeconds(3f);
        Notification.SetActive(false);
        //enabled = false;
        Myplayer.CmdPlayerFinished(PlayerId);// staticVariables.UserProfiledata.user._id);
       
    }
    // -------- SERVER SIDE (Commands) --------

    private void SetFinishTrue()
    {
        finish = true;
    }
    //[Command(requiresAuthority =false)]
    //private void CmdSetFinishTrue()
    //{
    //    finish = true;
    //}

    // [Command(requiresAuthority =false)]
    //private void CmdSendMessage(int id)
    //{
    //    // Server calls the RPC on all clients
    //    RpcSendMessage(id);
    //}

    //// -------- CLIENT SIDE (RPCs) --------

    //[ClientRpc]
    //private void RpcSendMessage(int id)
    //{
    //    if (targetObject != null)
    //    {
    //        targetObject.SendMessage(methodName, id, SendMessageOptions.DontRequireReceiver);
    //    }
    //    else
    //    {
    //        Debug.LogWarning("RPC: Target Object is null. Bro… seriously?");
    //    }
    //}
}
// using UnityEngine;
// using UnityEngine.SceneManagement;

// public class TriggerEvent :MonoBehaviour//Photon Removal: MonoBehaviourPun
// {
//     public string targetTag = "SpecialTag";
//     public string methodName = "OnSpecialTagEnter";
//     public GameObject targetObject;

//     public bool finish = false;

//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag(targetTag) && finish == false)
//         {
//             if (targetObject != null)
//             {
//                 if (SceneManager.GetActiveScene().name == "HighwayNight")
//                 {
//                     //Photon Removal     photonView.RPC(nameof(SendMessaageRPC), RpcTarget.All, staticVariables.UserProfiledata.user._id);  
//                     Debug.Log("MainMenu scene detected, not sending RPC.");
//                     //Photon Removal      photonView.RPC(nameof(SetFinishTrue), RpcTarget.All);
//                 }
//                 else
//                 {
//                     SendMessaageRPC(staticVariables.UserProfiledata.user._id);
//                     SetFinishTrue();
//                 }
//             }
//             else
//             {
//                 Debug.LogWarning("Target Object is null. No method called.");
//             }

//             // Call the RPC to set finish to true on all clients
//         }
//     }

//     //Photon Removal [PunRPC]
//     public void SetFinishTrue()
//     {
//         finish = true;
//     }

//     //Photon Removal  [PunRPC]
//     public void SendMessaageRPC(int id)
//     {
//         targetObject.SendMessage(methodName, id, SendMessageOptions.DontRequireReceiver);

//     }

// }

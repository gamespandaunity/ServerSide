using UnityEngine;
using Mirror;

public class FinishPointMechanism : NetworkBehaviour
{
    public static FinishPointMechanism ins;
    public bool isFinishLineCorrectChecker;
    public GameObject hurdle;
    private int position = 1;
    public void Awake()
    {
        ins = this;
    }
    public void Start()
    {
      
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
          //  winLoseHorse.gameObject.SetActive(true);

           // winLoseHorse.GetComponent<ResultManagerForHorse>().WinPlayer(true, staticVariables.UserProfiledata.user._id.ToString());

        }
    }
  //  public GameObject winLoseHorse;
 
    private void OnTriggerEnter(Collider other)
    {
       
        Debug.Log($"🟦 [FinishTrigger] Triggered by: {other.name}");

        // Try to get the PlayerPositionController from parent or self
        PlayerPositionController player = other.GetComponentInParent<PlayerPositionController>();
        if (!player)
        {
            Debug.Log("🟨 [FinishTrigger] Not found in parent, trying self...");
            player = other.GetComponent<PlayerPositionController>();
        }

        // If still null, yeet out of here
        if (player == null)
        {
            Debug.LogWarning("🟥 [FinishTrigger] No PlayerPositionController found. Collider ignored like your DMs.");
            return;
        }

        Debug.Log($"🟩 [FinishTrigger] Player found: {player.PlayerName}, PositionBefore: {player.playerPoisition}");

        // Only handle if player is at 5th position (why 5? you know it.)
        if (player.playerPoisition != 5)
        {
            Debug.Log($"🟨 [FinishTrigger] Player {player.PlayerName} not at position 5 (current: {player.playerPoisition}). Ignored.");
            return;
        }

        // Multiplayer settlement is owned by the Horse server. The server uses its
        // own horse identity, finish proximity and waypoint progress; no client ID
        // or client win flag is accepted.
        if (!MultiPlayerGame.isSinglePlayer && NetworkServer.active)
        {
            HorseAnimationSync networkHorse = other.GetComponentInParent<HorseAnimationSync>();
            if (networkHorse != null && HorseMirrorGameManager.Instance != null)
                HorseMirrorGameManager.Instance.ServerRecordFinish(networkHorse.netIdentity);
            return;
        }

        // Mark race over globally
        MConstants.isRaceOver = true;
        Debug.Log($"🏁 [FinishTrigger] Race Over triggered by: {player.PlayerName}");

        // Handle first place logic
        if (position == 1 && !player.isAI)
        {
            player.isAtFirst = true;
            Debug.Log($"👑 [FinishTrigger] Player {player.PlayerName} finished FIRST! Attaching story camera.");

            // Story camera stuff
            if (LevelsManager.instance != null && LevelsManager.instance.endRaceCamera != null)
            {
                GameObject storyCamera = LevelsManager.instance.endRaceCamera;
                storyCamera.transform.SetParent(player.transform);
                storyCamera.transform.localPosition = Vector3.zero;
                storyCamera.transform.localRotation = Quaternion.identity;
                storyCamera.SetActive(true);

                LevelsManager.instance.finishLineTarget = gameObject;
                Debug.Log($"🎥 [FinishTrigger] Story camera attached to {player.PlayerName}.");
            }
            else
            {
                Debug.LogWarning("🟥 [FinishTrigger] Missing LevelsManager or endRaceCamera. Can’t attach story camera.");
            }
        }

        // Update player position
        Debug.Log($"🟦 [FinishTrigger] Updating {player.PlayerName}'s position from {player.playerPoisition} to {position}");
        player.playerPoisition = position;

        // Mirror: add score for local player only
        if(MultiPlayerGame.isSinglePlayer)
        {
            if (player.isAI)
        {
           // int scoreToAdd = 5 - position;
           // Debug.Log($"💚 [FinishTrigger] Local player! Adding score: {scoreToAdd}");
            // GameManager.instance.AddScore(player, scoreToAdd);
            Debug.Log("AI Player finished, no score added.");
            this.gameObject.GetComponent<Collider>().enabled = false;
             //  winLoseHorse.gameObject.SetActive(true);
                 "isAIWin".Show("Result");
                if (ResultManager.GameSpawnedFinished == false)
                {
                    ResultManager.GameSpawnedFinished = true;
                    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                    if (enemyPrefab != null)
                    {
                        "1".Show();
                        // Spawn at position (0,0,0)
                        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                        gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(false, staticVariables.UserProfiledata.user._id.ToString());
                    }
                    else
                    {
                        Debug.LogError("WinLose GameManager prefab not found!");
                    }
                }
              //  winLoseHorse.GetComponent<ResultManagerForHorse>().WinPlayer(false, "ai");
        }
        else
        {
          // winLoseHorse.gameObject.SetActive(true);
           
            MConstants.isPlayerWin = true;
            Debug.Log("  MConstants.isPlayerWin "+ MConstants.isPlayerWin);
              if (MConstants.isPlayerWin)
                {
                    "isPlayerWin".Show("Result");
                //if(winLoseHorse== null)
                //{
                //  Debug.LogWarning("🟥 [FinishTrigger] ResultManagerForHorse not found in scene.")    ;
                //}
                this.gameObject.GetComponent<Collider>().enabled = false;
                    //APIManager.instance.WinnerLossAIBet(staticVariables.UserProfiledata.user._id.ToString());
                    if (ResultManager.GameSpawnedFinished == false)
                    {
                        ResultManager.GameSpawnedFinished = true;
                        GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                        if (enemyPrefab != null)
                        {
                            "1".Show();
                            // Spawn at position (0,0,0)
                            var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                            gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(true, staticVariables.UserProfiledata.user._id.ToString());
                        }
                        else
                        {
                            Debug.LogError("WinLose GameManager prefab not found!");
                        }
                    }
                  //  winLoseHorse.GetComponent<ResultManagerForHorse>().WinPlayer(true,staticVariables .UserProfiledata.user._id.ToString());
                }
                
            
        }
        }
        else
        {
            HorseAnimationSync Myplayer = other.GetComponentInParent<HorseAnimationSync>();
            if (Myplayer != null && Myplayer.isOwned)
                Myplayer.CmdPlayerFinished();
        }
        

        position++;
        Debug.Log($"🟧 [FinishTrigger] Position counter incremented. Next player will be position {position}");
    }
   
}
// using UnityEngine;
// using System.Collections;

// public class FinishPointMechanism : MonoBehaviour {

// 	public bool isfinishLineCorectChecker;
// 	public GameObject hurdle;
//     int position = 1;

// 	void OnTriggerEnter(Collider other){

        
//         //
//         if(other.transform.GetComponentInParent<PlayerPositionController>() && other.transform.GetComponentInParent<PlayerPositionController>().playerPoisition == 5){
        
//             MConstants.isRaceOver = true;
//             //Debug.Log("Pos "+ other.transform.GetComponentInParent<PlayerPositionController>().playerPoisition );

//             //MConstants.isRaceOverEnabled = true;
//             if (position ==1 && !other.transform.root.GetComponent<PlayerPositionController>().isAI)
//             {
//                 other.transform.root.GetComponent<PlayerPositionController>().isAtFirst = true;
//                 // Show story camera

//                 // GameObject StoryCamera= LevelsManager.instance.FinishSToryCamera;
//                 // StoryCamera.transform.parent = other.transform.root;
//                 // StoryCamera.transform.localPosition = Vector3.zero;
//                 // StoryCamera.transform.localRotation = Quaternion.identity;
//                 // //StoryCamera.transform.parent = null;
//                 // StoryCamera.SetActive(true);
//                 // LevelsManager.instance.Target = gameObject;

//             }
            
//             if (position ==1 && !MultiPlayerGame.isChampion)
//             {
//                 GameObject StoryCamera= LevelsManager.instance.endRaceCamera;
//                 StoryCamera.transform.parent = other.transform.root;
//                 StoryCamera.transform.localPosition = Vector3.zero;
//                 StoryCamera.transform.localRotation = Quaternion.identity;
//                 //StoryCamera.transform.parent = null;
//                 StoryCamera.SetActive(true);
//                 LevelsManager.instance.finishLineTarget  = gameObject;

//             }
//             if (other.gameObject.GetComponentInParent<PlayerPositionController>())
//             {
//                 other.gameObject.GetComponentInParent<PlayerPositionController>().playerPoisition = position;
//                 if (other.gameObject.GetComponentInParent<PhotonView>() && other.gameObject.GetComponentInParent<PhotonView>().IsMine )
//                 {
//                          other.gameObject.GetComponentInParent<PhotonView>().Owner.AddScore(5- position);
//                 }
//             }
//             else if (other.gameObject.GetComponent<PlayerPositionController>())
//             {
//                 other.gameObject.GetComponent<PlayerPositionController>().playerPoisition = position;
//                 if (other.gameObject.GetComponent<PhotonView>() && other.gameObject.GetComponent<PhotonView>().IsMine)
//                 {
//                           other.gameObject.GetComponent<PhotonView>().Owner.AddScore(5 - position);
//                 }
//             }
//             position++;
//         }
//         return;
//         //
        
//         if(other.transform.root.GetComponent<PlayerPositionController>() && other.transform.root.GetComponent<PlayerPositionController>().playerPoisition == 5 && MConstants.isRaceOverEnabled){
//             MConstants.isRaceOver = true;

//             if (position ==1)
//             {
//                 GameObject StoryCamera= LevelsManager.instance.endRaceCamera;
//                 StoryCamera.transform.parent = other.transform.root;
//                 StoryCamera.transform.localPosition = Vector3.zero;
//                 StoryCamera.transform.localRotation = Quaternion.identity;
//                 //StoryCamera.transform.parent = null;
//                 StoryCamera.SetActive(true);
//                 LevelsManager.instance.finishLineTarget  = gameObject;

//             }
//             if (other.gameObject.GetComponentInParent<PlayerPositionController>())
//             {
//                 other.gameObject.GetComponentInParent<PlayerPositionController>().playerPoisition = position;
//                 //Photon Removal  if (other.gameObject.GetComponentInParent<PhotonView>() && other.gameObject.GetComponentInParent<PhotonView>().IsMine )
//                 {
//                     //Photon Removal     other.gameObject.GetComponentInParent<PhotonView>().Owner.AddScore(5- position);
//                 }
//             }
//             else if (other.gameObject.GetComponent<PlayerPositionController>())
//             {
//                 other.gameObject.GetComponent<PlayerPositionController>().playerPoisition = position;
//                 //Photon Removal   if (other.gameObject.GetComponent<PhotonView>() && other.gameObject.GetComponent<PhotonView>().IsMine)
//                 {
//                     //Photon Removal    other.gameObject.GetComponent<PhotonView>().Owner.AddScore(5 - position);
//                 }
//             }
//             position++;
           
//         }

	
// 	}

// }

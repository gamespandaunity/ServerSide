using CarRace;

using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensions;
using static events;

public class HighwayCarNetwork : NetworkBehaviour
{
    public static HighwayCarNetwork ins;
    [SerializeField] private RCC_CarControllerV3 carController;
    [SerializeField] private RCC_MobileButtons mobileControls;
    [SerializeField] private HR_CarCamera carCamera;
    private Rigidbody rigid;

    [Header("Synced Inputs")]
    [SyncVar] public float accelerationInput;
    [SyncVar] public float brakeForceInput;
    [SyncVar] public float steeringLeftInput;
    [SyncVar] public float steeringRightInput;
    [SyncVar] public float steeringInput;
    [SyncVar] public float gyroscopeInput;
    [SyncVar] public float joystickDirectionInput;
    //  [SyncVar(hook = nameof(OnPlayerNameChanged))] public string PlayerName = "";
    //  [SyncVar] public string playerCountry = "";
    [SyncVar] public string playerID = "";

    [Header("Lane")]
    [Tooltip("Server spawn ke waqt lane assign karta hai. Reset hone par car isi lane me wapas aati hai, taake dono cars ek dusre ke upar spawn na hon.")]
    [SyncVar] public float resetLaneX = 0f;
    [SyncVar] public int laneIndex = 0;

    /// <summary>
    /// Scene me maujood saari networked cars. Reset ke waqt overlap check karne ke liye.
    /// </summary>
    public static readonly List<HighwayCarNetwork> Cars = new List<HighwayCarNetwork>();


    [Tooltip("Kitni baar position server ko bhejni hai (per second)")]
    public float positionUpdateRate = 10f;
    private float positionUpdateTimer = 0f;
    private Vector3 lastSentPosition;
    private float positionChangeThreshold = 5f; // 5 units change pe update

    // public ResultManagerForHorse ResultManagerHorse;
    // public Text nameText;
    [SyncVar(hook = nameof(SyncHeadlights))]
    public bool headlightsOn = false;
    [SerializeField]private MyMiniMapController MiniMapController;
    private HR_PlayerHandler playerHandler;
    // ✅ Add this method anywhere in the class
    public void ToggleHeadlights()
    {
        if (!isOwned) return;

        headlightsOn = !headlightsOn;

        // Apply locally
        if (carController != null)
        {
            carController.highBeamHeadLightsOn = headlightsOn;
            carController.lowBeamHeadLightsOn = headlightsOn;
        }

        // Sync to network
        CmdSyncHeadlights(headlightsOn);
    }

    [Command]
    private void CmdSyncHeadlights(bool state)
    {
        headlightsOn = state;
    }

    private void SyncHeadlights(bool oldState, bool newState)
    {
        if (carController != null)
        {
            carController.highBeamHeadLightsOn = newState;
            carController.lowBeamHeadLightsOn = newState;
        }
    }

    private void Awake()
    {
        ins = this;

        if (!Cars.Contains(this))
            Cars.Add(this);

        carController = GetComponent<RCC_CarControllerV3>();
        rigid = GetComponent<Rigidbody>();
        playerHandler = GetComponent<HR_PlayerHandler>();
        // ResultManagerHorse = FindAnyObjectByType<ResultManagerForHorse>(FindObjectsInactive.Include);

        FixHoodCameraToCar();
    }

    /// <summary>
    /// Hood camera RCC me ek alag rigidbody hai jo ConfigurableJoint se car ke sath juri hoti hai,
    /// taake tez raftaar par halka sa jhatka de. Single player me car sirf local physics se chalti
    /// hai to joint apni limit ke andar rehta hai aur camera fixed lagta hai. Multiplayer me car ka
    /// transform network se set hota hai, aur transform seedha set karne par child ka rigidbody us
    /// ke sath teleport nahi hota - wo peeche reh jata hai aur speed kam hone par joint use wapas
    /// kheench laata hai.
    ///
    /// Joint aur rigidbody hata dene se hood camera sirf ek child transform reh jata hai, yani car
    /// ke sath sakhti se juda - wahi behaviour jo AI mode me milta hai. RCC khud bhi connectedBody
    /// na milne par yahi karta hai, dekho RCC_HoodCamera.CheckConnecter().
    /// </summary>
    private void FixHoodCameraToCar()
    {

        RCC_HoodCamera hoodCamera = GetComponentInChildren<RCC_HoodCamera>(true);

        if (hoodCamera == null)
            return;

        //	Joint pehle, kyunki wo rigidbody ko reference karta hai.
        ConfigurableJoint joint = hoodCamera.GetComponent<ConfigurableJoint>();

        if (joint != null)
            Destroy(joint);

        Rigidbody hoodRigid = hoodCamera.GetComponent<Rigidbody>();

        if (hoodRigid != null)
            Destroy(hoodRigid);

    }

    private void Start()
    {
        if (isOwned)
        {
            // PlayerName = staticVariables.UserProfiledata.user.first_name + " " + 
            //              staticVariables.UserProfiledata.user.last_name;
            // playerCountry = staticVariables.UserProfiledata.user.country;
            //	GameObject.Find sirf active object dhoondta hai, aur us par GetComponent null
            //	par NRE de deta tha. Reconnect par ye canvas hamesha active nahi hota, aur wo NRE
            //	neeche wali SavePositionPeriodically ko shuru hi nahi hone deta tha - yani agle
            //	reconnect ke liye position save hi nahi hoti thi. Update() null par dobara dhoond
            //	leta hai, is liye yahan miss ho jana bhi theek hai.
            GameObject gameplayCanvas = GameObject.Find("HR_GameplayCanvas");

            if (gameplayCanvas != null)
                mobileControls = gameplayCanvas.GetComponent<RCC_MobileButtons>();

            MiniMapController = FindObjectOfType<MyMiniMapController>();

            if (MiniMapController != null)
                MiniMapController.SetTarget(transform);
            // Start saving position periodically
            StartCoroutine(SavePositionPeriodically());
        }
        else
        {
            MiniMapController = FindObjectOfType<MyMiniMapController>();

            if (MiniMapController != null)
                MiniMapController.SetOpponent(transform);
        }
    }


    public override void OnStartClient()
    {
        base.OnStartClient();

        if (isOwned)
        {
            HR_PlayerHandler handler = GetComponent<HR_PlayerHandler>();

            //	Auto-register band hai (dekho HR_NetworkManager.Awake), is liye apni car khud ko
            //	RCC player vehicle banati hai. Camera aur uska hood camera isi car se resolve honge,
            //	chahe Target_AssignCar late aaye ya reconnect ke baad aaye.
            if (carController != null && RCC_SceneManager.Instance != null
                && RCC_SceneManager.Instance.activePlayerVehicle != carController)
            {
                RCC.RegisterPlayerVehicle(carController);
            }

            //	Reconnect par MirrorPlayerPrefab abhi respawn na hua ho to ye NRE deta tha, aur
            //	us NRE ki wajah se neeche wala position/score restore chalta hi nahi tha - car
            //	start line par reh jati thi. Wahi id staticVariables me bhi hoti hai, dekho
            //	MirrorPlayerPrefab.RunLocalPlayerSetup, is liye key dono taraf ek jaisi rehti hai.
            playerID = ResolveLocalPlayerId();

            // Restore position if reconnecting
            if (HR_NetworkManager.HighWayCarData.ContainsKey(playerID))
            {
                HR_NetworkManager.HighWayCarRecord carData =
                HR_NetworkManager.HighWayCarData[playerID];

                transform.position = carData.Position;
                transform.rotation = carData.Rotation;
                playerHandler.score = carData.Score;
                Debug.Log($"✅ Restored car position for player {playerID}: {carData.Position}");
                if (rigid != null)
                {
                    rigid.linearVelocity = carData.Velocity;           // ✅ important
                    rigid.angularVelocity = carData.AngularVelocity;
                }

                // ✅ Restore HR_PlayerHandler state
                if (playerHandler != null)
                {
                    playerHandler.score = carData.Score;
                }
                StartCoroutine(SyncRestoredPositionToServer(carData));
                //  carController.GetComponent<CarModification>().SetPlayerNameInNumerPlate(staticVariables.UserProfiledata.user.first_name);
            }
            
        }
        if (!isOwned)
        {
            CarModification modification = carController != null
                ? carController.GetComponent<CarModification>()
                : null;

            if (modification != null)
                modification.ChangeColor(1);

            MiniMapController = FindObjectOfType<MyMiniMapController>();

            if (MiniMapController != null)
                MiniMapController.SetOpponent(transform);
            //  carController.GetComponent<CarModification>().SetPlayerNameInNumerPlate(staticVariables.OpponetProfile.userName);
        }
    }

    /// <summary>
    /// Car ki position isi id par save aur restore hoti hai, is liye id ka milna zaroori hai -
    /// MirrorPlayerPrefab us waqt maujood ho ya na ho.
    /// </summary>
    private static string ResolveLocalPlayerId()
    {
        MirrorPlayerPrefab localPlayer = MirrorPlayerPrefab.Instance;

        if (localPlayer != null && localPlayer.playerData != null &&
            !string.IsNullOrEmpty(localPlayer.playerData.playerId))
            return localPlayer.playerData.playerId;

        UserModel profile = staticVariables.UserProfiledata;

        return profile != null && profile.user != null ? profile.user._id.ToString() : string.Empty;
    }

    private IEnumerator SyncRestoredPositionToServer(HR_NetworkManager.HighWayCarRecord carData)
    {
        yield return new WaitForEndOfFrame();
        CmdRestorePosition(carData.Position, carData.Rotation,
                           carData.Velocity, carData.AngularVelocity);
    }

    [Command]
    private void CmdRestorePosition(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
    {
        transform.position = pos;
        transform.rotation = rot;

        if (rigid != null)
        {
            rigid.linearVelocity = vel;
            rigid.angularVelocity = angVel;
        }

        Debug.Log($"🔄 Server updated car position for reconnected player: {pos}");
        MatchFlow.Log("Highway Racer", $"{HR_NetworkManager.FlowWho(netIdentity)} restored position after reconnect (z {pos.z:F0} m)");
        RpcForcePositionSync(pos, rot, vel, angVel);
    }

    [ClientRpc]
    private void RpcForcePositionSync(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
    {
        if (!isOwned)
        {
            transform.position = pos;
            transform.rotation = rot;

            if (rigid != null)
            {
                rigid.linearVelocity = vel;
                rigid.angularVelocity = angVel;
            }
        }
    }

    private IEnumerator SavePositionPeriodically()
    {
        while (true&&!ResultManager.isGameFinished)
        {
            yield return new WaitForSeconds(0.3f);
            SaveCurrentPosition();
        }
    }

    private void SaveCurrentPosition()
    {
        if (string.IsNullOrEmpty(playerID)) return;

        HR_NetworkManager.HighWayCarRecord record = new HR_NetworkManager.HighWayCarRecord
        {
            Position = transform.position,
            Rotation = transform.rotation,
            Velocity = rigid != null ? rigid.linearVelocity : Vector3.zero,
            AngularVelocity = rigid != null ? rigid.angularVelocity : Vector3.zero  ,
            Score = playerHandler != null ? playerHandler.score : 0 // ✅ Save score
        };

        HR_NetworkManager.HighWayCarData[playerID] = record;
    }

    private void Update()
    {
        if (!carController) return;

        if (isOwned)
        {

            // ✅ Position sync for traffic management
            SyncPositionForTraffic();

            // if(carCamera==null)
            // {
            //     SetupCameraDirectly();
            //     RCC_Customization.LoadStats(this.gameObject.GetComponent<RCC_CarControllerV3>());
            // }

            if (mobileControls == null)
            {
                mobileControls = FindObjectOfType<RCC_MobileButtons>();
            }
            else
            {
                float acc = mobileControls.GetAccelerationInput();
                float brake = mobileControls.GetBrakeInput();
                float steerL = mobileControls.GetSteerLeftInput();
                float steerR = mobileControls.GetSteerRightInput();
                float steer = mobileControls.GetSteeringInput();
                float gyro = mobileControls.GetGyroscopeInput();
                float joyDir = mobileControls.GetJoystickDirectionInput();

                CmdSyncCarInputs(acc, brake, steerL, steerR, steer, gyro, joyDir);
                ApplyInputs(acc, brake, steerL, steerR, steer, gyro, joyDir);

                //   PlayerName = staticVariables.UserProfiledata.user.first_name + " " +
                //                staticVariables.UserProfiledata.user.last_name;
                //    playerCountry = staticVariables.UserProfiledata.user.country;

                //    CmdSetPlayerName(PlayerName, playerCountry);
            }
            carController.GetComponent<CarModification>().SetPlayerNameInNumerPlate(staticVariables.UserProfiledata.user.first_name);
        }
        else
        {
            ApplyInputs(accelerationInput, brakeForceInput, steeringLeftInput,
                       steeringRightInput, steeringInput, gyroscopeInput, joystickDirectionInput);
            carController.GetComponent<CarModification>().SetPlayerNameInNumerPlate(staticVariables.OpponetProfile.userName);
        }
    }

    /// <summary>
    /// ✅ Sync position to server for traffic management
    /// </summary>
    private void SyncPositionForTraffic()
    {
        positionUpdateTimer += Time.deltaTime;

        // Check if enough time has passed OR position changed significantly
        if (positionUpdateTimer >= (1f / positionUpdateRate) ||
            Vector3.Distance(transform.position, lastSentPosition) > positionChangeThreshold)
        {
            CmdSendPositionForTraffic(transform.position);
            lastSentPosition = transform.position;
            positionUpdateTimer = 0f;
        }
    }

    /// <summary>
    /// ✅ CLIENT -> SERVER: Position bhejo for traffic spawning
    /// </summary>
    [Command]
    private void CmdSendPositionForTraffic(Vector3 position)
    {
        // Server receives client position and registers it for traffic
        if (HR_TrafficPooling.Instance != null)
        {
            HR_TrafficPooling.Instance.RegisterPlayerPosition(netId, position);
        }
    }

    [Command]
    private void CmdSyncCarInputs(float acc, float brake, float steerL,
                                  float steerR, float steer, float gyro, float joyDir)
    {
        accelerationInput = acc;
        brakeForceInput = brake;
        steeringLeftInput = steerL;
        steeringRightInput = steerR;
        steeringInput = steer;
        gyroscopeInput = gyro;
        joystickDirectionInput = joyDir;
    }

    private void ApplyInputs(float acc, float brake, float steerL,
                            float steerR, float steer, float gyro, float joyDir)
    {
        if (!carController) return;

        // Apply inputs to RCC car controller
        carController.throttleInput = acc;
        carController.brakeInput = brake;
        carController.steerInput = -steerL + steerR + steer + gyro + joyDir;
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (HR_NetworkManager.Instance == null || other.GetComponent<TriggerEvent>() == null)
            return;

        Debug.Log($"[HighwayServerResult] Server finish trigger hit by netId={netId}.");
        MatchFlow.Log("Highway Racer", $"{HR_NetworkManager.FlowWho(netIdentity)} crossed the finish line (score {(playerHandler != null ? Mathf.FloorToInt(playerHandler.score) : 0)}, {(playerHandler != null ? playerHandler.distance : 0f):F2} km)");
        ServerReachedFinishLine();
    }

    [Command]
    public void CmdPlayerFinished(int PlayerId)
    {
        MatchFlow.Log("Highway Racer", $"{HR_NetworkManager.FlowWho(netIdentity)} reports reaching the finish");
        ServerValidateFinishRequest();
    }

    [Server]
    private bool ServerValidateFinishRequest()
    {
        if (HR_NetworkManager.Instance == null || !HR_NetworkManager.hasHighWayGameStarted)
        {
            Debug.LogWarning($"[HighwayServerResult] Finish request rejected before race start, netId={netId}.");
            MatchFlow.Log("Highway Racer", $"finish of {HR_NetworkManager.FlowWho(netIdentity)} rejected — race not started");
            MatchFlow.Flag("Highway Racer", connectionToClient, "finish_before_start", "claimed the finish before the race started");
            return false;
        }

        TriggerEvent finish = TriggerEvent.instance;
        Collider finishCollider = finish != null ? finish.GetComponent<Collider>() : null;
        if (finishCollider == null)
        {
            Debug.LogError("[HighwayServerResult] Server finish collider was not found.");
            return false;
        }

        Vector3 closestPoint = finishCollider.ClosestPoint(transform.position);
        float distanceFromFinish = Vector3.Distance(transform.position, closestPoint);
        if (distanceFromFinish > 35f)
        {
            Debug.LogWarning($"[HighwayServerResult] Remote finish request rejected: netId={netId}, distance={distanceFromFinish:F1}m.");
            MatchFlow.Log("Highway Racer", $"finish of {HR_NetworkManager.FlowWho(netIdentity)} rejected — {distanceFromFinish:F0} m from the line");
            MatchFlow.Flag("Highway Racer", connectionToClient, "finish_too_far", $"claimed the finish {distanceFromFinish:F0} m from the line");
            return false;
        }

        Debug.Log($"[HighwayServerResult] Finish request verified by server: netId={netId}, distance={distanceFromFinish:F1}m.");
        return ServerReachedFinishLine();
    }

    [Server]
    public bool ServerReachedFinishLine()
    {
        return HR_NetworkManager.Instance != null &&
               HR_NetworkManager.Instance.ServerDeclareWinner(netIdentity);
    }

    [Server]
    public bool ServerWasEliminated()
    {
        MatchFlow.Log("Highway Racer", $"{HR_NetworkManager.FlowWho(netIdentity)} eliminated (score {(playerHandler != null ? Mathf.FloorToInt(playerHandler.score) : 0)}, {(playerHandler != null ? playerHandler.distance : 0f):F2} km)");
        return HR_NetworkManager.Instance != null &&
               HR_NetworkManager.Instance.ServerDeclareLoser(netIdentity);
    }

    [ClientRpc]
    void RpcActivateUIAndHandleResult(bool didWin, string playerId)
    {
        Debug.Log("RpcActivateUIAndHandleResult");
        if (HR_NetworkManager.Instance != null)
            HR_NetworkManager.Instance.ShowServerWinner(playerId);
    }
    [Command]
    public void CmdWinText(int PlayerId)
    {
        // PlayerId is intentionally ignored. The server derives the player from
        // this owned car's connection after validating its synced position.
        MatchFlow.Log("Highway Racer", $"{HR_NetworkManager.FlowWho(netIdentity)} reports reaching the finish");
        ServerValidateFinishRequest();
    }

    [ClientRpc]
    void RpcWinText(string playerId)
    {
        if (playerId == staticVariables.UserProfiledata.user._id.ToString())
        {
            TriggerEvent.instance.ResultImage.sprite=TriggerEvent.instance.WinImage;
        }
        else
        {
             TriggerEvent.instance.ResultImage.sprite = TriggerEvent.instance.LoseImage;
        }
    }


    // [Command]
    // private void CmdSetPlayerName(string name, string country)
    // {
    //     PlayerName = name;
    //     playerCountry = country;
    // }

    // private void OnPlayerNameChanged(string oldName, string newName)
    // {
    //     if (nameText != null)
    //         nameText.text = newName;
    // }


    private void OnDestroy()
    {
        Cars.Remove(this);
        if (isServer)
            MatchFlow.Log("Highway Racer", $"{HR_NetworkManager.FlowWho(netIdentity)}'s car removed — left or disconnected (score {(playerHandler != null ? Mathf.FloorToInt(playerHandler.score) : 0)}, {(playerHandler != null ? playerHandler.distance : 0f):F2} km)");

        if (isOwned)
        {
            // ✅ Unregister position from traffic system
            if (isServer && HR_TrafficPooling.Instance != null)
            {
                HR_TrafficPooling.Instance.UnregisterPlayerPosition(netId);
            }

            Debug.Log($"💾 Saved car position on disconnect for player {playerID}");
        }
    }

    private void OnDisable()
    {
        MirrorNetwork.OnWinCall -= AnnounceWinner;


    }

    private void OnEnable()
    {
        MirrorNetwork.OnWinCall += AnnounceWinner;
    }

    // [Command]
    // public void CmdPlayerFinished(int PlayerId)
    // {
    //     Debug.Log("CmdPlayerFinished - Car");
    //     RpcActivateUIAndHandleResult(true, PlayerId.ToString());
    // }

    // [ClientRpc]
    // void RpcActivateUIAndHandleResult(bool didWin, string playerId)
    // {
    //     Debug.Log("RpcActivateUIAndHandleResult - Car");
    //     FinishPointMechanism.ins.winLoseHorse.gameObject.SetActive(true);
    //     if (FinishPointMechanism.ins.winLoseHorse != null)
    //     {
    //         FinishPointMechanism.ins.winLoseHorse.GetComponent<ResultManagerForHorse>()
    //             .WinPlayer(didWin, playerId);
    //     }
    // }

    private void AnnounceWinner(string reason)
    {
        Debug.Log("Highway disconnect result is waiting for the server/backend decision.");
        CmdAnnounceVictory(staticVariables.UserProfiledata.user._id, reason);
    }
    [Command(requiresAuthority = false)]
    public void CmdAnnounceVictory(int playerId, string reaosn)
    {
        MatchFlow.Log("Highway Racer", $"victory announced: {MatchFlow.Who(playerId.ToString())} — {reaosn}");
        MatchFlow.SendResult(playerId.ToString(), "victory announced: " + reaosn);
        ApiAndRoomManager._instance.WinnerLossChallenge(playerId.ToString());
        RpcAnnounceVictory(playerId, reaosn);
        NetworkGameManager.Instance.creatorData.Scores = 0;
        NetworkGameManager.Instance.joinerData.Scores = 0;
     
        this.Delay(5, () =>
        {
            "Destroy After Delay".Show();
            NetworkGameManager.Instance.currentServerRequestId.Show("Current Server Id");
            MirrorNetwork.Instance.edgegapAPIClient.CleanupServer(NetworkGameManager.Instance.currentServerRequestId);
            //RpcCleanUp(NetworkGameManager.Instance.requestId);
        });
    }

    [ClientRpc]
    public void RpcAnnounceVictory(int playerId, string reason)
    {
        Victory(playerId, reason);

    }
    
    public void Victory(int id, string reason)
    {
        
        if (ResultManager.isGameFinished == false)
        {
            PopupMessageManager.instance.SetPanelStaus(false, body: "You have been disconnected. The opponent is declared the winner.");
            PopupMessageManager.instance.waitingPanel.StopTimer();
            PopupMessageManager.instance.disconnectedPanel.PanelStatus(false);
            PopupMessageManager.instance.slowInternetPanel.PanelStatus(false);
            ResultManager.isGameFinished = true;
            
                // ResultManagerForSnooker.instance.HandleGameResultAlt(true, id.ToString());
                if (ResultManager.GameSpawnedFinished == false)
                {
                    ResultManager.GameSpawnedFinished = true;
                    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");
                    if (enemyPrefab != null)
                    {
                        "3".Show();
                        // Spawn at position (0,0,0)
                        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                        gameObject.GetComponent<ResultManager>().HandleGameResultAltMultiplayer(true, id.ToString());
                    }
                    else
                    {
                        Debug.LogError("WinLose GameManager prefab not found!");
                    }
                }
                ConstantsData_M.Log(true + "1");
           
        }
    }
}

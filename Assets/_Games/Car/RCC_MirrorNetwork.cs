namespace CarRace
{
    //----------------------------------------------
    //            Realistic Car Controller
    //
    // Copyright © 2014 - 2022 BoneCracker Games
    // http://www.bonecrackergames.com
    // Buğra Özdoğanlar
    //
    //----------------------------------------------

    using Mirror;
    using System.Collections;
    using TMPro;
    using UnityEngine;
    using static CarRace.MultiPlayerGameManager;

    /// <summary>
    /// Streaming player input, or receiving data from server. And then feeds the RCC.
    /// </summary>
    [RequireComponent(typeof(PlayerSetup))]
    [RequireComponent(typeof(RCC_CarControllerV3))]
    [RequireComponent(typeof(NetworkTransformReliable))] // Mirror's built-in transform sync
    [AddComponentMenu("BoneCracker Games/Realistic Car Controller/Network/Mirror/RCC Mirror Network")]
    public class RCC_MirrorNetwork : NetworkBehaviour
    {
        // Main RCC, Rigidbody, and Wheel Colliders
        private RCC_CarControllerV3 carController;
        private RCC_WheelCollider[] wheelColliders;
        private Rigidbody rigid;

        // Synced variables for all inputs
        [SyncVar] private float gasInput = 0f;
        [SyncVar] private float brakeInput = 0f;
        [SyncVar] private float steerInput = 0f;
        [SyncVar] private float handbrakeInput = 0f;
        [SyncVar] private float boostInput = 0f;
        [SyncVar] private float clutchInput = 0f;
        [SyncVar] private int gear = 0;
        [SyncVar] private int direction = 1;
        [SyncVar] private bool changingGear = false;
        [SyncVar] private bool semiAutomaticGear = false;
        [SyncVar] private float fuelInput = 1f;
        [SyncVar] private bool engineRunning = false;

        // Lights
        [SyncVar] private bool lowBeamHeadLightsOn = false;
        [SyncVar] private bool highBeamHeadLightsOn = false;

        // For Indicators
        [SyncVar] private RCC_CarControllerV3.IndicatorsOn indicatorsOn;

        // For player name display
        [SyncVar(hook = nameof(OnPlayerNameChanged))]
        public string playerName = "";
        [SyncVar]
        public string playerID = "";

        // For Nickname Text
        private TextMeshProUGUI nicknameText2;
        GameObject PlayerNameUI;
        bool hasStopped = false;
        private PlayerSetup playerSetup;

        // Update interval for sending data
        private float sendRate = 0.05f;
        private float lastSendTime = 0f;
        public bool isRestoreComplete = false;
        // QA 29 Oct (car stuck at random points after a reconnect): this runs every time the shared waiting
        // panel opens or closes (NetworkGameManager.ShowPausePanel), and after a reconnect the server opens and
        // closes it again and again while the link settles. The old ResetRigidbody coroutine first set the
        // OPPOSITE state for 0.5 s, so every resume froze the car for half a second, and when a pause and a resume
        // came within 0.5 s of each other the older coroutine finished last and left the car kinematic - stuck
        // until the next panel. Apply the requested state once, and keep the speed the car had so a resume does
        // not start from a dead stop.
        private Vector3 pausedVelocity;
        private Vector3 pausedAngularVelocity;

        public void StartStopGame(bool isstop)
        {
            if (!rigid)
                rigid = GetComponent<Rigidbody>();
            if (!rigid || !isOwned)
                return;

            if (isstop)
            {
                if (rigid.isKinematic)
                    return;
                pausedVelocity = rigid.linearVelocity;
                pausedAngularVelocity = rigid.angularVelocity;
                rigid.isKinematic = true;
                return;
            }

            if (!rigid.isKinematic)
                return;
            rigid.isKinematic = false;
            rigid.linearVelocity = pausedVelocity;
            rigid.angularVelocity = pausedAngularVelocity;
            pausedVelocity = Vector3.zero;
            pausedAngularVelocity = Vector3.zero;
        }
        void OnDestroy()
        {
            // Static-event leak fix: StartStopGame (above) is subscribed to the STATIC
            // NetworkGameManager.StartStopGame action in OnStartClient. Without unsubscribing, this dead
            // handler keeps firing after the Car scene unloads; in another game (e.g. Ludo) its
            // GetComponent on the now-destroyed object throws a NullReferenceException, which aborts that
            // game's SyncVar deserialize (corrupting reconnect state). Unsubscribe so it can't leak.
            NetworkGameManager.StartStopGame -= StartStopGame;
            if (NetworkServer.active)
                MatchFlow.Log("Car Race", $"{(string.IsNullOrEmpty(playerName) ? "a player" : playerName)}'s car removed — left or disconnected");
        }
        void Awake()
        {
            DisableDuplicateTransformSync();
        }

        // Set when the prefab's NetworkRigidbodyReliable was switched off: it used to keep the remote car kinematic.
        private bool keepRemoteKinematic;

        /// <summary>
        /// QA 29 Oct (car stuck at points, then jumping on): 1Car20 carries a NetworkRigidbodyReliable AND a plain
        /// NetworkTransformReliable on the same transform. The NetworkRigidbodyReliable has syncInterval 1.2 s, so
        /// besides the plain component's smooth stream a second one went out once every 1.2 s, and on the other
        /// phone (and the server) both wrote the transform every frame - whenever the slow one wrote last, the car
        /// was drawn at a pose up to 1.2 s old and then jumped forward. The plain one (interval 0, the one this
        /// class requires) stays; the slow one is switched off, on app and server alike. The component stays on
        /// the prefab so the NetworkBehaviour order Mirror relies on is unchanged. 2Car3 has only the plain one.
        /// </summary>
        private void DisableDuplicateTransformSync()
        {
            NetworkTransformReliable plainSync = null;
            foreach (NetworkTransformReliable sync in GetComponents<NetworkTransformReliable>())
            {
                if (sync.GetType() == typeof(NetworkTransformReliable))
                    plainSync = sync;
            }
            if (plainSync == null)
                return;

            foreach (NetworkRigidbodyReliable rigidbodySync in GetComponents<NetworkRigidbodyReliable>())
            {
                if (rigidbodySync.target == plainSync.target)
                {
                    rigidbodySync.enabled = false;
                    keepRemoteKinematic = true;
                }
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            carController = GetComponent<RCC_CarControllerV3>();
            wheelColliders = GetComponentsInChildren<RCC_WheelCollider>();
            rigid = GetComponent<Rigidbody>();

            // The server only follows the owner's synced pose (client authority), as the switched-off
            // NetworkRigidbodyReliable made sure of.
            if (keepRemoteKinematic && rigid != null && !NetworkClient.active)
                rigid.isKinematic = true;
        }
        public override void OnStartClient()
        {
            base.OnStartClient();

            carController = GetComponent<RCC_CarControllerV3>();
            wheelColliders = GetComponentsInChildren<RCC_WheelCollider>();
            rigid = GetComponent<Rigidbody>();
            playerSetup = GetComponent<PlayerSetup>();

            // QA 29 Oct (car stuck / jumping on the other phone): the car Rigidbody is set to Interpolate. On the
            // opponent's car that fights the NetworkTransform - with physics interpolation on, Unity drives the
            // transform from the physics pose and drops direct transform writes between physics steps, so the
            // remote car is drawn at its locally simulated pose (which runs into walls on its own) instead of
            // the synced one. Mirror already interpolates the remote car; switch Unity's off for it.
            if (!isOwned && rigid != null)
                rigid.interpolation = RigidbodyInterpolation.None;

            if (!isOwned)
            {
                SetupPlayerNameUI();
                carController.GetComponent<CarModification>().ChangeColor(1);
            }

            if (isOwned)
            {
                GetValues();
                NetworkGameManager.StartStopGame = null;
                NetworkGameManager.StartStopGame += StartStopGame;
            }

            gameObject.name = gameObject.name + "_" + netId;

            if (isOwned)
            {
                playerName = MirrorPlayerPrefab.Instance.playerData.playerName;
                playerID = MirrorPlayerPrefab.Instance.playerData.playerId;
                CmdSetPlayerName(playerName, playerID);

                // ✅ Restore pehle — save baad mein
                isRestoreComplete = false;

                if (PlayersCarsData.ContainsKey(playerID))
                {
                    CarRecord cardat = PlayersCarsData[playerID];
                    transform.position = cardat.Position;
                    transform.rotation = cardat.Rotation;
                    rigid.linearVelocity = cardat.Velocity;
                    rigid.angularVelocity = cardat.AngularVelocity;

                    Debug.Log($"[RCC_MirrorNetwork] ✅ Position restored — {cardat.Position}");
                }
                else
                {
                    Debug.LogWarning($"[RCC_MirrorNetwork] ⚠️ Fresh start — ID: {playerID}");
                    // ✅ Fresh start mein bhi restore complete mark karo
                    isRestoreComplete = true;
                }
            }
        }


        void SetupPlayerNameUI()
        {
            PlayerNameUI = Instantiate(Resources.Load("PlayerNameUI", typeof(GameObject))) as GameObject;
            PlayerNameUI.transform.SetParent(transform, false);
            PlayerNameUI.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            PlayerNameUI.transform.localScale = Vector3.one;
            nicknameText2 = PlayerNameUI.transform.Find("playerNameText").transform.GetComponent<TextMeshProUGUI>();
        }

        void GetValues()
        {
            if (!carController) return;

            gasInput = carController.throttleInput;
            brakeInput = carController.brakeInput;
            steerInput = carController.steerInput;
            handbrakeInput = carController.handbrakeInput;
            boostInput = carController.boostInput;
            clutchInput = carController.clutchInput;
            gear = carController.currentGear;
            direction = carController.direction;
            changingGear = carController.changingGear;
            semiAutomaticGear = carController.semiAutomaticGear;

            fuelInput = carController.fuelInput;
            engineRunning = carController.engineRunning;
            lowBeamHeadLightsOn = carController.lowBeamHeadLightsOn;
            highBeamHeadLightsOn = carController.highBeamHeadLightsOn;
            indicatorsOn = carController.indicatorsOn;
        }

        void FixedUpdate()
        {
            if (!NetworkClient.active || !carController) return;

            carController.externalController = !isOwned;
            if (!hasStopped)
                carController.canControl = isOwned;

            if (isOwned)
            {
                if (Time.time - lastSendTime > sendRate)
                {
                    GetValues();
                    CmdSendInputs(gasInput, brakeInput, steerInput, handbrakeInput,
                                 boostInput, clutchInput, gear, direction, changingGear,
                                 semiAutomaticGear, fuelInput, engineRunning,
                                 lowBeamHeadLightsOn, highBeamHeadLightsOn, indicatorsOn);
                    lastSendTime = Time.time;
                }

                // ✅ Sirf tab save karo jab restore complete ho chuka ho
                if (!ResultManagerForCar.IsGameWin && gameStarted && isRestoreComplete)
                {
                    CarRecord cardata = new CarRecord();
                    cardata.Position = transform.position;
                    cardata.Rotation = transform.rotation;
                    cardata.Velocity = rigid.linearVelocity;
                    cardata.AngularVelocity = rigid.angularVelocity;

                    if (playerSetup != null && playerSetup.checkWrongDirection != null)
                    {
                        cardata.LapCount = playerSetup.lapCount;
                        cardata.HasCrossedMiddleCheckPoint = playerSetup.hasCrossedMiddleCheckPoint;
                        cardata.CurrentWaypointIndex =
                            playerSetup.checkWrongDirection.currentCheckpointIndex;

                        if (Time.frameCount % 300 == 0)
                            Debug.Log($"[RCC_MirrorNetwork] 💾 Saving — " +
                                      $"Lap: {cardata.LapCount}, " +
                                      $"Waypoint: {cardata.CurrentWaypointIndex}, " +
                                      $"MiddleCrossed: {cardata.HasCrossedMiddleCheckPoint}");
                    }

                    PlayersCarsData[playerID] = cardata;
                }
            }
            else
            {
                // What the switched-off NetworkRigidbodyReliable did: the opponent's car is moved by the network
                // sync only, so local physics must not push it around.
                if (keepRemoteKinematic && rigid != null && !rigid.isKinematic)
                    rigid.isKinematic = true;
                ApplyInputsToCarController();
            }

            UpdatePlayerNameDisplay();
        }
        //IEnumerator RestoreAfterStart()
        //{
        //    // Start() run hone tak wait karo
        //    yield return new WaitUntil(() => playerSetup != null
        //                                  && playerSetup.checkWrongDirection != null);

        //    if (!PlayersCarsData.ContainsKey(playerID))
        //        yield break;

        //    CarRecord cardat = PlayersCarsData[playerID];

        //    // Lap restore
        //    playerSetup.lapCount = cardat.LapCount;
        //    playerSetup.hasCrossedMiddleCheckPoint = cardat.HasCrossedMiddleCheckPoint;

        //    // UI update
        //    //if (playerSetup.lapText != null)
        //    //    playerSetup.lapText.text = "Laps " + (playerSetup.lapCount + 1)
        //    //                             + "/" + playerSetup.totalLaps;

        //    // Trigger state restore
        //    playerSetup.StartCoroutine(
        //        playerSetup.ToggleLapTriggers(
        //            halfLap: !cardat.HasCrossedMiddleCheckPoint,
        //            finishLap: cardat.HasCrossedMiddleCheckPoint
        //        )
        //    );

        //    // ✅ Waypoint index restore
        //    playerSetup.checkWrongDirection.currentCheckpointIndex = cardat.CurrentWaypointIndex;

        //    Debug.Log($"✅ Restored — Lap: {cardat.LapCount}, " +
        //              $"MiddleCrossed: {cardat.HasCrossedMiddleCheckPoint}, " +
        //              $"Waypoint: {cardat.CurrentWaypointIndex}");
        //}
        void ApplyInputsToCarController()
        {
            carController.throttleInput = gasInput;
            carController.brakeInput = brakeInput;
            carController.steerInput = steerInput;
            carController.handbrakeInput = handbrakeInput;
            carController.boostInput = boostInput;
            carController.clutchInput = clutchInput;
            carController.currentGear = gear;
            carController.direction = direction;
            carController.changingGear = changingGear;
            carController.semiAutomaticGear = semiAutomaticGear;

            carController.fuelInput = fuelInput;
            carController.engineRunning = engineRunning;
            carController.lowBeamHeadLightsOn = lowBeamHeadLightsOn;
            carController.highBeamHeadLightsOn = highBeamHeadLightsOn;
            carController.indicatorsOn = indicatorsOn;
        }

        void UpdatePlayerNameDisplay()
        {
            if (nicknameText2)
            {
                nicknameText2.text = playerName;
                PlayerNameUI.SetActive(true);

                if (RCC_SceneManager.Instance.activeMainCamera)
                {
                    PlayerNameUI.transform.LookAt(RCC_SceneManager.Instance.activeMainCamera.transform);
                    PlayerNameUI.transform.rotation = Quaternion.Euler(
                        PlayerNameUI.transform.eulerAngles.x,
                        PlayerNameUI.transform.eulerAngles.y + 180f,
                        PlayerNameUI.transform.eulerAngles.z);
                }
            }


        }

        [Command]
        void CmdSendInputs(float gas, float brake, float steer, float handbrake,
                          float boost, float clutch, int gearVal, int dir,
                          bool changingGearVal, bool semiAuto, float fuel,
                          bool engineOn, bool lowBeam, bool highBeam,
                          RCC_CarControllerV3.IndicatorsOn indicators)
        {
            gasInput = gas;
            brakeInput = brake;
            steerInput = steer;
            handbrakeInput = handbrake;
            boostInput = boost;
            clutchInput = clutch;
            gear = gearVal;
            direction = dir;
            changingGear = changingGearVal;
            semiAutomaticGear = semiAuto;
            fuelInput = fuel;
            engineRunning = engineOn;
            lowBeamHeadLightsOn = lowBeam;
            highBeamHeadLightsOn = highBeam;
            indicatorsOn = indicators;
        }

        [Command]
        void CmdSetPlayerName(string name, string id)
        {
            playerName = name;
            MatchFlow.Log("Car Race", $"{(string.IsNullOrEmpty(name) ? "a player" : name)} joined the race");
        }

        void OnPlayerNameChanged(string oldName, string newName)
        {
            playerName = newName;
        }

        public void Stop()
        {
            carController.SetCanControl(false);
            hasStopped = true;

            if (isLocalPlayer)
            {
                CmdStop();
            }
        }

        [Command]
        void CmdStop()
        {
            RpcStop();
        }

        [ClientRpc]
        void RpcStop()
        {
            if (carController)
            {
                carController.SetCanControl(false);
                hasStopped = true;
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            if (PlayerNameUI)
            {
                Destroy(PlayerNameUI);
            }
        }
    }
}

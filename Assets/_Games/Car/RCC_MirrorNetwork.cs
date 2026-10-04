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
        public void StartStopGame(bool isstop)
        {
            if (!rigid)
                rigid = GetComponent<Rigidbody>();

            rigid.isKinematic = isstop;
            StartCoroutine(ResetRigidbody(isstop));
        }
        IEnumerator ResetRigidbody(bool state)
        {
            rigid.isKinematic = !state;
            yield return new WaitForSecondsRealtime(0.5f);
            rigid.isKinematic = state;

        }
        void OnDestroy()
        {
            // Static-event leak fix: StartStopGame (above) is subscribed to the STATIC
            // NetworkGameManager.StartStopGame action in OnStartClient. Without unsubscribing, this dead
            // handler keeps firing after the Car scene unloads; in another game (e.g. Ludo) its
            // GetComponent on the now-destroyed object throws a NullReferenceException, which aborts that
            // game's SyncVar deserialize (corrupting reconnect state). Unsubscribe so it can't leak.
            NetworkGameManager.StartStopGame -= StartStopGame;
        }
        public override void OnStartServer()
        {
            base.OnStartServer();
            carController = GetComponent<RCC_CarControllerV3>();
            wheelColliders = GetComponentsInChildren<RCC_WheelCollider>();
            rigid = GetComponent<Rigidbody>();

        }
        public override void OnStartClient()
        {
            base.OnStartClient();

            carController = GetComponent<RCC_CarControllerV3>();
            wheelColliders = GetComponentsInChildren<RCC_WheelCollider>();
            rigid = GetComponent<Rigidbody>();
            playerSetup = GetComponent<PlayerSetup>();

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

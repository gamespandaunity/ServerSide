namespace CarRace
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;


    using TMPro;
    using System;
    //using ExitGames.Client.Photon;
    using Mirror;

    /// <summary>
    /// this cript is used in Online Mode with player 
    /// </summary>
    public class PlayerSetup : NetworkBehaviour
    {
        TextMeshProUGUI plateNumber;
        NetworkIdentity identity;
       public CheckWrongDirection_M checkWrongDirection;
        MultiPlayerGameManager gameManager;
        BoxCollider halfLapTrigger;
        BoxCollider finishLapTrigger;
       public bool hasCrossedMiddleCheckPoint = true;
        int totalLaps = 2;
      public  int lapCount = 0;
        TextMeshProUGUI lapText;
        bool isGameFinished = false;
        float matchStartTime;
        float matchEndTime;
        GameObject positionIndicator;
        void Start()
        {
            identity = GetComponent<NetworkIdentity>();                                                                 //Photon Removal
            plateNumber = transform.GetComponentInChildren<TextMeshProUGUI>();
            if (isOwned)
            {
                if (plateNumber)
                    plateNumber.text = staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name;
                SetUpUserLocalPlayerSettings();
            }
            else
            {
                plateNumber.text = staticVariables.OpponetProfile.userName;
            }
        }
        void SetUpUserLocalPlayerSettings()
        {
            bool useDamage = System.Convert.ToBoolean(PlayerPrefs.GetInt(PPConst.Damage));
            gameManager = FindAnyObjectByType<MultiPlayerGameManager>();

            if (gameManager == null)
            {
                Debug.LogError("[PlayerSetup] ❌ MultiPlayerGameManager not found!");
                return;
            }

            gameManager.SetUpUIForCar(useDamage);
            halfLapTrigger = gameManager.halfLapTrigger;
            finishLapTrigger = gameManager.finishLapTrigger;
            lapText = gameManager.lapText;

            checkWrongDirection = gameObject.AddComponent<CheckWrongDirection_M>();
            checkWrongDirection.wrongDirectionCheckpoints = gameManager.waypoints;
            checkWrongDirection.wrongWayPopUp = gameManager.wrongWayPopUp;

            // ✅ Restore karo — returns true if data mila, false if fresh start
            bool wasRestored = RestoreSavedData();

            lapText.text = "" + (lapCount + 1) + "/" + totalLaps;

            if (wasRestored)
            {
                // ✅ Reconnect — saved trigger state use karo
                Debug.Log($"[PlayerSetup] 🔄 Reconnect — " +
                          $"halfLap: {!hasCrossedMiddleCheckPoint}, " +
                          $"finishLap: {hasCrossedMiddleCheckPoint}");

                StartCoroutine(ToggleLapTriggers(
                    halfLap: !hasCrossedMiddleCheckPoint,
                    finishLap: hasCrossedMiddleCheckPoint
                ));
            }
            else
            {
                // ✅ Fresh start — original default state
                Debug.Log("[PlayerSetup] 🆕 Fresh start — halfLap: true, finishLap: false");
                StartCoroutine(ToggleLapTriggers(halfLap: true, finishLap: false));
            }

            int colorNum = UnityEngine.Random.Range(0, 3);
        }

        // ✅ bool return karo — true = data mila, false = fresh start
        bool RestoreSavedData()
        {
            if (MirrorPlayerPrefab.Instance == null)
            {
                Debug.LogError("[PlayerSetup] ❌ MirrorPlayerPrefab.Instance null hai!");
                return false;
            }

            string myPlayerID = MirrorPlayerPrefab.Instance.playerData.playerId;

            Debug.Log($"[PlayerSetup] 🔍 Restore check — PlayerID: {myPlayerID}, " +
                      $"Dictionary entries: {MultiPlayerGameManager.PlayersCarsData.Count}");

            if (!MultiPlayerGameManager.PlayersCarsData.ContainsKey(myPlayerID))
            {
                Debug.LogWarning("[PlayerSetup] 🆕 Fresh start");

                // ✅ Fresh start mein RCC_MirrorNetwork ko batao save shuru karo
                GetComponent<RCC_MirrorNetwork>().isRestoreComplete = true;
                return false;
            }

            MultiPlayerGameManager.CarRecord saved =
                MultiPlayerGameManager.PlayersCarsData[myPlayerID];

            lapCount = saved.LapCount;
            hasCrossedMiddleCheckPoint = saved.HasCrossedMiddleCheckPoint;
            Debug.Log($"[PlayerSetup] ✅ Lap restored — lapCount: {lapCount}, " +
                      $"hasCrossedMiddle: {hasCrossedMiddleCheckPoint}");

            checkWrongDirection.currentCheckpointIndex = saved.CurrentWaypointIndex;
            Debug.Log($"[PlayerSetup] ✅ Waypoint restored — index: {saved.CurrentWaypointIndex}");

            // ✅ Ab restore complete — FixedUpdate save karna shuru kar sakta hai
            GetComponent<RCC_MirrorNetwork>().isRestoreComplete = true;
            Debug.Log("[PlayerSetup] ✅ isRestoreComplete = true — saving shuru hoga ab");

            return true;
        }
        //void SetUpUserLocalPlayerSettings()
        //{
        //    bool useDamage = System.Convert.ToBoolean(PlayerPrefs.GetInt(PPConst.Damage));
        //    gameManager = FindAnyObjectByType<MultiPlayerGameManager>();
        //    1.Show("khi khi khi");
        //    if (gameManager == null)
        //    {
        //        ("MultiPlayerGameManager not found in the scene.").Show("Multiplayer");
        //        return;
        //    }
        //    2.Show("khi khi khi");
        //    gameManager.SetUpUIForCar(useDamage);
        //    //Photon Removal   gameManager.repairButton.GetComponent<Button>().onClick.AddListener(() => view.RPC("RepairCarRPC", RpcTarget.AllBuffered));
        //    halfLapTrigger = gameManager.halfLapTrigger;
        //    finishLapTrigger = gameManager.finishLapTrigger;
        //    lapText = gameManager.lapText;
        //    lapText.text = "Laps " + (lapCount + 1) + "/" + totalLaps;
        //    StartCoroutine(ToggleLapTriggers(halfLap: true, finishLap: false));

        //    int colorNum = UnityEngine.Random.Range(0, 3);
        //    //view.RPC("ChangeColor", RpcTarget.AllBuffered, colorNum); // the function is in Car Customization script                                                     //Photon Removal
        //    //view.RPC("SyncUserSettings", RpcTarget.AllBuffered, useDamage);
        //    //view.RPC(nameof(SetupMiniMap), RpcTarget.AllBuffered);

        //    checkWrongDirection = gameObject.AddComponent<CheckWrongDirection_M>();
        //    checkWrongDirection.wrongDirectionCheckpoints = gameManager.waypoints;
        //    checkWrongDirection.wrongWayPopUp = gameManager.wrongWayPopUp;
        //}

        //Photon Removal [PunRPC]
        public void SyncUserSettings(bool damageVal)
        {
            GetComponent<RCC_CarControllerV3>().useDamage = damageVal;
        }

        //Photon Removal  [PunRPC]
        public void RepairCarRPC()
        {
            GetComponent<RCC_CarControllerV3>().Repair();
        }

        //Photon Removal  [PunRPC]
        public void SetupMiniMap()
        {
            positionIndicator = Instantiate(Resources.Load("MMAI", typeof(GameObject))) as GameObject;
            positionIndicator.transform.SetParent(transform, false);
            positionIndicator.transform.localPosition = new Vector3(0f, 10f, 0f);
            positionIndicator.transform.localScale = Vector3.one;
            positionIndicator.transform.localEulerAngles = new Vector3(-90, 0, 0);
        }
        public IEnumerator ToggleLapTriggers(bool halfLap, bool finishLap)
        {
            yield return new WaitForSeconds(2f);
            halfLapTrigger.isTrigger = halfLap;
            finishLapTrigger.isTrigger = finishLap;
        }

        private void OnTriggerExit(Collider collider)
        {
            if (NetworkServer.active && MultiPlayerGameManager.Instance != null)
            {
                if (collider.CompareTag("Middle"))
                    MultiPlayerGameManager.Instance.ServerRecordMiddleCheckpoint(netIdentity);
                else if (collider.CompareTag("Finish"))
                    MultiPlayerGameManager.Instance.ServerRecordFinishCheckpoint(netIdentity);
            }

            if (!identity.isOwned)
                return;

            if (collider.CompareTag("DirectionPoints"))
            {
                if (!collider.GetComponent<WayPoint>().IsCarPassed()) // if the check point was already passed, then do not increment the WayPoint Index
                {
                    Debug.Log($"✅ Restored — Lap: {lapCount}, " +
                                  $"MiddleCrossed: {hasCrossedMiddleCheckPoint}, " +
                                  $"Waypoint: {checkWrongDirection.currentCheckpointIndex}");
                    checkWrongDirection.OnCheckpointPassed();

                }
            }
            else if (collider.CompareTag("Middle"))
            {
                hasCrossedMiddleCheckPoint = true;
                CmdReportMiddleCheckpoint();
                StartCoroutine(ToggleLapTriggers(halfLap: false, finishLap: true));
            }
            else if (collider.CompareTag("Finish"))
            {
                if (hasCrossedMiddleCheckPoint)
                {
                    hasCrossedMiddleCheckPoint = false;
                    CmdReportFinishCheckpoint();
                    StartCoroutine(ToggleLapTriggers(halfLap: true, finishLap: false));
                    LapFinished();
                }
            }
        }

        [Command]
        private void CmdReportMiddleCheckpoint()
        {
            if (MultiPlayerGameManager.Instance != null)
                MultiPlayerGameManager.Instance.ServerRecordMiddleCheckpoint(netIdentity);
        }

        [Command]
        private void CmdReportFinishCheckpoint()
        {
            if (MultiPlayerGameManager.Instance != null)
                MultiPlayerGameManager.Instance.ServerRecordFinishCheckpoint(netIdentity);
        }

        void LapFinished()
        {
            EventManager.OnLapFinishEvent.Invoke();
            lapCount += 1;
            lapText.text = "" + (lapCount + 1) + "/" + totalLaps;
            if (lapCount == totalLaps)
            {
                SubmitResult();
                lapText.transform.parent.gameObject.SetActive(false);
            }

        }
        public void Update()
        {
            if (Input.GetKeyUp(KeyCode.Space))
            {
                SubmitResult();
            }
        }
        /// <summary>
        /// Player Score / Position System
        /// </summary>

        private void OnEnable()
        {
            EventManager.OnMultiplayergameStarted += GameStarted;
        }

        private void OnDisable()
        {
            EventManager.OnMultiplayergameStarted -= GameStarted;
        }
        private void GameStarted()
        {
            if (!identity.isOwned)
                return;
            matchStartTime = Time.time;
            positionIndicator.GetComponentInChildren<Image>().color = Color.green;
            // SpawnMiniMap();


        }
        void SpawnMiniMap()
        {
            GameObject miniMapObject = Instantiate(Resources.Load("MiniMap", typeof(GameObject))) as GameObject;
            Transform miniMapCam = miniMapObject.transform.GetChild(0).transform;
            miniMapCam.transform.SetParent(this.transform, false);
            miniMapCam.transform.localPosition = new Vector3(0f, 40f, 0f);
            miniMapCam.transform.localScale = Vector3.one;
            miniMapCam.transform.localEulerAngles = new Vector3(90, 0, 0);

            positionIndicator.GetComponentInChildren<Image>().color = Color.green;
        }
        void SubmitResult()
        {
            if (isGameFinished)
                return;
            isGameFinished = true;
            matchEndTime = Time.time;
            float totalTime = matchEndTime - matchStartTime;
            DoFinishRaceStuff(totalTime);
            //Photon Removal   string finishPropKey = PhotonNetwork.CurrentRoom.Name + "Finish";
            //Photon Removal       PhotonNetwork.SetPlayerCustomProperties(new ExitGames.Client.Photon.Hashtable() { { finishPropKey, totalTime } });

        }
        void DoFinishRaceStuff(float totalTime)
        {
            //   GetComponent<RCC_PhotonNetwork>().Stop();
            gameManager.finishPopTime.text = System.Math.Round(totalTime, 3).ToString();
            gameManager.ShowFinishCinematics();
        }
    }

}

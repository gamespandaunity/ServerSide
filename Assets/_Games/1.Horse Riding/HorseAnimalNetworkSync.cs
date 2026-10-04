using MalbersAnimations;
using Mirror;
using Shapes2D;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

public class HorseAnimationSync : NetworkBehaviour
{
    public static HorseAnimationSync ins;
    private Animal horse;
    private PlayerPowerController powerController;
    private HorseMobileButton mobileControls;
    private Rigidbody rigid; // Add this
    private PlayerPositionController Playerpositioncontroller;

    [Header("Synced Inputs")]
    [SyncVar] public float accelerationInput;
    [SyncVar] public float brakeForceInput;
    [SyncVar] public float steeringLeftInput;
    [SyncVar] public float steeringRightInput;
    [SyncVar] public float steeringInput;
    [SyncVar] public float gyroscopeInput;
    [SyncVar] public float joystickDirectionInput;
    [SyncVar(hook = nameof(OnPlayerNameChanged))] public string PlayerName = "";
    [SyncVar] public string playerCountry = "";
    // public ResultManagerForHorse ResultManagerHorse;
    public Text nameText;
    [SyncVar] public string playerID = "";
    [SyncVar] public Vector3 netPosition;
    [SyncVar] public Quaternion netRotation;
    public GameObject FlagObject;
    public GameObject NameObject;
    // public static bool ControlsEnabled = false;

    private void Awake()
    {
        ins = this;
        horse = GetComponent<Animal>();
        powerController = GetComponent<PlayerPowerController>();
        rigid = GetComponent<Rigidbody>(); // Get Rigidbody
                                           // ResultManagerHorse = FindAnyObjectByType<ResultManagerForHorse>(FindObjectsInactive.Include);
        Playerpositioncontroller = GetComponent<PlayerPositionController>();
    }

    private void Start()
    {
        if (isOwned)
        {
            FlagObject.SetActive(false);
            NameObject.SetActive(false);
            PlayerName = staticVariables.UserProfiledata.user.first_name + " " +
                         staticVariables.UserProfiledata.user.last_name;
            playerCountry = staticVariables.UserProfiledata.user.country;
            mobileControls = FindObjectOfType<HorseMobileButton>();

            if (mobileControls != null)
            {
                mobileControls.horseController = horse;

                if (powerController != null)
                {
                    mobileControls.powerBoostController = powerController;
                    powerController.OnBoostChanged += mobileControls.IncreaseNitroCharge;
                }

            }
            //  ResultManagerForHorse.isGameFinished = false;

            StartCoroutine(SavePositionPeriodically());


        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isOwned)
        {
            playerID = MirrorPlayerPrefab.Instance.playerData.playerId;

            // Restore position if reconnecting
            if (HorseMirrorGameManager.PlayersHorseData.ContainsKey(playerID))
            {
                HorseMirrorGameManager.HorseRecord horseData =
                    HorseMirrorGameManager.PlayersHorseData[playerID];

                transform.position = horseData.Position;
                transform.rotation = horseData.Rotation;

                if (Playerpositioncontroller != null)
                {
                    Playerpositioncontroller.currentWaypoint = horseData.CurrentWaypoint;
                    Playerpositioncontroller.lap = horseData.Lap;
                    Playerpositioncontroller.GrandTalWaypointPassed = horseData.GrandTotalWaypointPassed;

                    Playerpositioncontroller.ResetWaypointTracking();

                }
                // if (rigid != null)
                // {
                //     rigid.linearVelocity = horseData.Velocity;
                //     rigid.angularVelocity = horseData.AngularVelocity;
                // }

                Debug.Log($"✅ Restored position for player {playerID}: {horseData.Position}");
                StartCoroutine(SyncRestoredPositionToServer(horseData));
            }
        }
    }
    private IEnumerator SyncRestoredPositionToServer(HorseMirrorGameManager.HorseRecord horseData)
    {
        yield return new WaitForEndOfFrame();
        CmdRestorePosition(horseData.Position, horseData.Rotation,
                           horseData.Velocity, horseData.AngularVelocity);
    }

    [Command]
    private void CmdRestorePosition(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
    {
        // Update server's transform
        transform.position = pos;
        transform.rotation = rot;

        if (rigid != null)
        {
            rigid.linearVelocity = vel;
            rigid.angularVelocity = angVel;
        }

        Debug.Log($"🔄 Server updated position for reconnected player: {pos}");

        // Force sync to all clients
        RpcForcePositionSync(pos, rot, vel, angVel);
    }

    [ClientRpc]
    private void RpcForcePositionSync(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel)
    {
        if (!isOwned) // Only update other clients, not the owner
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

    // Save position every 0.5 seconds
    private IEnumerator SavePositionPeriodically()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            SaveCurrentPosition();
        }
    }

    // Save current horse position to dictionary
    private void SaveCurrentPosition()
    {
        if (string.IsNullOrEmpty(playerID)) return;

        HorseMirrorGameManager.HorseRecord record = new HorseMirrorGameManager.HorseRecord
        {
            Position = transform.position,
            Rotation = transform.rotation,
            Velocity = rigid != null ? rigid.linearVelocity : Vector3.zero,
            AngularVelocity = rigid != null ? rigid.angularVelocity : Vector3.zero,
            CurrentWaypoint = Playerpositioncontroller.currentWaypoint,
            Lap = Playerpositioncontroller.lap,
            GrandTotalWaypointPassed = Playerpositioncontroller.GrandTalWaypointPassed
        };

        HorseMirrorGameManager.PlayersHorseData[playerID] = record;
    }
    public void SetMobileButtonsVisible(bool visible)
    {
        if (mobileControls != null)
            mobileControls.gameObject.SetActive(false);
        Debug.Log($"Setting mobile controls visibility to {visible}");
    }
    private void Update()
    {
        if (!horse) return;
        //if (!ControlsEnabled) return;
        if (isOwned)
        {
            if (mobileControls != null)
            {
                float acc = mobileControls.GetAccelerationInput();
                float brake = mobileControls.GetBrakeInput();
                float steerL = mobileControls.GetSteerLeftInput();
                float steerR = mobileControls.GetSteerRightInput();
                float steer = mobileControls.GetSteeringInput();
                float gyro = mobileControls.GetGyroscopeInput();
                float joyDir = mobileControls.GetJoystickDirectionInput();

                CmdSyncHorseInputs(acc, brake, steerL, steerR, steer, gyro, joyDir);
                ApplyInputs(acc, brake, steerL, steerR, steer, gyro, joyDir);

                PlayerName = staticVariables.UserProfiledata.user.first_name + " " +
                         staticVariables.UserProfiledata.user.last_name;
                playerCountry = "IN";
                CmdSyncTransform(transform.position, transform.rotation);
                CmdSetPlayerName(PlayerName, playerCountry);
            }
        }
        else
        {
            ApplyInputs(accelerationInput, brakeForceInput, steeringLeftInput,
                       steeringRightInput, steeringInput, gyroscopeInput, joystickDirectionInput);
        }
    }

    public void RequestJump()
    {
        if (!isOwned || horse == null) return;

        // Play immediately for the owner, then reliably notify every observer.
        horse.SetJump();
        CmdTriggerJump();
    }

    [Command]
    private void CmdTriggerJump()
    {
        RpcTriggerJump();
    }

    [ClientRpc]
    private void RpcTriggerJump()
    {
        // The owner already played the jump locally for responsive controls.
        if (isOwned || horse == null) return;

        horse.SetJump();
    }

    [Command]
    void CmdSyncTransform(Vector3 pos, Quaternion rot)
    {
        netPosition = pos;
        netRotation = rot;
    }
    [Command]
    private void CmdSyncHorseInputs(float acc, float brake, float steerL,
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
        if (!horse) return;

        Vector3 moveVector = new Vector3(-steerL + steerR + steer + gyro + joyDir, 0, acc - brake);
        horse.Move(moveVector, false);
    }

    [Command]
    private void CmdSetPlayerName(string name, string country)
    {
        PlayerName = name;
        playerCountry = country;
    }

    private void OnPlayerNameChanged(string oldName, string newName)
    {
        if (nameText != null)
            nameText.text = newName;
    }

    private void OnDestroy()
    {
        // Save position one last time before destruction
        if (isOwned)
        {
            SaveCurrentPosition();
            Debug.Log($"💾 Saved position on disconnect for player {playerID}");
        }

        if (powerController != null && mobileControls != null)
        {
            powerController.OnBoostChanged -= mobileControls.IncreaseNitroCharge;
        }
    }

    private void OnDisable()
    {
        // Save position when disabled
        if (isOwned && !string.IsNullOrEmpty(playerID))
        {
            SaveCurrentPosition();
        }
    }

    [Command]
    public void CmdPlayerFinished()
    {
        if (HorseMirrorGameManager.Instance == null)
        {
            Debug.LogWarning("[HorseServerResult] Finish rejected: Horse manager is missing on the server.");
            return;
        }

        HorseMirrorGameManager.Instance.ServerRecordFinish(netIdentity);
    }

   
}

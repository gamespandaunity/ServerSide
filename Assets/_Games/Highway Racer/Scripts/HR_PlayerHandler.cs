//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------
using CarRace;

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
// #if PHOTON_UNITY_NETWORKING
// //using Photon;

// #endif

/// <summary>
/// Player manager that containts current score, near misses.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
//[RequireComponent(typeof(RCC_CarControllerV3))]
[RequireComponent(typeof(HR_ModApplier))]
[AddComponentMenu("BoneCracker Games/Highway Racer/Player/HR Player Handler")]
public class HR_PlayerHandler : MonoBehaviour
{

    private RCC_CarControllerV3 carController;      //	Car controller.
    private Rigidbody rigid;        //	Rigidbody.
    private HighwayCarNetwork carNetwork;       //	Networked car (multiplayer me hi hota hai).

    public bool canCrash = true;

    [Header("Respawn")]
    [Tooltip("Respawn karte waqt do cars ke darmiyan kam se kam X (lane) distance.")]
    public float minimumLaneSeparation = 3f;
    [Tooltip("Respawn karte waqt do cars ke darmiyan kam se kam Z distance.")]
    public float minimumRespawnGap = 15f;

    [Range(250f, 1000f)] public float maxEngineTorque = 300f;        //	Maximum upgradable engine torque.
    [Range(2000f, 6000f)] public float maxBrakeTorque = 2000f;        //	Maximum upgradable brake torque.
    [Range(.1f, .5f)] public float maxHandlingStrength = .1f;     //	Maximum upgradable handling strength.
    [Range(200f, 400f)] public float maxSpeed = 360f;     //	Maximum upgradable speed.

    private bool crashed = false;      //	Game is over now?

    internal float score;       //	Current score
    internal float timeLeft = 100f;     //	Time left.
    internal int combo;     //	Current near miss combo.
    internal int maxCombo;      //	Highest combo count.
    internal float distanceToNextPlayer = -9999f;     //	Time left.

    internal float speed = 0f;      //  Current speed.
    internal float distance = 0f;       //  Total distance traveled.
    internal float highSpeedCurrent = 0f;       //  Current high speed time.
    internal float highSpeedTotal = 0f;     //  Total high speed time.
    internal float opposideDirectionCurrent = 0f;       //  Current opposite direction time.
    internal float opposideDirectionTotal = 0f;     //  Total opposite direction time.
    internal int nearMisses;        //  Total near misses.
    private float comboTime;        //  Combo time for near misses.
    private Vector3 previousPosition;

    private int minimumSpeedForGainScore
    {
        get
        {
            return HR_HighwayRacerProperties.Instance._minimumSpeedForGainScore;
        }
    }
    private int minimumSpeedForHighSpeed
    {
        get
        {
            return HR_HighwayRacerProperties.Instance._minimumSpeedForHighSpeed;
        }
    }

    public int totalDistanceMoneyMP
    {
        get
        {
            return HR_HighwayRacerProperties.Instance._totalDistanceMoneyMP;
        }
    }
    public int totalNearMissMoneyMP
    {
        get
        {
            return HR_HighwayRacerProperties.Instance._totalNearMissMoneyMP;
        }
    }
    public int totalOverspeedMoneyMP
    {
        get
        {
            return HR_HighwayRacerProperties.Instance._totalOverspeedMoneyMP;
        }
    }
    public int totalOppositeDirectionMP
    {
        get
        {
            return HR_HighwayRacerProperties.Instance._totalOppositeDirectionMP;
        }
    }

    private string currentTrafficCarNameLeft;
    private string currentTrafficCarNameRight;

    internal bool bombTriggered = false;
    internal float bombHealth = 100f;

    [Header("Wrong Way")]
    [Tooltip("Sadak hamesha +Z par jati hai. Car ki heading aur road direction ka dot product is se neeche jaye to warning aati hai. .35 ka matlab road se ~70 degree se zyada mura hua.")]
    [Range(-1f, 1f)] public float wrongWayHeadingThreshold = .35f;
    [Tooltip("Is se tez (m/s) peeche ki taraf move karna wrong way hai, chahe car ka mun abhi bhi aage ho (reverse gear).")]
    public float wrongWayReverseSpeed = 1f;
    [Tooltip("TwoWay mode me x <= 0 wali side par traffic ulti aati hai, to us side par jana bhi wrong way count hota hai.")]
    public bool warnOnOncomingLane = true;
    [Tooltip("Warning dikhane se pehle itni der wrong way par hona zaroori hai. Spin ya choti si correction par popup blink na kare.")]
    public float wrongWayConfirmTime = .25f;

    [Tooltip("Sirf dekhne ke liye. Play mode me yahan se confirm kar sakte hain ke detection chal rahi hai ya nahi.")]
    public bool wrongWay = false;       //  Player is going against the flow of the highway?
    private float wrongWayTimer = 0f;       //  How long the player has been going the wrong way.

    private AudioSource hornSource;

    public delegate void onNearMiss(HR_PlayerHandler player, int score, HR_UIDynamicScoreDisplayer.Side side);
    public static event onNearMiss OnNearMiss;

    // #if PHOTON_UNITY_NETWORKING
    //     private PhotonView photonView;
    // #endif

    private void Awake()
    {
           Debug.Log("HR_PlayerHandlerAwake");
        //	Getting components and setting rigid settings.
        carController = GetComponent<RCC_CarControllerV3>();
        rigid = GetComponent<Rigidbody>();
        carNetwork = GetComponent<HighwayCarNetwork>();

        HR_VehicleUpgrade_Engine upgradeEngine = GetComponentInChildren<HR_VehicleUpgrade_Engine>();
        HR_VehicleUpgrade_Brake upgradeBrake = GetComponentInChildren<HR_VehicleUpgrade_Brake>();
        HR_VehicleUpgrade_Handling upgradeHandling = GetComponentInChildren<HR_VehicleUpgrade_Handling>();
        HR_VehicleUpgrade_Speed upgradeSpeed = GetComponentInChildren<HR_VehicleUpgrade_Speed>();

        //	Setting maximum upgradable values to the correspondind component.
        if (upgradeEngine)
            upgradeEngine.maxEngine = maxEngineTorque;

        if (upgradeBrake)
            upgradeBrake.maxBrake = maxBrakeTorque;

        if (upgradeHandling)
            upgradeHandling.maxHandling = maxHandlingStrength;

        if (upgradeSpeed)
            upgradeSpeed.maxSpeed = maxSpeed;

    }

    private void OnEnable()
    {

        Debug.Log("HR_PlayerHandlerOnEnableOnEnable");
        //	If engine is not running, start the engine.
        if (carController && !carController.engineRunning)
            carController.StartEngine();

        //if (HR_NetworkManager.Instance)
        //{
        //    photonView = GetComponent<PhotonView>();                                                                                  //Photon Removal

        //    if (photonView && !photonView.IsMine)
        //        return;
        //    if (!photonView.IsMine)
        //        HR_NetworkManager.Instance.hR_GamePlayHandler.player2 = this;
        //}
        //	Creating horn audio source.
        hornSource = HR_CreateAudioSource.NewAudioSource(gameObject, "Horn", 10f, 100f, 1f, HR_HighwayRacerProperties.Instance.hornClip, true, false, false);

        CheckGroundGap();

    }

    private void Update()
    {

        // #if PHOTON_UNITY_NETWORKING && BCG_HR_PHOTON

        //         if (photonView && !photonView.IsMine)
        //             return;

        // #endif

        //	If scene doesn't include gameplay manager, return.
       // if (!HR_GamePlayHandler.Instance)
       //     return;

        //	If game is not started yet, return.
       // if (crashed || !HR_GamePlayHandler.Instance.gameStarted)
      //      return;

        //	Speed of the car.
        if (carController)
            speed = carController.speed;

        // Total distance traveled.
        distance += Vector3.Distance(previousPosition, transform.position) / 1000f;
        previousPosition = transform.position;

        //	Is speed is high enough, gain score.
        if (speed >= minimumSpeedForGainScore)
            score += carController.speed * (Time.deltaTime * .05f);

        //	If speed is higher than high speed, gain score.
        if (speed >= minimumSpeedForHighSpeed)
        {

            highSpeedCurrent += Time.deltaTime;
            highSpeedTotal += Time.deltaTime;

        }
        else
        {

            highSpeedCurrent = 0f;

        }

        // If car is at opposite direction, gain score.
        if (speed >= (minimumSpeedForHighSpeed / 2f) && transform.position.x <= 0f && HR_GamePlayHandler.Instance.mode == HR_GamePlayHandler.Mode.TwoWay)
        {

            opposideDirectionCurrent += Time.deltaTime;
            opposideDirectionTotal += Time.deltaTime;

        }
        else
        {

            opposideDirectionCurrent = 0f;

        }

        //	If mode is time attack, reduce the timer.
        if (HR_GamePlayHandler.Instance.mode == HR_GamePlayHandler.Mode.TimeAttack)
        {

            timeLeft -= Time.deltaTime;

            // If timer hits 0, game over.
            if (timeLeft < 0)
            {

                timeLeft = 0;
              //  GameOver();

            }

        }

        comboTime += Time.deltaTime;

        //	If game mode is bomb...
        if (HR_GamePlayHandler.Instance.mode == HR_GamePlayHandler.Mode.Bomb)
        {

            //	Bomb will be triggered below 80 km/h.
            if (speed > 80f)
            {

                if (!bombTriggered)
                    bombTriggered = true;

                else
                    bombHealth += Time.deltaTime * 5f;

            }
            else if (bombTriggered)
            {

                bombHealth -= Time.deltaTime * 10f;

            }

            bombHealth = Mathf.Clamp(bombHealth, 0f, 100f);

            //	If bomb health hits 0, blow and game over.
            if (bombHealth <= 0f)
            {

                GameObject explosion = Instantiate(HR_HighwayRacerProperties.Instance.explosionEffect, transform.position, transform.rotation);
                explosion.transform.SetParent(null);
                rigid.isKinematic = true;
               // GameOver();

            }

        }

        if (comboTime >= 2)
            combo = 0;

        CheckWrongWay();
        CheckStatus();

    }

    /// <summary>
    /// Checks whether the player is going against the flow of the highway. Traffic always heads
    /// towards +Z, so heading and travel are both measured against that. Covers every way of
    /// going back: turning the car around, reversing, and - in two way mode - crossing over to
    /// the side the oncoming traffic uses.
    /// </summary>
    private void CheckWrongWay()
    {

        //	No warning after the crash or while the car is frozen. Deliberately not gated on
        //	gameStarted: the flag stays false in some of the networked start flows while the
        //	player can already drive.
        if (crashed || rigid.isKinematic || !HR_GamePlayHandler.Instance)
        {

            wrongWayTimer = 0f;
            wrongWay = false;
            return;

        }

        //	Car ka mun sadak se kaafi hut chuka hai (U turn ki koshish).
        bool facingBackwards = Vector3.Dot(transform.forward, Vector3.forward) < wrongWayHeadingThreshold;

        //	Reverse gear - mun aage hai lekin car peeche ja rahi hai.
        bool reversingBackwards = rigid.linearVelocity.z < -wrongWayReverseSpeed;

        //	TwoWay me x <= 0 wali side ki traffic 180 degree ghuma kar spawn hoti hai
        //	(HR_TrafficPooling), yani wahan jana oncoming traffic me ghusna hai. Baaki modes me
        //	poori sadak ek hi taraf chalti hai, is liye wahan ye clause lagu nahi hota.
        bool onOncomingSide = warnOnOncomingLane
            && HR_GamePlayHandler.Instance.mode == HR_GamePlayHandler.Mode.TwoWay
            && transform.position.x <= 0f;

        if (facingBackwards || reversingBackwards || onOncomingSide)
            wrongWayTimer += Time.deltaTime;
        else
            wrongWayTimer = 0f;

        wrongWay = wrongWayTimer >= wrongWayConfirmTime;

    }

    private void FixedUpdate()
    {
            // Debug.Log("HR_PlayerHandlerFixedUpdateFixedUpdate");
        // #if PHOTON_UNITY_NETWORKING && BCG_HR_PHOTON

        //         if (photonView && !photonView.IsMine)
        //             return;

        // #endif

        //	If scene doesn't include gameplay manager, return.
        if (!HR_GamePlayHandler.Instance)
            return;

        //	If game is started, check near misses with raycasts.
        if (!crashed && HR_GamePlayHandler.Instance.gameStarted)
            CheckNearMiss();

    }

    /// <summary>
    /// Checks near vehicles by drawing raycasts to the left and right sides.
    /// </summary>
    private void CheckNearMiss()
    {

        RaycastHit hit;

        Debug.DrawRay(carController.COM.position, (-transform.right * 2f), Color.white);
        Debug.DrawRay(carController.COM.position, (transform.right * 2f), Color.white);

        // Raycasting to the left side.
        if (Physics.Raycast(carController.COM.position, (-transform.right), out hit, 2f, HR_HighwayRacerProperties.Instance.trafficCarsLayer) && !hit.collider.isTrigger)
        {

            //	If hits, get it's name.
            currentTrafficCarNameLeft = hit.transform.name;

        }
        else
        {

            if (currentTrafficCarNameLeft != null && speed > HR_HighwayRacerProperties.Instance._minimumSpeedForGainScore)
            {

                nearMisses++;
                combo++;
                comboTime = 0;

                if (maxCombo <= combo)
                    maxCombo = combo;

                score += 100f * Mathf.Clamp(combo / 1.5f, 1f, 20f);
                OnNearMiss(this, (int)(100f * Mathf.Clamp(combo / 1.5f, 1f, 20f)), HR_UIDynamicScoreDisplayer.Side.Left);

                currentTrafficCarNameLeft = null;

            }
            else
            {

                currentTrafficCarNameLeft = null;

            }

        }

        // Raycasting to the right side.
        if (Physics.Raycast(carController.COM.position, (transform.right), out hit, 2f, HR_HighwayRacerProperties.Instance.trafficCarsLayer) && !hit.collider.isTrigger)
        {

            //	If hits, get it's name.
            currentTrafficCarNameRight = hit.transform.name;

        }
        else
        {

            if (currentTrafficCarNameRight != null && speed > HR_HighwayRacerProperties.Instance._minimumSpeedForGainScore)
            {

                nearMisses++;
                combo++;
                comboTime = 0;

                if (maxCombo <= combo)
                    maxCombo = combo;

                score += 100f * Mathf.Clamp(combo / 1.5f, 1f, 20f);
                OnNearMiss(this, (int)(100f * Mathf.Clamp(combo / 1.5f, 1f, 20f)), HR_UIDynamicScoreDisplayer.Side.Right);

                currentTrafficCarNameRight = null;

            }
            else
            {

                currentTrafficCarNameRight = null;

            }

        }

        // Raycasting to the front side. Used for taking down the lane.
        if (Physics.Raycast(carController.COM.position, (transform.forward), out hit, 40f, HR_HighwayRacerProperties.Instance.trafficCarsLayer) && !hit.collider.isTrigger)
        {

            Debug.DrawRay(carController.COM.position, (transform.forward * 20f), Color.red);

            if (carController.highBeamHeadLightsOn)
                hit.transform.SendMessage("ChangeLines");

        }

        // Horn and siren.
        if (hornSource)
        {

            hornSource.volume = Mathf.Lerp(hornSource.volume, carController.highBeamHeadLightsOn ? 1f : 0f, Time.deltaTime * 25f);

            if (carController.highBeamHeadLightsOn)
            {

                HR_VehicleUpgrade_Siren upgradeSiren = GetComponentInChildren<HR_VehicleUpgrade_Siren>();

                if (upgradeSiren && upgradeSiren.isActiveAndEnabled)
                    hornSource.clip = HR_HighwayRacerProperties.Instance.sirenAudioClip;

                if (!hornSource.isPlaying)
                    hornSource.Play();

            }
            else
            {

                hornSource.Stop();

            }

        }

    }

    private void OnCollisionEnter(Collision col)
    {


        //Photon Removal if (photonView && !photonView.IsMine)
        // return;


        if (!HR_GamePlayHandler.Instance)
            return;

        if (!canCrash)
            return;

        if (crashed)
            return;

        //	If scene doesn't include gameplay manager, return.
        if (!HR_GamePlayHandler.Instance)
            return;

        //	Calculating collision impulse.
        float impulse = col.impulse.magnitude / 1000f;

        //	If impulse is below the limit, return.
        if (impulse < HR_HighwayRacerProperties.Instance._minimumCollisionForGameOver)
            return;

        // Resetting combo to 0.
        combo = 0;

        // If hit is not a traffic car, return.
        if ((1 << col.gameObject.layer) != HR_HighwayRacerProperties.Instance.trafficCarsLayer.value)
            return;

        // If mode is bomb mode, reduce the bomb health.
        if (HR_GamePlayHandler.Instance.mode == HR_GamePlayHandler.Mode.Bomb)
        {

            bombHealth -= impulse * 3f;
            return;

        }

        //	Freezing the car and game over.
       // rigid.isKinematic = true;
     //   GameOver();

    }

    /// <summary>
    /// Checks position of the car. If exceeds limits, respawns it.
    /// </summary>
    private void CheckStatus()
    {

        if (rigid.isKinematic)
            return;

        if (!HR_GamePlayHandler.Instance.gameStarted)
            return;

        //	Multiplayer me sirf apni car reset hoti hai. Opponent ki car ki position
        //	network se aati hai (NetworkTransform client authority), isliye usay locally reset na karo.
        if (IsMultiplayer && !carNetwork.isOwned)
            return;
        // Debug.Log("Speed" + speed);
        // Debug.Log("Mathf.X" + Mathf.Abs(transform.position.x));
        // Debug.Log("Mathf.Y" + Mathf.Abs(transform.position.y));
        //	If speed is below 5, or X position of the car exceeds limits, respawn it.
        //
        // if (speed <= 15f || Mathf.Abs(transform.position.x) > 10f || Mathf.Abs(transform.position.y) > 10f)
        //    transform.position = new Vector3(0f, 2.5f, transform.position.z + 15f);
        //    transform.rotation = Quaternion.identity;
        //    rigid.angularVelocity = Vector3.zero;
        //    rigid.linearVelocity = new Vector3(0f, 0f, 12f);
        //}
        if (speed <= 5f)
            lowSpeedTimer += Time.deltaTime;
        else
            lowSpeedTimer = 0f;

        // ✅ Reset after 5 seconds OR position limits
        if (lowSpeedTimer >= 5f || Mathf.Abs(transform.position.x) > 10f || Mathf.Abs(transform.position.y) > 10f)
        {
            transform.position = GetRespawnPosition();
            transform.rotation = Quaternion.identity;
            rigid.angularVelocity = Vector3.zero;
            rigid.linearVelocity = new Vector3(0f, 0f, 12f);
            lowSpeedTimer = 0f;
        }

    }

    /// <summary>
    /// Multiplayer chal raha hai ya nahi. netId sirf tab set hoti hai jab car network par spawn hui ho,
    /// offline instantiate ki hui car ke liye ye 0 rehti hai.
    /// </summary>
    private bool IsMultiplayer
    {
        get { return carNetwork != null && carNetwork.netId != 0; }
    }

    /// <summary>
    /// Respawn position. Multiplayer me har car apni assign ki hui lane me wapas aati hai
    /// (side by side), aur agar wahan opponent maujood ho to aage shift kar di jati hai
    /// taake dono cars ek dusre ke upar spawn na hon.
    /// </summary>
    private Vector3 GetRespawnPosition()
    {

        //	Single player me purana behaviour - road ke beech me.
        if (!IsMultiplayer)
            return new Vector3(0f, 2.5f, transform.position.z + 15f);

        float laneX = carNetwork.resetLaneX;
        float laneZ = transform.position.z + 15f;

        //	Agar opponent isi lane me aur isi jagah ke aas paas hai, to us se aage respawn karo.
        for (int i = 0; i < HighwayCarNetwork.Cars.Count; i++)
        {

            HighwayCarNetwork other = HighwayCarNetwork.Cars[i];

            if (other == null || other == carNetwork)
                continue;

            Vector3 otherPosition = other.transform.position;

            if (Mathf.Abs(otherPosition.x - laneX) < minimumLaneSeparation && Mathf.Abs(otherPosition.z - laneZ) < minimumRespawnGap)
                laneZ = otherPosition.z + minimumRespawnGap;

        }

        return new Vector3(laneX, 2.5f, laneZ);

    }

    public float lowSpeedResetTime = 3f;
    private float lowSpeedTimer = 0f;

    /// <summary>
    /// Game Over.
    /// </summary>
    public void GameOver()
    {

        crashed = true;
        carController.canControl = false;
        carController.engineRunning = false;

        int[] scores = new int[4];
        scores[0] = Mathf.FloorToInt(distance * totalDistanceMoneyMP);
        scores[1] = Mathf.FloorToInt(nearMisses * totalNearMissMoneyMP);
        scores[2] = Mathf.FloorToInt(highSpeedTotal * totalOverspeedMoneyMP);
        scores[3] = Mathf.FloorToInt(opposideDirectionTotal * totalOppositeDirectionMP);

        for (int i = 0; i < scores.Length; i++)
            HR_API.AddCurrency(scores[i]);

        HR_GamePlayHandler.Instance.CrashedPlayer(this, scores);

    }

    /// <summary>
    /// Eliminates ground gap distance on when spawned.
    /// </summary>
    private void CheckGroundGap()
    {

        WheelCollider wheel = GetComponentInChildren<WheelCollider>();
        float distancePivotBetweenWheel = Vector3.Distance(new Vector3(0f, transform.position.y, 0f), new Vector3(0f, wheel.transform.position.y, 0f));

        RaycastHit hit;

        if (Physics.Raycast(wheel.transform.position, -Vector3.up, out hit, 10f))
            transform.position = new Vector3(transform.position.x, hit.point.y + distancePivotBetweenWheel + (wheel.radius / 1f) + (wheel.suspensionDistance / 2f), transform.position.z);

    }

    private void Reset()
    {

        carController = GetComponent<RCC_CarControllerV3>();
        rigid = GetComponent<Rigidbody>();

        maxEngineTorque = carController.maxEngineTorque + 50;
        maxBrakeTorque = carController.brakeTorque + 500;
        maxHandlingStrength = carController.steerHelperAngularVelStrength + .2f;
        maxSpeed = carController.maxspeed + 40f;

    }

}

//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using Mirror;

/// <summary>
/// Traffic car controller.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[AddComponentMenu("BoneCracker Games/Highway Racer/Traffic/HR Traffic Car")]
public class HR_TrafficCar : NetworkBehaviour
{

    // Getting an Instance of HR_GamePlayHandler.
    #region HR_GamePlayHandler Instance

    private HR_GamePlayHandler HR_GamePlayHandlerInstance;
    private HR_GamePlayHandler HR_GamePlayHandler
    {
        get
        {
            if (HR_GamePlayHandlerInstance == null)
            {
                HR_GamePlayHandlerInstance = HR_GamePlayHandler.Instance;
            }
            return HR_GamePlayHandlerInstance;
        }
    }

    #endregion

    // Getting an Instance of HR_TrafficPooling.
    #region HR_TrafficPooling Instance

    private HR_TrafficPooling HR_TrafficPoolingInstance;
    private HR_TrafficPooling HR_TrafficPooling
    {
        get
        {
            if (HR_TrafficPoolingInstance == null)
            {
                HR_TrafficPoolingInstance = HR_TrafficPooling.Instance;
            }
            return HR_TrafficPoolingInstance;
        }
    }

    #endregion

    private Rigidbody rigid;        //  Rigidbody.

    private bool crashed = false;       //  Crashed?
    public BoxCollider bodyCollider;        //  Collider.
    internal BoxCollider triggerCollider;       //  Trigger.

    public ChangingLines changingLines = ChangingLines.Straight;
    public enum ChangingLines { Straight, Right, Left }
    internal int currentLine = 0;       //  Current line.
    internal int trafficIndex = -1;     // ✅ Traffic car ka index (for identification)

    public float maximumSpeed = 10f;        //  Maximum speed of the car.
    private float _maximumSpeed = 10f;
    private float desiredSpeed;     //  Desired speed (adapted) of the car.
    private float distance = 0f;        //  Distance to next car.
    private Quaternion steeringAngle = Quaternion.identity;     //  Steering angle.

    public Transform[] wheelModels;     //  Wheel models.
    private float wheelRotation = 0f;       //  Wheel rotation.

    [Header("Just Lights. All of them will work on ''NOT Important'' Render Mode.")]
    public Light[] headLights;
    public Light[] brakeLights;
    public Light[] signalLights;

    private bool headlightsOn = false;
    private bool brakingOn = false;

    private SignalsOn signalsOn = SignalsOn.Off;
    private enum SignalsOn { Off, Right, Left, All }
    private float signalTimer = 0f;
    private float spawnProtection = 0f;

    [Space(10)]

    public AudioClip engineSound;
    private AudioSource engineSoundSource;

    [Header("Realism / Strength (GamesPanda)")]
    [Tooltip("Mass is multiplied by this so traffic is not pushed around like paper.")]
    public float massMultiplier = 1.5f;
    [Tooltip("No traffic car is lighter than this (kg).")]
    public float minimumMass = 1900f;
    [Tooltip("How fast the car speeds up (m/s per second).")]
    public float acceleration = 3.5f;
    [Tooltip("How hard the car brakes for traffic ahead (m/s per second).")]
    public float brakeDeceleration = 7f;
    [Tooltip("Bumps softer than this relative speed (m/s) do not count as a crash - the car keeps driving.")]
    public float crashSpeedThreshold = 3f;
    [Tooltip("Indicator blinks this long before the car starts changing lane (s).")]
    public float signalTimeBeforeLaneChange = 0.8f;
    [Tooltip("Steering angle used while changing lane (deg).")]
    public float laneChangeAngle = 4f;
    private float laneChangeTimer = 0f;

    private bool isMultiplayer = false;

    // ✅ Network synced variables - Server sets, clients display
    //   [SyncVar] public Vector3 syncedPos;
    //   [SyncVar] public Quaternion syncedRot;
    //   [SyncVar] public float syncedSpeed;
    //public override void OnStartClient()
    //{
    //    base.OnStartClient();

    //    // Make sure pooling instance exists
    //    if (HR_TrafficPooling.instance != null)
    //    {
    //        var container = HR_TrafficPooling.instance.container;

    //        if (container != null)
    //            transform.SetParent(container.transform, true);
    //    }
    //}
    private void Awake()
    {
        // Rigidbody.
        rigid = GetComponent<Rigidbody>();
        rigid.linearDamping = 1f;
        rigid.angularDamping = 4f;
        rigid.maxAngularVelocity = 2.5f;
        rigid.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
        rigid.mass = Mathf.Max(minimumMass, rigid.mass * massMultiplier);       // stronger, harder to push
        rigid.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // no clipping at high speed
        rigid.interpolation = RigidbodyInterpolation.Interpolate;

        //  Getting all lights.
        Light[] allLights = GetComponentsInChildren<Light>();

        //  Setting lights as vertex lights and make sure they are not affecting to any surface.
        foreach (Light l in allLights)
        {
            l.renderMode = LightRenderMode.ForceVertex;
            l.cullingMask = 0;
        }

        //  Forward distance.
        distance = 50f;

        //  Collider.
        if (!bodyCollider)
        {
            bodyCollider = GetComponentInChildren<BoxCollider>();
        }

        //  Creating trigger for detecting front vehicles.
        GameObject triggerColliderGO = new GameObject("TriggerVolume");
        triggerColliderGO.transform.position = bodyCollider.bounds.center;
        triggerColliderGO.transform.rotation = bodyCollider.transform.rotation;
        triggerColliderGO.transform.SetParent(transform, true);
        triggerColliderGO.transform.localScale = bodyCollider.transform.localScale;
        triggerColliderGO.AddComponent<BoxCollider>();
        triggerColliderGO.GetComponent<BoxCollider>().isTrigger = true;
        triggerColliderGO.GetComponent<BoxCollider>().size = bodyCollider.size;
        triggerColliderGO.GetComponent<BoxCollider>().center = bodyCollider.center;

        triggerCollider = triggerColliderGO.GetComponent<BoxCollider>();
        triggerCollider.size = new Vector3(bodyCollider.size.x * 1.5f, bodyCollider.size.y, bodyCollider.size.z + (bodyCollider.size.z * 3f));
        triggerCollider.center = new Vector3(bodyCollider.center.x, 0f, bodyCollider.center.z + (triggerCollider.size.z / 2f) - (bodyCollider.size.z / 2f));

        // Enabling lights if scene is a night or rainy scene.
        if (HR_GamePlayHandler.dayOrNight == HR_GamePlayHandler.DayOrNight.Night)
            headlightsOn = true;
        else
            headlightsOn = false;

        //  Creating engine sound.
        engineSoundSource = HR_CreateAudioSource.NewAudioSource(gameObject, "Engine Sound", 2f, 5f, 1f, engineSound, true, true, false);
        engineSoundSource.pitch = 1.5f;

        _maximumSpeed = maximumSpeed;
        desiredSpeed = maximumSpeed;     // spawn already cruising (same as before)

        //  Setting layer of the child gameobjects.
        foreach (Transform t in transform)
            t.gameObject.layer = (int)Mathf.Log(HR_HighwayRacerProperties.Instance.trafficCarsLayer.value, 2);

        //  Setting layer of the trigger volume.
        triggerCollider.gameObject.layer = LayerMask.NameToLayer("TrafficCarVolume");
    }

    private void Start()
    {
        isMultiplayer = PlayerPrefs.GetInt("Multiplayer", 0) == 1;

        // ✅ CLIENT MODE: Sirf visuals handle karo, AI nahi
        // if (isMultiplayer && !isServer)
        {
            //    rigid.isKinematic = true; // Client pe physics off
            //     Debug.Log($"👁️ CLIENT: Traffic car {gameObject.name} initialized (visual only)");
            //     return;
        }

        // ✅ SERVER or OFFLINE: Full AI chalaao
        InvokeRepeating("SpeedUp", 4f, 4f);
        InvokeRepeating("ChangeLines", Random.Range(15, 45), Random.Range(15, 45));

        // if (isServer)
        Debug.Log($"🤖 SERVER: Traffic car {gameObject.name} AI started");
    }

    private void Update()
    {
        // ✅ CLIENT: Server se synced state display karo
        //if (isMultiplayer && !isServer)
        //{
        //    // Smooth interpolation for position and rotation
        //    transform.position = Vector3.Lerp(transform.position, syncedPos, 20 * Time.deltaTime);
        //    transform.rotation = Quaternion.Lerp(transform.rotation, syncedRot, 20 * Time.deltaTime);
        //    desiredSpeed = syncedSpeed; // For wheels rotation
        //}

        // Everyone runs lights and wheels
        spawnProtection += Time.deltaTime;
        Lights();
        Wheels();
    }

    /// <summary>
    /// Lights of the car.
    /// </summary>
    private void Lights()
    {
        signalTimer += Time.deltaTime;

        for (int i = 0; i < signalLights.Length; i++)
        {
            if (signalsOn == SignalsOn.Off)
                signalLights[i].intensity = 0f;

            if (signalsOn == SignalsOn.Left)
            {
                if (signalTimer >= .5f)
                {
                    if (signalLights[i].transform.localPosition.x < 0f)
                        signalLights[i].intensity = 0f;
                }
                else
                {
                    if (signalLights[i].transform.localPosition.x < 0f)
                        signalLights[i].intensity = 1f;
                }

                if (signalTimer >= 1f)
                    signalTimer = 0f;
            }

            if (signalsOn == SignalsOn.Right)
            {
                if (signalTimer >= .5f)
                {
                    if (signalLights[i].transform.localPosition.x > 0f)
                        signalLights[i].intensity = 0f;
                }
                else
                {
                    if (signalLights[i].transform.localPosition.x > 0f)
                        signalLights[i].intensity = 1f;
                }

                if (signalTimer >= 1f)
                    signalTimer = 0f;
            }

            if (signalsOn == SignalsOn.All)
            {
                if (signalTimer >= .5f)
                    signalLights[i].intensity = 0f;
                else
                    signalLights[i].intensity = 1f;

                if (signalTimer >= 1f)
                    signalTimer = 0f;
            }
        }

        for (int i = 0; i < headLights.Length; i++)
        {
            if (!headlightsOn)
                headLights[i].intensity = 0f;
            else
                headLights[i].intensity = 1f;
        }

        for (int i = 0; i < brakeLights.Length; i++)
        {
            if (brakingOn)
            {
                brakeLights[i].intensity = 1f;
            }
            else
            {
                if (!headlightsOn)
                    brakeLights[i].intensity = 0f;
                else
                    brakeLights[i].intensity = .6f;
            }
        }
    }

    /// <summary>
    /// Wheels rotation.
    /// </summary>
    private void Wheels()
    {
        for (int i = 0; i < wheelModels.Length; i++)
        {
            wheelRotation += desiredSpeed * 20 * Time.deltaTime;
            wheelModels[i].transform.localRotation = Quaternion.Euler(wheelRotation, 0f, 0f);
        }
    }

    private void FixedUpdate()
    {
        // ✅ CLIENT: Kuch nahi karna, sirf Update() mein interpolation
        //  if (isMultiplayer && !isServer)
        //      return;

        // ========== SERVER + OFFLINE: Full AI Logic ==========

        // Adjusting desired speed - smooth acceleration and realistic braking
        float targetSpeed = crashed ? 0f : Mathf.Clamp(maximumSpeed - Mathf.Lerp(maximumSpeed, 0f, (distance - 10f) / 50f), 0f, maximumSpeed);
        float rate = crashed ? brakeDeceleration * 0.35f : (targetSpeed < desiredSpeed ? brakeDeceleration : acceleration);
        desiredSpeed = Mathf.MoveTowards(desiredSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        // Braking distance (brake lights also when actually slowing down)
        if (distance < 50 || crashed || targetSpeed < desiredSpeed - 0.5f)
            brakingOn = true;
        else
            brakingOn = false;

        // If mode is not two ways, adjust steering angle
        if (!crashed && HR_GamePlayHandler.mode != HR_GamePlayHandler.Mode.TwoWay)
            transform.rotation = Quaternion.Lerp(transform.rotation, steeringAngle, Time.fixedDeltaTime * 3f);

        // Setting linear and angular velocity
        if (!crashed)
        {
            rigid.linearVelocity = Vector3.Slerp(rigid.linearVelocity, transform.forward * desiredSpeed, Time.fixedDeltaTime * 3f);
            rigid.angularVelocity = Vector3.Slerp(rigid.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
        }
        else
        {
            // After a crash physics takes over: the car slides, spins and slows down naturally (no snapping straight)
            rigid.linearVelocity = Vector3.Lerp(rigid.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 0.8f);
        }

        // Line changing logic
        if (!crashed && HR_GamePlayHandler.mode != HR_GamePlayHandler.Mode.TwoWay)
        {
            currentLine = Mathf.Clamp(currentLine, 0, 3);

            switch (changingLines)
            {
                case ChangingLines.Straight:
                    steeringAngle = Quaternion.identity;
                    laneChangeTimer = 0f;
                    break;

                case ChangingLines.Left:
                    if (currentLine == 0)
                    {
                        changingLines = ChangingLines.Straight;
                        break;
                    }
                    if (transform.position.x <= HR_TrafficPooling.lines[currentLine - 1].position.x + .5f)
                    {
                        currentLine--;
                        signalsOn = SignalsOn.Off;
                        changingLines = ChangingLines.Straight;
                    }
                    else
                    {
                        signalsOn = SignalsOn.Left;
                        laneChangeTimer += Time.fixedDeltaTime;   // blink first, then move over
                        steeringAngle = laneChangeTimer >= signalTimeBeforeLaneChange
                            ? Quaternion.identity * Quaternion.Euler(0f, -laneChangeAngle, 0f)
                            : Quaternion.identity;
                    }
                    break;

                case ChangingLines.Right:
                    if (currentLine == 3)
                    {
                        changingLines = ChangingLines.Straight;
                        break;
                    }
                    if (transform.position.x >= HR_TrafficPooling.lines[currentLine + 1].position.x - .5f)
                    {
                        currentLine++;
                        signalsOn = SignalsOn.Off;
                        changingLines = ChangingLines.Straight;
                    }
                    else
                    {
                        signalsOn = SignalsOn.Right;
                        laneChangeTimer += Time.fixedDeltaTime;
                        steeringAngle = laneChangeTimer >= signalTimeBeforeLaneChange
                            ? Quaternion.identity * Quaternion.Euler(0f, laneChangeAngle, 0f)
                            : Quaternion.identity;
                    }
                    break;
            }
        }

        // ✅ SERVER: Sync variables update karo (clients automatically receive karenge)
        //  if (isMultiplayer && isServer)
        {
            //     syncedPos = transform.position;
            //     syncedRot = transform.rotation;
            //     syncedSpeed = desiredSpeed;
        }
    }

    private void OnTriggerStay(Collider col)
    {
        // Clients pe trigger logic nahi chalani
        //if (isMultiplayer && !isServer)
        //    return;

        if ((1 << col.gameObject.layer) != HR_HighwayRacerProperties.Instance.trafficCarsLayer.value || col.isTrigger)
            return;

        distance = Vector3.Distance(transform.position, col.transform.position);
    }

    private void OnCollisionEnter(Collision col)
    {
        if (crashed || spawnProtection < .5f)
            return;

        if (col.relativeVelocity.magnitude < crashSpeedThreshold)
            return;     // light bump: keep driving

        crashed = true;
        signalsOn = SignalsOn.All;
    }

    public void OnReAligned()
    {
        crashed = false;
        spawnProtection = 0f;
        rigid.linearVelocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;
        signalsOn = SignalsOn.Off;
        changingLines = ChangingLines.Straight;
        maximumSpeed = Random.Range(_maximumSpeed, _maximumSpeed * 1.5f);
        desiredSpeed = maximumSpeed;
        laneChangeTimer = 0f;
        distance = 50f;
    }

    /// <summary>
    /// Speeds up the car.
    /// </summary>
    private void SpeedUp()
    {
        distance = 50f;
    }

    /// <summary>
    /// Switches the lines.
    /// </summary>
    private void ChangeLines()
    {
        if (changingLines == ChangingLines.Left || changingLines == ChangingLines.Right)
            return;

        int randomNumber = Random.Range(0, 2);

        changingLines = randomNumber == 0 ? ChangingLines.Left : ChangingLines.Right;
    }

    /// <summary>
    /// ✅ RPC to sync active/inactive state across all clients
    /// Server calls this, all clients receive
    /// </summary>
    [ClientRpc]
    public void RpcSetActiveState(bool isActive)
    {
        // Skip if this is the server (already set locally)
        if (isServer) return;

        gameObject.SetActive(isActive);

        if (isActive)
            Debug.Log($"🟢 CLIENT: Activated traffic {trafficIndex} - {gameObject.name}");
        else
            Debug.Log($"🔴 CLIENT: Deactivated traffic {trafficIndex} - {gameObject.name}");
    }

    private void Reset()
    {
        Rigidbody rigidbody = GetComponent<Rigidbody>();

        if (!rigidbody)
            rigidbody = gameObject.AddComponent<Rigidbody>();

        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.mass = 1500f;
        rigidbody.linearDamping = 1f;
        rigidbody.angularDamping = 4f;
        rigidbody.maxAngularVelocity = 2.5f;
        rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
    }
}

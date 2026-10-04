//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using CarRace;
using UnityEngine.SceneManagement;

/// <summary>
/// Car camera with three modes.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Camera/HR Car Camera")]
public class HR_CarCamera : MonoBehaviour {
public static HR_CarCamera Instance;
    public CameraMode cameraMode = CameraMode.Top;       //  Camera modes.
    public enum CameraMode { Top, TPS, FPS }

    private GameObject audioListener;   //  Audio listener.

    internal int cameraSwitchCount = 0; //  Current camera mode as int.

    private RCC_HoodCamera hoodCam;     //  Hood camera transform.

    private float targetFieldOfView = 50f;      //  Field of the camera will be adapted to this value.
    public float topFOV = 48f;      //  FOV for top mode.
    public float tpsFOV = 55f;      //  FOV for tps mode.
    public float fpsFOV = 65f;      //  FOV for fps mode.

    // The target we are following
    public Transform playerCar;
    private Rigidbody playerRigid;
    public bool gameover = false;

    private Camera cam;     //  Actual camera.
    private Vector3 targetPosition = new Vector3(0, 0, 50);     //  Target position.
    private Vector3 pastFollowerPosition, pastTargetPosition;

    // The distance in the x-z plane to the target
    public float distance = 12f;

    // The height we want the camera to be above the target
    public float height = 8.5f;

    //  X Rotation of the camera.
    public float rotation = 30f;

    private float currentT;
    private float oldT;

    private float speed = 0f;

    public GameObject mirrors;

    public void Awake()
    {
        Instance=this;
    }
    private void Start() {

        //  Getting camera component.
        cam = GetComponent<Camera>();

        //  Setting very first position and rotation of the camera (before the intro animation).
        transform.position = new Vector3(2f, 1f, 55f);
        transform.rotation = Quaternion.Euler(new Vector3(0f, -40f, 0f));

        //  If camera has an audiolistener, destroy it and create a new one.
        AudioListener aListener = GetComponent<AudioListener>();

        if (aListener)
            Destroy(aListener);

        //  Creating audio listener.
        audioListener = new GameObject("Audio Listener");
        audioListener.transform.SetParent(transform, false);
        audioListener.AddComponent<AudioListener>();

    }
    
    private void OnEnable() {

        //  Listening events when player spawns, dies, and change cameras.
        if (SceneManager.GetActiveScene().name == "HighwayNight")
        {
            HR_NetworkManager.OnPlayerSpawned += OnPlayerSpawned;
        }
        HR_GamePlayHandler.OnPlayerSpawned += OnPlayerSpawned;
        HR_GamePlayHandler.OnPlayerDied += OnPlayerCrashed;
        RCC_InputManager.OnChangeCamera += RCC_InputManager_OnChangeCamera;

    }
    

    /// <summary>
    /// When player changes the camera mode.
    /// </summary>
    private void RCC_InputManager_OnChangeCamera() {

        ChangeCamera();

    }

    /// <summary>
    /// When player spawns, set player variable and get hood and tps camera locations.
    /// </summary>
    /// <param name="player"></param>
    public void OnPlayerSpawned(HR_PlayerHandler player) {

        //  Multiplayer me HR_GamePlayHandler.SpawnCar apni car spawn nahi karti, phir bhi
        //  OnPlayerSpawned(null) fire kar deti hai - us null par yahan NRE aata tha.
        if (player == null)
            return;

        //  Nayi car mili hai, yani race dobara chal rahi hai (reconnect ke baad bhi).
        gameover = false;

        playerCar = player.transform;
        playerRigid = player.GetComponent<Rigidbody>();
        hoodCam = player.GetComponentInChildren<RCC_HoodCamera>();

        if (GameObject.Find("Mirrors"))
            mirrors = GameObject.Find("Mirrors").gameObject;

    }

    /// <summary>
    /// When player dies.
    /// </summary>
    /// <param name="player"></param>
    private void OnPlayerCrashed(HR_PlayerHandler player, int[] scores) {

        gameover = true;

    }

    private void LateUpdate() {

        //  If no player car found yet, try to resolve it before giving up.
        if (!playerCar)
            ResolvePlayerCar();

        if (!playerCar)
            return;

        //  Animating the camera at spawn sequence.
        if (!playerCar || !playerRigid || Time.timeSinceLevelLoad < 1.5f) {

            transform.position += Quaternion.identity * Vector3.forward * (Time.deltaTime * 3f);

        } else if (playerCar && playerRigid) {

            //  Setting FOV of the camera.
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFieldOfView, Time.deltaTime * 3f);

            //  Running corresponding method with current camera mode.
            if (!gameover) {

                switch (cameraMode) {

                    case CameraMode.Top:

                        TOP();

                        if (mirrors)
                            mirrors.SetActive(false);

                        break;

                    case CameraMode.TPS:

                        TPS();

                        if (mirrors)
                            mirrors.SetActive(false);

                        break;

                    case CameraMode.FPS:

                        if (hoodCam) {

                            FPS();

                            if (mirrors)
                                mirrors.SetActive(true);

                        } else {

                            cameraSwitchCount++;
                            ChangeCamera();

                        }

                        break;

                }

            } else {

                //  After the animation, track the player car.
                if (Time.timeScale >= 1)
                    transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z + Mathf.Clamp(currentT, 0f, Mathf.Infinity));

            }

            //  Setting proper camera mode with int.
            switch (cameraSwitchCount) {

                case 0:
                    cameraMode = CameraMode.Top;
                    break;
                case 1:
                    cameraMode = CameraMode.TPS;
                    break;
                case 2:
                    cameraMode = CameraMode.FPS;
                    break;

            }

        }

        //  Setting position of the audiolistener.
        audioListener.transform.position = new Vector3(playerCar.position.x, transform.position.y, transform.position.z);

        pastFollowerPosition = transform.position;
        pastTargetPosition = targetPosition;

        currentT = (transform.position.z - oldT);
        oldT = transform.position.z;

    }

    /// <summary>
    /// Camera sirf ek dafa bind hoti hai - Target_AssignCar se. Reconnect ke baad wo message
    /// miss ho jaye (ya camera ke Awake se pehle aa jaye) to playerCar khali reh jata tha aur
    /// LateUpdate pehli line par hi return karta reh jata tha - camera hamesha ke liye jam.
    /// Apni car reconnect ke baad khud ko RCC player vehicle register karti hai, dekho
    /// HighwayCarNetwork.OnStartClient, is liye target wahan se dobara resolve ho jata hai.
    /// HighwayNight me RCC ka auto-register band hai (HR_NetworkManager.Awake), is liye yahan
    /// opponent ki car kabhi nahi milti.
    /// </summary>
    private void ResolvePlayerCar() {

        if (RCC_SceneManager.Instance == null)
            return;

        RCC_CarControllerV3 activeVehicle = RCC_SceneManager.Instance.activePlayerVehicle;

        if (activeVehicle == null)
            return;

        HR_PlayerHandler handler = activeVehicle.GetComponent<HR_PlayerHandler>();

        if (handler == null)
            return;

        OnPlayerSpawned(handler);

    }

    /// <summary>
    /// Changes the camera mode.
    /// </summary>
    public void ChangeCamera() {

        cameraSwitchCount++;

        if (cameraSwitchCount >= 3)
            cameraSwitchCount = 0;

    }

    /// <summary>
    /// Top camera mode.
    /// </summary>
    private void TOP() {

        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(rotation, 0f, 0f), Time.deltaTime * 2f);

        targetPosition = new Vector3(0f, playerCar.position.y, playerCar.position.z);
        targetPosition -= transform.rotation * Vector3.forward * distance;

        targetPosition = new Vector3(targetPosition.x, height, targetPosition.z);

        if (Time.timeSinceLevelLoad < 3f)
            transform.position = SmoothApproach(pastFollowerPosition, pastTargetPosition, targetPosition, (speed / 2f) * Mathf.Clamp(Time.timeSinceLevelLoad - 1.5f, 0f, 10f));
        else
            transform.position = targetPosition;

        targetFieldOfView = topFOV;

    }

    /// <summary>
    /// TPS camera mode.
    /// </summary>
    private void TPS() {

        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(rotation / 3f, 0f, 0f), Time.deltaTime * 2f);
        targetPosition = new Vector3(playerCar.position.x, height / 3f, playerCar.position.z - (distance / 1.75f));
        transform.position = targetPosition;

        targetFieldOfView = tpsFOV;

    }

    /// <summary>
    /// FPS camera mode.
    /// </summary>
    private void FPS() {

        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, Time.deltaTime * 2f);

        if (HR_HighwayRacerProperties.Instance._tiltCamera)
            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.InverseTransformDirection(playerRigid.linearVelocity).x / 2f, -transform.InverseTransformDirection(playerRigid.linearVelocity).x / 2f);

        targetPosition = hoodCam.transform.position;
        transform.position = targetPosition;
        targetFieldOfView = fpsFOV;

    }

    /// <summary>
    /// Used for smooth position lerping.
    /// </summary>
    /// <param name="pastPosition"></param>
    /// <param name="pastTargetPosition"></param>
    /// <param name="targetPosition"></param>
    /// <param name="delta"></param>
    /// <returns></returns>
    private Vector3 SmoothApproach(Vector3 pastPosition, Vector3 pastTargetPosition, Vector3 targetPosition, float delta) {

        if (Time.timeScale == 0 || float.IsNaN(delta) || float.IsInfinity(delta) || delta == 0 || pastPosition == Vector3.zero || pastTargetPosition == Vector3.zero || targetPosition == Vector3.zero)
            return transform.position;

        float t = (Time.deltaTime * delta) + .00001f;
        Vector3 v = (targetPosition - pastTargetPosition) / t;
        Vector3 f = pastPosition - pastTargetPosition + v;
        Vector3 l = targetPosition - v + f * Mathf.Exp(-t);

        if (l != Vector3.negativeInfinity && l != Vector3.positiveInfinity && l != Vector3.zero)
            return l;
        else
            return transform.position;

    }

    private void FixedUpdate() {

        //  If no player rigid found yet, return.
        if (!playerRigid)
            return;

        //  Getting speed of the car.
        speed = Mathf.Lerp(speed, (playerRigid.linearVelocity.magnitude * 3.6f), Time.deltaTime * 1.5f);

    }

    private void OnDisable() {

        if (SceneManager.GetActiveScene().name == "HighwayNight")
        {
            HR_NetworkManager.OnPlayerSpawned -= OnPlayerSpawned;
        }
        HR_GamePlayHandler.OnPlayerSpawned -= OnPlayerSpawned;
        HR_GamePlayHandler.OnPlayerDied -= OnPlayerCrashed;
        RCC_InputManager.OnChangeCamera -= RCC_InputManager_OnChangeCamera;

    }

}
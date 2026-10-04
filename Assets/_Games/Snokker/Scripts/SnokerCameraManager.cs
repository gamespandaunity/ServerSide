using NaughtyAttributes;
 
using System.Collections;
using UnityEngine;
using UnityStandardAssets.ImageEffects;

public class SnokerCameraManager : MonoBehaviour
{
    public SnokerGameManager _SnokerGameManager;
    public Transform cameraObjTransform;
    public Camera cameraObjCamera;
    public BlurOptimized blurview;

    public Transform camParentObjMainMenuTransform;

    public GameObject camParentObjInGame;

    public Transform camParentObjInGameTransform;

    public Transform cameraTopParentObjTransform;

    public Transform cameraAiParentObjTransform;

    public Transform cameraFreeViewParentObjTransform;
    public float CAMERA_Y_MIN = 10f;

    public float CAMERA_Y_MAX = 70f;

    public float CAMERA_DISTANCE_MIN = 10f;

    public float CAMERA_DISTANCE_MAX = 45f;

    public CAMERA_MODE cameraMode = CAMERA_MODE.NORMAL;

    public float camDistance = 10f;

    public Quaternion camParentRotation;

    public float camParentRotValueY = 40f;

    public float camParentRotTargetY = 30f;

    public float camParentRotVelY;

    public Vector3 camParentPosVel;

    public bool camCanRotate = true;

    public bool camEasingActive;

    public Vector3 camLocalPosVel = Vector3.zero;

    public Vector3 camLocalRotVel = Vector3.zero;

    public float cameraAnimSpeed;


    public Color camTopAmbientColor = Color.black;
    public Matrix4x4 ortho;

    public Matrix4x4 perspective;
    public Matrix4x4 aiOrtho;
    public Matrix4x4 pocketCamera;

    public float near = 5f;

    public float far = 1000f;

    public float orthoSize = 20f;

    public float orthoAspect;


    public Color camNormalAmbientColor = new Color(0.654902f, 0.654902f, 0.654902f, 1f);

    public Vector3 CAM_MENU_PARENT_DEFAULT_POS = new Vector3(28f, 37f, -43f);

    public Vector3 CAM_MENU_PARENT_DEFAULT_ROT = new Vector3(27f, 317f, 0f);

    public Vector3 CAM_FIRST_START_POS = new Vector3(0f, 16.25f, 21f);

    private Vector3 cachedThisPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        camParentObjMainMenuTransform.position = CAM_MENU_PARENT_DEFAULT_POS;
        camParentObjMainMenuTransform.eulerAngles = CAM_MENU_PARENT_DEFAULT_ROT;

    }


    public void blurGameView(bool val)
    {
        blurview.enabled = val;
    }
    // Update is called once per frame
    void Update()
    {
        if (_SnokerGameManager._SnokerCueBall.initialized&& SnookerCamera._instance)
        {

            cachedThisPosition = _SnokerGameManager._SnokerCueBall.transform.position;
            HandleCameraPositioning();
            getcurrentMatrix();
        }
        //  cameraSwitchMode(CAMERA_MODE.POCKET);
    }
    private void HandleCameraPositioning()
    {
       
         if (cameraObjTransform.parent == cameraAiParentObjTransform)
        {
            HandleAICamera();
        }
        else if (cameraObjTransform.parent == camParentObjInGameTransform)
        {
            HandleInGameCamera();
        }
        else if (cameraObjTransform.parent == cameraTopParentObjTransform)
        {
            HandleTopCamera();
        }
        else if (cameraObjTransform.parent.parent == SnookerCamera._instance.cameras[0].parent)//&& _SnokerGameManager._SnokerCueBall.firstTargetBallToHit)
        {
            Camera kh = cameraObjCamera;
            kh.projectionMatrix = pocketCamera;
            kh.nearClipPlane = 0.01f;
            kh.farClipPlane = 1500f;
            if (_SnokerGameManager._SnokerCueBall.firstTargetBallToHit)
            {
                Vector3 direction = _SnokerGameManager._SnokerCueBall.firstTargetBallToHit.transform.position - cameraObjTransform.position;
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                cameraObjTransform.rotation = Quaternion.RotateTowards(cameraObjTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    public float rotationSpeed = 0.01f;
   
    private void HandleAICamera()
    {
        cameraObjTransform.localPosition = Vector3.SmoothDamp(cameraObjTransform.localPosition,
            Vector3.zero, ref camLocalPosVel, 0.01f);
        setCameraLocalRotSmooth(0.01f);
        cameraObjCamera.projectionMatrix = aiOrtho;
        Camera cam1 = cameraObjCamera;
        cam1.nearClipPlane = 0.01f;
        cam1.farClipPlane = 1500f;
    }

    private void HandleInGameCamera()
    {
        if (!float.IsNaN(camDistance) && !float.IsNaN(camParentRotValueY))
        {
            if (_SnokerGameManager.bTossDone)
            {
                Vector3 targetPos = new Vector3(0f, 0, -camDistance - camParentRotValueY / 2.4f);
                cameraObjTransform.localPosition = Vector3.SmoothDamp(cameraObjTransform.localPosition,
                    targetPos, ref camLocalPosVel, 0.01f);
            }
            else
            {
                Vector3 targetPos = new Vector3(0f, 0, -camDistance - camParentRotValueY / 1.4f);
                cameraObjTransform.localPosition = Vector3.SmoothDamp(cameraObjTransform.localPosition,
                    targetPos, ref camLocalPosVel, 0.01f);
            }
            if (_SnokerGameManager.bTossDone)
            {
                cameraObjCamera.projectionMatrix = perspective;
                cameraObjCamera.orthographicSize = 1.3f; // Or whatever suits your scene
                cameraObjCamera.nearClipPlane = 0.01f;
                cameraObjCamera.farClipPlane = 1500f; // Increase it since you’re hitting 1500 depth
            }
        }

        setCameraLocalRotSmooth(0.01f);

        RenderSettings.ambientEquatorColor = camNormalAmbientColor;
        if (!SnokerGameManager.bGameOver)
        {

            Vector3 targetCamPos = _SnokerGameManager.ballIsStanding ? cachedThisPosition : _SnokerGameManager._SnokerCueBall.cueBallPosOnHit;
            camParentObjInGameTransform.position = Vector3.SmoothDamp(camParentObjInGameTransform.position,
                targetCamPos, ref camParentPosVel, 0.01f);
        }
    }
   
    public Matrix4x4 currentCameraMatrix;

    public void getcurrentMatrix()
    {
        currentCameraMatrix = cameraObjCamera.projectionMatrix;
    }


    private void HandleTopCamera()
    {
        cameraObjTransform.localPosition = Vector3.SmoothDamp(cameraObjTransform.localPosition,
            Vector3.zero, ref camLocalPosVel, 0.01f);
        setCameraLocalRotSmooth(0.01f);
        orthoAspect = (float)Screen.width / (float)Screen.height;
        cameraObjCamera.aspect = orthoAspect;
        cameraObjCamera.projectionMatrix = ortho;

    }

    private void setCameraLocalRotSmooth(float timeVal)
    {
        Vector3 localEulerAngles = cameraObjTransform.localEulerAngles;
        localEulerAngles.x = Mathf.SmoothDampAngle(cameraObjTransform.localEulerAngles.x, 0f, ref camLocalRotVel.x, timeVal);
        localEulerAngles.y = Mathf.SmoothDampAngle(cameraObjTransform.localEulerAngles.y, 0f, ref camLocalRotVel.y, timeVal);
        localEulerAngles.z = Mathf.SmoothDampAngle(cameraObjTransform.localEulerAngles.z, 0f, ref camLocalRotVel.z, timeVal);
        cameraObjTransform.localEulerAngles = localEulerAngles;
    }
  
    public void cameraSwitchMode(CAMERA_MODE val)
    {
        _SnokerGameManager._SnokerUIManager.cameraButtonObj.SetActive(true);
        //if (SnokerNetwork.IsMultiplayer)
        //{
        //    _SnokerGameManager.isyourturn = (PhotonNetwork.IsMasterClient && SnokerGameManager.currentTurn == TURN.PLAYER_1) ||
        //                            (!PhotonNetwork.IsMasterClient && SnokerGameManager.currentTurn == TURN.PLAYER_2);
        //    if (!_SnokerGameManager.isyourturn)
        //    {
        //        cameraObjTransform.parent = cameraAiParentObjTransform;
        //        cameraObjCamera.projectionMatrix = aiOrtho;
        //        Camera cam1 = cameraObjCamera;
        //        cam1.nearClipPlane = 0.01f;
        //        cam1.farClipPlane = 1500f; // Increase it since you’re hitting 1500 depth
        //        RenderSettings.ambientEquatorColor = camNormalAmbientColor;
        //        _SnokerGameManager._SnokerUIManager.cameraButtonObj.SetActive(false);
        //        return;
        //    }
        //} 
        switch (val)
        {
            case CAMERA_MODE.NORMAL:
                cameraObjTransform.parent = camParentObjInGameTransform;
                Camera cam = cameraObjCamera;

                if (_SnokerGameManager.bTossDone)
                {
                    cameraObjCamera.projectionMatrix = perspective;
                    cam.orthographicSize = 1.3f; // Or whatever suits your scene
                    cam.nearClipPlane = 0.01f;
                    cam.farClipPlane = 1500f; // Increase it since you’re hitting 1500 depth
                }
                else
                {
                  Invoke( nameof(DoCamSetting),1f);
                }
                    break;
            case CAMERA_MODE.TOP:
                cameraObjTransform.parent = cameraTopParentObjTransform;
                cameraObjCamera.projectionMatrix = ortho;
                cameraObjTransform.localPosition = Vector3.zero;
                cameraObjTransform.localRotation = Quaternion.identity;
                cameraObjCamera.orthographicSize = 1.3f; // Or whatever suits your scene
                cameraObjCamera.nearClipPlane = 0.01f;
                cameraObjCamera.farClipPlane = 1500f; // Increase it since you’re hitting 1500 depth
                break;
            case CAMERA_MODE.AI:
                cameraObjTransform.parent = cameraAiParentObjTransform;
                cameraObjCamera.projectionMatrix = aiOrtho;
                Camera cam1 = cameraObjCamera;
                cam1.nearClipPlane = 0.01f;
                cam1.farClipPlane = 1500f; 
                _SnokerGameManager._SnokerUIManager.cameraButtonObj.SetActive(false);
                break;        
            case CAMERA_MODE.POCKET:
                cameraObjTransform.parent = SnookerCamera._instance.cameras[int.Parse(_SnokerGameManager._SnokerCueBall.closestPot.name) - 1];
                cameraObjTransform.localPosition = Vector3.zero;
                cameraObjTransform.localRotation = Quaternion.identity;
                cameraObjTransform.rotation = new Quaternion(0, 0, 0, 0);
                cameraObjCamera.projectionMatrix = pocketCamera;
                Camera kh = cameraObjCamera;
                kh.nearClipPlane = 0.01f;
                kh.farClipPlane = 1500f; // Increase it since you’re hitting 1500 depth
                break;
        }
    }
    void DoCamSetting()
    {
        cameraObjCamera.projectionMatrix = perspective;
        cameraObjCamera.orthographicSize = 1.3f; // Or whatever suits your scene
        cameraObjCamera.nearClipPlane = 0.01f;
        cameraObjCamera.farClipPlane = 1500f; // Increase it since you’re hitting 1500 depth
        RenderSettings.ambientEquatorColor = camNormalAmbientColor;
    }
}

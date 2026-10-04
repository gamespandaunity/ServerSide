using UnityEngine;
using UnityEngine.Serialization;

public class HorseCameraOrbit : MonoBehaviour
{
    [FormerlySerializedAs("target")] public Transform followTarget;
    [FormerlySerializedAs("autoRotateOn")] public bool isAutoRotateEnabled  = false;
    [FormerlySerializedAs("autoRotateReverse")] public bool isAutoRotateReversed  = false;
    [FormerlySerializedAs("autoRotateSpeed")] public float autoRotateSpeedNormal  = 1f;
    [FormerlySerializedAs("autoRotateSpeedFast")] public float autoRotateSpeedBoosted  = 5f;
    [FormerlySerializedAs("distance")] public float cameraDistance  = 1.5f;
    [FormerlySerializedAs("distanceMin")] public float minCameraDistance  = 1f;
    [FormerlySerializedAs("distanceMax")] public float maxCameraDistance  = 3f;
    [FormerlySerializedAs("speed")] public float generalSpeed  = 1;
    [FormerlySerializedAs("xSpeed")] public float rotationSpeedX  = 1.0f;
    [FormerlySerializedAs("ySpeed")] public float rotationSpeedY  = 1.0f;
    [FormerlySerializedAs("yMaxLimit")] public float verticalRotationMax  = 80f;
    [FormerlySerializedAs("yMinLimit")] public float verticalRotationMin  = -20f;
    [FormerlySerializedAs("smoothTime")] public float rotationSmoothTime  = 2f;
    [FormerlySerializedAs("autoTimer")] public float autoRotateDelay  = 5f;
    [FormerlySerializedAs("collision")] public bool enableCollisionCheck  = false;
    [FormerlySerializedAs("ismainmenu")] public bool isInMainMenu  = false;


    float originalAutoRotateSpeed;
    float autoRotateDirection  = 1;

    //#if UNITY_ANDROID
    //#else
    //    public float xSpeed = 15.0f;
    //    public float ySpeed = 15.0f;
    //#endif



    float currentRotationY = 0.0f;
    float currentRotationX  = 0.0f;

    float rotationVelocityX  = 0.0f;
    float rotationVelocityY = 0.0f;
    bool isFastRotation;
    private bool isRightKeyPressed;

    void OnEnable()
    {
        if (HudMenuManager.instance)
        {
            HudMenuManager.instance.cameraTutorialPanel.SetActive(true);
        }
    }

    private void OnDisable()
    {
        if (HudMenuManager.instance)
        {
            HudMenuManager.instance.cameraTutorialPanel.SetActive(false);
        }
    }

    void Start()
    {
        isRightKeyPressed = isAutoRotateEnabled ;
        autoRotateDirection  = 1;
        Vector3 angles = transform.eulerAngles;
        currentRotationY = angles.y + 50;
        currentRotationX  = angles.x - 3;
        originalAutoRotateSpeed = autoRotateSpeedNormal ;
        if (GetComponent<Rigidbody>())
        {
            GetComponent<Rigidbody>().freezeRotation = true;
        }
    }


    private void Update()
    {

        if (isAutoRotateEnabled )
        {
            rotationVelocityX  += (autoRotateSpeedNormal  * autoRotateDirection ) * Time.deltaTime;
        }
        if (Input.GetKeyUp("r") && isAutoRotateEnabled  == false)
        {
            isAutoRotateEnabled  = true;
            isRightKeyPressed = true;

        }
        else if (Input.GetKeyUp("r") && isAutoRotateEnabled  == true)
        {
            isAutoRotateEnabled  = false;
            isRightKeyPressed = false;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && (!isFastRotation))
        {
            isFastRotation = true;
            autoRotateSpeedNormal  = autoRotateSpeedBoosted ;
            isAutoRotateEnabled  = true;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift) && (isFastRotation))
        {
            isFastRotation = false;
            autoRotateSpeedNormal  = originalAutoRotateSpeed;
            if (isRightKeyPressed == false)
            {
                isAutoRotateEnabled  = false;
            }
        }

        if (isAutoRotateReversed  == true)
        {
            autoRotateDirection  = -1;
        }
        else
        {
            autoRotateDirection  = 1;
        }



    }


    void LateUpdate()
    {
        if (followTarget != null)
        {
            rotationVelocityX  += rotationSpeedX  * ControlFreak2.CF2Input.GetAxis("Mouse X") * generalSpeed  * 0.025f * Time.deltaTime * HudMenuManager.inputSensitivity;
            rotationVelocityY += rotationSpeedY  * ControlFreak2.CF2Input.GetAxis("Mouse Y") * 0.045f * Time.deltaTime * HudMenuManager.inputSensitivity;

            //}
            autoRotateSpeedNormal  = autoRotateSpeedNormal  / 2.0f;


            currentRotationY += rotationVelocityX ;
            currentRotationX  -= rotationVelocityY;

            currentRotationX  = LimitAngle(currentRotationX , verticalRotationMin , verticalRotationMax );

            Quaternion toRotation = Quaternion.Euler(currentRotationX , currentRotationY + followTarget.transform.eulerAngles.y, 0);
            Quaternion rotation = toRotation;
            cameraDistance  = Mathf.Clamp(cameraDistance  - Input.GetAxis("Mouse ScrollWheel") * 5, minCameraDistance , maxCameraDistance );

            if (enableCollisionCheck  == true)
            {
                RaycastHit hit;
                if (Physics.Linecast(followTarget.position, transform.position, out hit))
                {
                    cameraDistance  -= hit.distance;
                }
            }
            Vector3 negDistance = new Vector3(0.0f, 0.0f, -cameraDistance );
            Vector3 position = rotation * negDistance + followTarget.position;
            transform.rotation = rotation;
            transform.position = position;

            rotationVelocityX  = Mathf.Lerp(rotationVelocityX , 0, Time.deltaTime * rotationSmoothTime );
            rotationVelocityY = Mathf.Lerp(rotationVelocityY, 0, Time.deltaTime * rotationSmoothTime );
        }
        else
        {
            //			Constants_M.LogInfo ("Orbit Camera - No Target Set");
        }
    }

    public static float LimitAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }

    public void Reset()
    {
        if (isInMainMenu )
        {
            currentRotationY = 0;
            currentRotationX  = 0;
        }
    }

}

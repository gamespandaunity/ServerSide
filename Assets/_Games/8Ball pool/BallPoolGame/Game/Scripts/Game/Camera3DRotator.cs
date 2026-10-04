using UnityEngine;
using UnityEngine.EventSystems;
using BallPool;

public class Camera3DRotator : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private RectTransform point;
    private RectTransform rectTransform;
    private float radius;
    private float currentRadius;
    private Vector3 localPosition;
    private Vector3 checkLocalPosition;
    private bool canControl = false;
    [SerializeField] private Transform tableCameraCenter;
    [SerializeField] private float cameraRotateSpeed = 3.0f;
    private float yRotation;
    private float zRotation;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    void Start()
    {
        radius = 0.5f * (rectTransform.sizeDelta.x - point.sizeDelta.x);
    }
    void OnEnable()
    {
        localPosition = point.localPosition;
        //Debug.Log("OnEnable");
        InputOutput.OnMouseState += InputOutput_OnMouseState;
    }
    void OnDisable()
    {
        InputOutput.OnMouseState -= InputOutput_OnMouseState;
    }
    void InputOutput_OnMouseState (MouseState mouseState)
    {
        if (canControl && mouseState == MouseState.Up)
        {
            Resset();
            ShotController.isControlAvailable = true;
            canControl = false;
        }
        if (canControl && mouseState == MouseState.Press)
        {
            localPosition -= 0.3f * InputOutput.mouseScreenSpeed * Time.deltaTime;
            yRotation += -cameraRotateSpeed * localPosition.x * Time.deltaTime;
            zRotation += -cameraRotateSpeed * localPosition.y * Time.deltaTime;
            zRotation = Mathf.Clamp(zRotation, -15.0f, 0.0f);
            tableCameraCenter.localRotation = Quaternion.Euler(0.0f, yRotation, zRotation);
            currentRadius = Mathf.Sqrt(localPosition.x * localPosition.x + localPosition.y * localPosition.y);
            if (currentRadius < radius)
            {
                checkLocalPosition = localPosition;
                point.localPosition = localPosition;
            }
            else
            {
                localPosition = checkLocalPosition;
            }
        }
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        canControl = true;
        ShotController.isControlAvailable = false;
    }
    public void Resset()
    {
        point.localPosition = localPosition = Vector3.zero;
        ShotController.isControlAvailable = true;
    }
    public void SetPointTargetingPosition(Vector3 normalizedPosition)
    {
        point.localPosition = normalizedPosition * radius;
    }
}

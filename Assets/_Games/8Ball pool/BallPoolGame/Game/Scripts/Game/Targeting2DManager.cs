using UnityEngine;
using UnityEngine.EventSystems;
using BallPool;

/// <summary>
/// Manager for targeting on cue ball in 2D mode.
/// </summary>
public class Targeting2DManager : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private RectTransform point;

    [SerializeField] private RectTransform pointBG; //RAR


    [SerializeField] private ShotController shotController;
    private RectTransform rectTransform;
    private float radius;
    private float currentRadius;
    private Vector3 localPosition;
    private Vector3 checkLocalPosition;
    private bool canControl = false;




    public GameObject whiteBallScreenShow; //RAR


    public static Targeting2DManager instance;



    void Awake()
    {

        if(instance == null)
        {
            instance = this;
        }


        rectTransform = GetComponent<RectTransform>();
    }
    void Start()
    {
        radius = 0.5f * (rectTransform.sizeDelta.x - point.sizeDelta.x);

        radius = 0.5f * (rectTransform.sizeDelta.x - pointBG.sizeDelta.x); //RAR

    }
    void OnEnable()
    {
        localPosition = point.localPosition;

        localPosition = pointBG.localPosition; //RAR

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
            ShotController.isControlAvailable = true;
            canControl = false;

            whiteBallScreenShow.SetActive(false);//RAR
            shotController.enabled = true; //RAR

            shotController.ResetCueStateAfterTargeting();
        }
        if (canControl && mouseState == MouseState.PressAndMove)
        {
            localPosition -= 0.3f * InputOutput.mouseScreenSpeed * Time.deltaTime;
            currentRadius = Mathf.Sqrt(localPosition.x * localPosition.x + localPosition.y * localPosition.y);
            if (currentRadius < radius)
            {
                checkLocalPosition = localPosition;
                point.localPosition = localPosition;

                pointBG.localPosition = localPosition; //RAR

            }
            else
            {
                localPosition = checkLocalPosition;
            }
            SetCuePosition(-localPosition / radius);
        }
        // if(mouseState == MouseState.Down)
        //{

        //    Vector3 inputMousePosition = Input.mousePosition;
        //    Vector3 viewportPoint = Camera.main.ScreenToViewportPoint(inputMousePosition);
        //    point.localPosition = viewportPoint;
        //}
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        canControl = true;
        ShotController.isControlAvailable = false;
        shotController.PrepCueForTargeting();
        UpdatePointPosition(eventData);
    }
    private void UpdatePointPosition(PointerEventData eventData)
    {
        Vector2 localPoint;

        // Convert the screen position to local position within the RectTransform
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        // Clamp the local point to be within the specified radius
        localPoint = Vector2.ClampMagnitude(localPoint, radius);

        // Update the position of the point image
        pointBG.localPosition = localPoint;
        point.localPosition = localPoint;
        
    }

    private void SetCuePosition(Vector3 normalizedPosition)
    {
        shotController.CueAimSnap(normalizedPosition);
    }
    public void Resset()
    {
        point.localPosition = localPosition = Vector3.zero;

        pointBG.localPosition = localPosition = Vector3.zero; //RAR

        ShotController.isControlAvailable = true;
    }
    public void SetPointTargetingPosition(Vector3 normalizedPosition)
    {
        point.localPosition = normalizedPosition * radius;

        pointBG.localPosition = normalizedPosition * radius; //RAR

    }



   
    
}

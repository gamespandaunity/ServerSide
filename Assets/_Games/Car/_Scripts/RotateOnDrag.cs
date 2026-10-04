namespace CarRace 
{
using UnityEngine;
using UnityEngine.EventSystems;

public class RotateOnDrag : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private Transform targetObject;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float resetDelay = 4f;
    [SerializeField] private float resetRotationSpeed = 5f;
    private float timeSinceDragStop = 0f;
    private Vector2 dragStartPosition;
    private Quaternion initialRotation;

    private bool isDragging = false;
    private bool resetRotation = false;

    private void Start()
    {
        initialRotation = targetObject.rotation;
    }
    private void Update()
    {
        if (resetRotation)
        {
            timeSinceDragStop += Time.deltaTime;

            if (timeSinceDragStop >= resetDelay)
            {
                targetObject.rotation = Quaternion.Slerp(targetObject.rotation, initialRotation, Time.deltaTime * resetRotationSpeed);

                if (Quaternion.Angle(targetObject.rotation, initialRotation) < 0.1f)
                {
                    resetRotation = false;
                    timeSinceDragStop = 0f;
                }
            }
        }
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (isDragging)
            return;
        isDragging = true;
        dragStartPosition = eventData.position;
        resetRotation = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging)
        {
            Vector2 dragDelta = eventData.position - dragStartPosition;
            float rotationAmount = -dragDelta.x * rotationSpeed * Time.deltaTime;

            targetObject.Rotate(Vector3.up, rotationAmount);

            dragStartPosition = eventData.position;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        resetRotation = true;
        timeSinceDragStop = 0f;

    }
}

}
using UnityEngine;

namespace CameraSystem.Follow
{
    public class MouseFollowCamera : MonoBehaviour
    {
        [Header("Target Settings")]
        public Transform target;
        
        [Header("Camera Distance")]
        public float distance = 10f;
        public float minDistance = 3f;
        public float maxDistance = 20f;
        public float zoomSpeed = 2f;
        
        [Header("Mouse Sensitivity")]
        public float mouseSensitivity = 2f;
        public float verticalLimit = 80f;
        
        [Header("Smoothing")]
        public float rotationSmoothing = 5f;
        public float positionSmoothing = 5f;
        
        private float currentX = 0f;
        private float currentY = 0f;
        private Vector3 targetPosition;
        
        void Start()
        {
            // Initialize rotation based on current transform
            Vector3 angles = transform.eulerAngles;
            currentX = angles.y;
            currentY = angles.x;
            
            // Lock cursor to center of screen
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        void Update()
        {
            if (target == null) return;
            
            HandleMouseInput();
            HandleZoomInput();
            UpdateCameraPosition();
            
            // Toggle cursor lock with Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleCursorLock();
            }
        }
        
        private void HandleMouseInput()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                currentX += Input.GetAxis("Mouse X") * mouseSensitivity;
                currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                
                // Clamp vertical rotation
                currentY = Mathf.Clamp(currentY, -verticalLimit, verticalLimit);
            }
        }
        
        private void HandleZoomInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
        
        private void UpdateCameraPosition()
        {
            // Calculate desired rotation
            Quaternion targetRotation = Quaternion.Euler(currentY, currentX, 0);
            
            // Calculate position offset from target
            Vector3 offset = targetRotation * Vector3.back * distance;
            targetPosition = target.position + offset;
            
            // Smooth camera movement
            transform.position = Vector3.Lerp(transform.position, targetPosition, positionSmoothing * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * Time.deltaTime);
        }
        
        private void ToggleCursorLock()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        void OnDisable()
        {
            // Unlock cursor when component is disabled
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
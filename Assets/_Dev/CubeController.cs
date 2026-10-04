using UnityEngine;

namespace CubeMovement.Controllers
{
    public class CubeController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float acceleration = 10f;
        public float deceleration = 10f;
        
        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 targetVelocity = Vector3.zero;
        
        void Update()
        {
            HandleInput();
            UpdateMovement();
        }
        
        private void HandleInput()
        {
            // Reset target velocity
            targetVelocity = Vector3.zero;
            
            // Check for arrow key inputs
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                targetVelocity += Vector3.forward * moveSpeed;
            }
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                targetVelocity += Vector3.back * moveSpeed;
            }
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                targetVelocity += Vector3.left * moveSpeed;
            }
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                targetVelocity += Vector3.right * moveSpeed;
            }
        }
        
        private void UpdateMovement()
        {
            // Smooth velocity transition
            float lerpRate = targetVelocity.magnitude > 0 ? acceleration : deceleration;
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, lerpRate * Time.deltaTime);
            
            // Apply movement
            transform.Translate(currentVelocity * Time.deltaTime, Space.World);
        }
    }
}
using UnityEngine;

namespace Setup.GameObjects
{
    /// <summary>
    /// This script helps set up the cube movement scene automatically.
    /// Attach this to any GameObject in the scene and press Play.
    /// </summary>
    public class SceneSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [Tooltip("Enable this to automatically configure components when scene starts")]
        public bool autoConfigureOnStart = true;
        
        void Start()
        {
            if (autoConfigureOnStart)
            {
                SetupScene();
            }
        }
        
        [ContextMenu("Setup Scene")]
        public void SetupScene()
        {
            // Find the cube and camera
            GameObject cube = GameObject.Find("MovableCube");
            GameObject camera = GameObject.Find("Main Camera");
            
            if (cube == null)
            {
                Debug.LogError("MovableCube not found in scene!");
                return;
            }
            
            if (camera == null)
            {
                Debug.LogError("Main Camera not found in scene!");
                return;
            }
            
            // Add CubeController if it doesn't exist
            var cubeController = cube.GetComponent<CubeMovement.Controllers.CubeController>();
            if (cubeController == null)
            {
                cubeController = cube.AddComponent<CubeMovement.Controllers.CubeController>();
                Debug.Log("Added CubeController to MovableCube");
            }
            
            // Add MouseFollowCamera if it doesn't exist
            var mouseFollow = camera.GetComponent<CameraSystem.Follow.MouseFollowCamera>();
            if (mouseFollow == null)
            {
                mouseFollow = camera.AddComponent<CameraSystem.Follow.MouseFollowCamera>();
                mouseFollow.target = cube.transform;
                Debug.Log("Added MouseFollowCamera to Main Camera and set target to MovableCube");
            }
            else if (mouseFollow.target == null)
            {
                mouseFollow.target = cube.transform;
                Debug.Log("Set MouseFollowCamera target to MovableCube");
            }
            
            Debug.Log("Scene setup complete! Use Arrow Keys or WASD to move the cube, Mouse to control camera, Scroll to zoom, ESC to toggle cursor lock.");
        }
    }
}
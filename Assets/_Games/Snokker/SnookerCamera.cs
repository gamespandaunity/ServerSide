using UnityEngine;

public class SnookerCamera : MonoBehaviour
{
    public static SnookerCamera _instance;

    public Transform[] cameras;

    private void Awake()
    {
        _instance = this;
        //DontDestroyOnLoad(gameObject); // Optional, if you want the camera to persist between scenes
    }
}
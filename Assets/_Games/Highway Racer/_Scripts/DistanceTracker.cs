using UnityEngine;

public class DistanceTracker : MonoBehaviour
{
    public float maxDistance = 10f;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private float currentDistance;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {

    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 20), "Distance: " + currentDistance.ToString("F2"));
    }
}
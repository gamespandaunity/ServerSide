using CarRace;
using UnityEngine;

public class RCCGrassSlowdown : MonoBehaviour
{
    [Header("Road Layer")]
    public LayerMask roadLayer;  // RCC_WheelCollider layer select karo

    [Header("Off-Road Slowdown - Light")]
    public float offRoadTorque = 0.85f;    // 85% power (thoda kam)
    public float offRoadSpeed = 0.90f;     // 90% speed (thoda slow)
    public float offRoadDrag = 0.7f;       // Halka resistance

    [Header("Status")]
    public bool isOnRoad = true;

    private RCC_CarControllerV3 car;
    private Rigidbody rb;
    [SerializeField] private float normalTorque;
    [SerializeField] private float normalSpeed;
    [SerializeField] private float normalDrag;

    void Start()
    {
        car = GetComponent<RCC_CarControllerV3>();
        rb = GetComponent<Rigidbody>();

        normalTorque = car.maxEngineTorque;
        normalSpeed = car.maxspeed;
        normalDrag = rb.linearDamping;
    }

    void FixedUpdate()
    {
        RaycastHit hit;
        bool hitRoad = Physics.Raycast(transform.position, -Vector3.up, out hit, 2f, roadLayer);

        if (hitRoad)
        {
            if (!isOnRoad)
            {
                isOnRoad = true;
                car.maxEngineTorque = normalTorque;
                car.maxspeed = normalSpeed;
                rb.linearDamping = normalDrag;
            }
        }
        else
        {
            if (isOnRoad)
            {
                isOnRoad = false;
                car.maxEngineTorque = normalTorque * offRoadTorque;
                car.maxspeed = normalSpeed * offRoadSpeed;
                rb.linearDamping = offRoadDrag;
            }
        }

        Debug.DrawRay(transform.position, -Vector3.up * 2f, hitRoad ? Color.green : Color.red);
    }


}
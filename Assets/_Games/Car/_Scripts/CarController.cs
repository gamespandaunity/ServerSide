using UnityEngine;
 
using CarRace;
using Mirror;
using TMPro;

public class CarController :NetworkBehaviour//Photon Removal : MonoBehaviourPun
{
    [Header("Car Settings")]
    public string carName = "Car";

    // Reference to RCC controller if available
    private RCC_CarControllerV3 rccController;
    [SerializeField] TextMeshProUGUI PlayerNameInNumerPlate;
    [SerializeField] private MiniMapController MiniMapController;
    void Start()
    {
        MiniMapController = FindObjectOfType<MiniMapController>();
        MiniMapController.SetTarget(transform);
        PlayerNameInNumerPlate.text = staticVariables.UserProfiledata.user.first_name;
        // Get RCC controller if it exists
        //Photon Removal    rccController = GetComponent<RCC_CarControllerV3>();

        //Photon Removal   if (string.IsNullOrEmpty(carName))
        //Photon Removal    carName = gameObject.name;

        // Auto-register with the progress UI
        if (RaceProgressUI.Instance != null)
        {
          //  RaceProgressUI.Instance.RegisterCar(this);
        }
    }

    // Get car's current speed (useful for additional UI elements)
    //public float GetSpeed()
    //{
    //    if (rccController != null)
    //        return rccController.speed;                                                                                         //Photon Removal

    //    // Fallback: calculate speed from rigidbody
    //    Rigidbody rb = GetComponent<Rigidbody>();
    //    return rb != null ? rb.linearVelocity.magnitude : 0f;
    //}

}
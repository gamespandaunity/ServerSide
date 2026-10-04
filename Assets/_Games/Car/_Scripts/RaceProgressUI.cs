using UnityEngine;
using UnityEngine.UI;
 
using System.Collections.Generic;
using System.Linq;

public class RaceProgressUI  //Photon Removal: MonoBehaviourPun
{
    [Header("UI References")]
    public RectTransform trackLine;
    public GameObject carIndicatorPrefab;
    public Color playerCarColor = Color.blue;
    public Color otherCarColor = Color.red;

    [Header("Track Settings")]
    public Transform startPoint;
    public Transform finishPoint;
    public float trackLength;

    private Dictionary<int, RectTransform> carIndicators = new Dictionary<int, RectTransform>();
    private Dictionary<int, Image> carIndicatorImages = new Dictionary<int, Image>();
    private CarController[] allCars;

    void Start()
    {
        // Calculate track length if not manually set
        if (trackLength <= 0 && startPoint != null && finishPoint != null)
        {
            trackLength = Vector3.Distance(startPoint.position, finishPoint.position);
            Debug.Log($"Calculated track length: {trackLength}");
        }

        // Wait a bit for all networked objects to spawn, then find cars
        //Photon Removal  InvokeRepeating(nameof(FindAndUpdateCars), 0.5f, 1f);
    }

    //void FindAndUpdateCars()                                                               //Photon Removal
    //{
    //    // Find ALL cars including networked ones
    //    CarController[] foundCars = FindObjectsOfType<CarController>();

    //    // Also try to find cars by PhotonView
    //    PhotonView[] allPhotonViews = FindObjectsOfType<PhotonView>();
    //    List<CarController> networkCars = new List<CarController>();

    //    foreach (PhotonView pv in allPhotonViews)
    //    {
    //        CarController car = pv.GetComponent<CarController>();
    //        if (car != null && !networkCars.Contains(car))
    //        {
    //            networkCars.Add(car);
    //        }
    //    }

    //    // Combine both methods
    //    List<CarController> allFoundCars = new List<CarController>(foundCars);
    //    foreach (CarController netCar in networkCars)
    //    {
    //        if (!allFoundCars.Contains(netCar))
    //        {
    //            allFoundCars.Add(netCar);
    //        }
    //    }

    //    // Update our cars array if we found new ones
    //    if (allFoundCars.Count > (allCars?.Length ?? 0))
    //    {
    //        allCars = allFoundCars.ToArray();
    //        Debug.Log($"Updated: Found {allCars.Length} total cars");

    //        // Create indicators for new cars
    //        foreach (CarController car in allCars)
    //        {
    //            if (car != null)
    //            {
    //                int carId = car.photonView != null ? car.photonView.ViewID : car.GetInstanceID();
    //                if (!carIndicators.ContainsKey(carId))
    //                {
    //                    CreateCarIndicator(car);
    //                    Debug.Log($"Created new indicator for car: {car.name} (ID: {carId})");
    //                }
    //            }
    //        }
    //    }

    //    // Stop searching after we have the expected number of cars
    //    if (allCars != null && allCars.Length >= 2) // Assuming 2 players
    //    {
    //        CancelInvoke(nameof(FindAndUpdateCars));
    //        Debug.Log("Found all cars, stopped searching");
    //    }
    //}

    void Update()
    {
        //Photon Removal UpdateCarPositions();
    }

    //void CreateCarIndicator(CarController car)                                                                      //Photon Removal
    //{
    //    if (car == null) return;

    //    // Handle both networked and non-networked cars
    //    int carId = car.photonView != null ? car.photonView.ViewID : car.GetInstanceID();

    //    // Create indicator UI element
    //    GameObject indicator = Instantiate(carIndicatorPrefab, trackLine);
    //    RectTransform indicatorRect = indicator.GetComponent<RectTransform>();
    //    Image indicatorImage = indicator.GetComponent<Image>();

    //    // Set color based on ownership (or default colors for non-networked)
    //    if (car.photonView != null && car.photonView.IsMine)
    //    {
    //        indicatorImage.color = playerCarColor;
    //    }
    //    else
    //    {
    //        indicatorImage.color = otherCarColor;
    //    }

    //    // Store references
    //    carIndicators[carId] = indicatorRect;
    //    carIndicatorImages[carId] = indicatorImage;

    //    // Add car name/number text (optional)
    //    Text carText = indicator.GetComponentInChildren<Text>();
    //    if (carText != null)
    //    {
    //        if (car.photonView != null)
    //        {
    //            carText.text = car.photonView.IsMine ? staticVariables.UserProfiledata.user.first_name + staticVariables.UserProfiledata.user.first_name :
    //                staticVariables.OpponetProfile.userName;
    //        }
    //        else
    //        {
    //            carText.text = car.name; // Use GameObject name for non-networked cars
    //        }
    //    }

    //    Debug.Log($"Created indicator for car {carId} at position {car.transform.position}");
    //}

    //void UpdateCarPositions()                                                                         //Photon Removal
    //{
    //    if (startPoint == null || finishPoint == null)
    //    {
    //        Debug.LogError("Start point or finish point is null!");
    //        return;
    //    }

    //    // Debug: Check if we have cars
    //    if (allCars == null || allCars.Length == 0)
    //    {
    //        Debug.LogWarning("No cars found! Re-finding cars...");
    //        allCars = FindObjectsOfType<CarController>();
    //    }

    //    foreach (CarController car in allCars)
    //    {
    //        if (car == null) continue;

    //        // For non-networked cars, use a different ID system
    //        int carId = car.photonView != null ? car.photonView.ViewID : car.GetInstanceID();

    //        if (carIndicators.ContainsKey(carId))
    //        {
    //            float progress = CalculateCarProgress(car.transform.position);
    //           // Debug.Log($"Car {carId} progress: {progress:F2}");
    //            UpdateIndicatorPosition(carIndicators[carId], progress);
    //        }
    //    }
    //}

    float CalculateCarProgress(Vector3 carPosition)
    {
        // Project car position onto the track line
        Vector3 trackDirection = (finishPoint.position - startPoint.position).normalized;
        Vector3 carFromStart = carPosition - startPoint.position;

        // Calculate how far along the track the car is
        float distanceAlongTrack = Vector3.Dot(carFromStart, trackDirection);

        // Convert to progress percentage (0 to 1)
        float progress = Mathf.Clamp01(distanceAlongTrack / trackLength);

        return progress;
    }

    void UpdateIndicatorPosition(RectTransform indicator, float progress)
    {
        // Get track line width
        float trackWidth = trackLine.rect.width;

        // Calculate new position along the track line
        float xPosition = (progress * trackWidth) - (trackWidth * 0.5f);

        // Update indicator position
        indicator.anchoredPosition = new Vector2(xPosition, indicator.anchoredPosition.y);
    }

    // Public method to manually register a car (call this from your car spawning code)
    //public void RegisterCar(CarController car)                                                                                                             //Photon Removal
    //{
    //    if (car == null) return;

    //    int carId = car.photonView != null ? car.photonView.ViewID : car.GetInstanceID();

    //    if (!carIndicators.ContainsKey(carId))
    //    {
    //        CreateCarIndicator(car);

    //        // Update the cars array
    //        List<CarController> carsList = allCars?.ToList() ?? new List<CarController>();
    //        if (!carsList.Contains(car))
    //        {
    //            carsList.Add(car);
    //            allCars = carsList.ToArray();
    //        }

    //        Debug.Log($"Manually registered car: {car.name} (Total cars: {allCars.Length})");
    //    }
    //}

    // Call this method from your car spawning script
    public static RaceProgressUI Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }
    //public void HighlightLeader()                                                                                                                 //Photon Removal
    //{
    //    float maxProgress = 0f;
    //    int leaderId = -1;

    //    foreach (CarController car in allCars)
    //    {
    //        if (car == null) continue;

    //        float progress = CalculateCarProgress(car.transform.position);
    //        if (progress > maxProgress)
    //        {
    //            maxProgress = progress;
    //            leaderId = car.photonView.ViewID;
    //        }
    //    }

    //    // Reset all indicators to normal size
    //    foreach (var indicator in carIndicators.Values)
    //    {
    //        indicator.localScale = Vector3.one;
    //    }

    //    // Highlight the leader
    //    if (leaderId != -1 && carIndicators.ContainsKey(leaderId))
    //    {
    //        carIndicators[leaderId].localScale = Vector3.one * 1.2f;
    //    }
    //}
}
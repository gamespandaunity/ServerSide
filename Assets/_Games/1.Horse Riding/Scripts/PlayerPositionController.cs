
using Mirror;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.U2D.Physics.PhysicsLayers;

public class PlayerPositionController : NetworkBehaviour
{
    public HorseAiWaypointsContainer waypointsContainer; // Waypoints Container.
    public int currentWaypoint = 0;
    public int lap = 0;
    public int totalWaypointPassed = 0;
    public int nextWaypointPassRadius = 40;
    float lastWavePointDistance = 0;
    public int WrongWayThreshHold = 5;
    public int GrandTalWaypointPassed = 0;
    float percentAhead = 0;
    public float TotalDistanecPoints = 0;

    public string PlayerName;
    public string playerCountry;

    public bool isLocalPlayer;
    //private PhotonView photonView;
    public int playerPoisition = 5;
    public Transform cameraTargetFront;
    public Transform cameraTargetBack;
    public Transform cameraTargetOrbit;
    public bool isAI;
    public int awatarID;
    [SerializeField] private GameObject PlayerNameObject;
    [SerializeField] private GameObject PlayerFlagObject;

    public bool isAtFirst = false;

    // Store the previous value of the number
    private float previousNumber;

    // Store whether the number is currently increasing or decreasing
    private string trend;

    void Start()
    {
        Debug.Log("Start");
        playerPoisition = 5;
        // Next waypoint's position.


        setWavePoint(DemoGameManagers.Instance.aiWaypointsContainer);

        //
        // if (!MultiPlayerGame.isChampion)
        // {
        //     setWavePoint(DemoGameManagers.Instance.waypointsContainer);
        // }
        //

        //  if (photonView)
        {
            //     DemoGameManagers.Instance.addAiPlayer(this);
        }
        if (MultiPlayerGame.isSinglePlayer)
        {
            if (isAI)
            {
                //  DemoGameManagers.Instance.addAiPlayer(this);
                Debug.Log("isAI");
            }
            else
            {
                PlayerNameObject.SetActive(false);
                PlayerFlagObject.SetActive(false);
                DemoGameManagers.Instance.addAiPlayer(this);
                isLocalPlayer = true;
                PlayerName = staticVariables.UserProfiledata.user.first_name + " " + staticVariables.UserProfiledata.user.last_name;
                string[] countries = { "US", "UK", "JP", "IN", "FR" };
                playerCountry = "AU";//countries[Random.Range(0, countries.Length)];
                awatarID = 45;
            }
        }
        if(!isAI)
        {
          //  DemoGameManagers.Instance.addAiPlayer(this);
            
        }   // List<PlayerPositionController> allPlayers = new List<PlayerPositionController>(); // add your player objects here
            // Debug.Log("AllPlayers" + allPlayers.Count);
            // foreach (var player in allPlayers)
            // {
            //     Debug.Log("player");
            //     if (player.isAI)
            //     {
            //         // Add AI to game manager
            //         DemoGameManagers.Instance.addAiPlayer(player);

            //         // Assign AI default or random country/avatar
            //         string[] countries = { "US", "UK", "JP", "IN", "FR" }; // add more if you want
            //         int[] avatars = { 0, 1, 2, 3, 4 }; // avatar IDs

            //         player.playerCountry = countries[Random.Range(0, countries.Length)];
            //         player.awatarID = avatars[Random.Range(0, avatars.Length)];
            //           Debug.Log($"AI Player assigned: Country = {player.playerCountry}, AvatarID = {player.awatarID}");
            //     }
            //     else
            //     {
            //         // For human/offline player
            //         player.playerCountry = "US";
            //         player.awatarID = 5;
            //         Debug.Log($"Human Player assigned: Country = {player.playerCountry}, AvatarID = {player.awatarID}");
            //     }
            // }

            trend = "None"; // Initial trend
    }

    private void OnDestroy()
    {
        //        DemoGameManagers.Instance.aiPlayersLis.Remove(this);
    }

    public void setWavePoint(HorseAiWaypointsContainer waypointsContainer)
    {
        this.waypointsContainer = waypointsContainer;
        Vector3 nextWaypointPosition = transform.InverseTransformPoint(new Vector3(waypointsContainer.waypoints[currentWaypoint].position.x, transform.position.y, waypointsContainer.waypoints[currentWaypoint].position.z));
        GrandTalWaypointPassed = 0;
        lastWavePointDistance = nextWaypointPosition.magnitude;
        previousNumber = nextWaypointPosition.magnitude;
        TotalDistanecPoints = GrandTalWaypointPassed;
    }
    bool isDecremented = false;
    void FixedUpdate()
    {

        if (isOwned)
        {

            Navigation();

        }
        else if (!isAI)
        {
            Navigation();
        }
        if (MultiPlayerGame.isSinglePlayer)
        {
             Navigation();
        }

    }
    void Navigation()
    {
        if (MConstants.isRaceOver)
        {
            return;
        }

        // If our scene doesn't have a Waypoint Container, return with error.
        if (!waypointsContainer)
        {

            Debug.LogError("Waypoints Container Couldn't Found!");
            return;

        }

        // If our scene has Waypoints Container and it doesn't have any waypoints, return with error.
        if (waypointsContainer && waypointsContainer.waypoints.Count < 1)
        {

            Debug.LogError("Waypoints Container Doesn't Have Any Waypoints!");
            return;

        }

        // Next waypoint's position.
        Vector3 currentWaypointPosition = transform.InverseTransformPoint(new Vector3(waypointsContainer.waypoints[currentWaypoint].position.x, transform.position.y, waypointsContainer.waypoints[currentWaypoint].position.z));


        // Checks for the distance to next waypoint. If it is less than written value, then pass to next waypoint.
        if (currentWaypointPosition.magnitude < nextWaypointPassRadius)
        {
            currentWaypoint++;
            totalWaypointPassed++;
            //Vector3 nextWaypointPosition1 = transform.InverseTransformPoint(new Vector3(waypointsContainer.waypoints[currentWaypoint].position.x, transform.position.y, waypointsContainer.waypoints[currentWaypoint].position.z));

            GrandTalWaypointPassed++;
            //if (photonView && photonView.IsMine && !MConstants.isRaceOver)
            //{
            //    photonView.Owner.AddScore(1);
            //}
            DemoGameManagers.Instance.updatePosition();

            // If all waypoints were passed, sets the current waypoint to first waypoint and increase lap.
            if (currentWaypoint >= waypointsContainer.waypoints.Count)
            {

                currentWaypoint = 0;

                lap++;
                if (isLocalPlayer && !MultiPlayerGame.isLastManStandingMode && lap < LevelsManager.instance.playerCompletedLaps)
                {
                    HudMenuManager.instance.notifyLapCompletion();

                }
            }
            currentWaypointPosition = transform.InverseTransformPoint(new Vector3(waypointsContainer.waypoints[currentWaypoint].position.x, transform.position.y, waypointsContainer.waypoints[currentWaypoint].position.z));
            lastWavePointDistance = currentWaypointPosition.magnitude;
            previousNumber = currentWaypointPosition.magnitude;
        }

        percentAhead = (1 - (currentWaypointPosition.magnitude / lastWavePointDistance));
       // TotalDistanecPoints = GrandTalWaypointPassed + percentAhead;
        if (MultiPlayerGame.isSinglePlayer)
        {
            TotalDistanecPoints = GrandTalWaypointPassed + percentAhead;
          //  Debug.Log("TotalDistanecPoints"+TotalDistanecPoints);

        }
        if (!isAI)
        {

            TotalDistanecPoints = GrandTalWaypointPassed + percentAhead;
          //  Debug.Log("isLocalPlayerTotalDistanecPoints"+TotalDistanecPoints);

        }
        if (MultiPlayerGame.isSinglePlayer)
        {
            // Debug.Log("isSinglePlayerCheckWrongWay");
            CheckWrongWay(currentWaypointPosition.magnitude);
        }
        if (isOwned)
        {
            //Debug.Log("isOwnedCheckWrongWay");
            //Debug.LogError("current waypoint: " + currentWaypoint);
            //Debug.LogError("lastWavePointDistance + WrongWayThreshHold: " + (lastWavePointDistance + WrongWayThreshHold));
            //Debug.LogError("lastWavePointDistance: " + lastWavePointDistance);
            //Debug.LogError("currentWaypointPosition: " + currentWaypointPosition.magnitude);
            CheckWrongWay(currentWaypointPosition.magnitude);
        }
       


        //if (currentWaypointPosition.magnitude > (lastWavePointDistance + WrongWayThreshHold) && isLocalPlayer)
        //{
        //    // Debug.Log("Wrong Way");
        //    HudMenuManager.instance.WrongWay();
        //}


        //else if (isLocalPlayer)
        //{
        //    HudMenuManager.instance.RightWay();
        //}
    }

    private void CheckWrongWay(float trackedNumber)
    {
        if (Mathf.Abs(trackedNumber - previousNumber) < 7f)
        {
           // Debug.LogError($"*** LOW threshold");
            return;
        }

        if (trackedNumber > previousNumber)
        {
            HudMenuManager.instance.showWrongWayWarning();
           // Debug.LogError($"*** Wrong Way detected");
        }
        else if (trackedNumber < previousNumber)
        {
            HudMenuManager.instance.clearWrongWayWarning();
            //Debug.LogError($"*** Right Way detected");
        }
        previousNumber = trackedNumber;
            //Debug.LogError($"*** tracked number ::: {trackedNumber} + previous number {previousNumber}");
            //if (previousNumber > 0 && Mathf.Abs(trackedNumber - previousNumber) > 50f)
            //{
            //    previousNumber = trackedNumber;
            //    HudMenuManager.instance.clearWrongWayWarning();
            //    return;
            //}
            //if (Mathf.Abs(trackedNumber - previousNumber) < 3f)
            //{
            //    //Debug.LogError($"*** LOW threshold");
            //    return;
            //}

            //if (trackedNumber > previousNumber)
            //{
            //    Debug.LogError($"*** Wrong Way detected");
            //    HudMenuManager.instance.showWrongWayWarning();
            //}
            //else if (trackedNumber < previousNumber)
            //{
            //    Debug.LogError($"*** Right Way detected");
            //    HudMenuManager.instance.clearWrongWayWarning();
            //}

            ////Debug.LogError($"*** trend :::{trend}");

            //previousNumber = trackedNumber;
    }
    public void ResetWaypointTracking()
    {
        Debug.Log("[WaypointDebug] ResetWaypointTracking called");

        if (waypointsContainer == null)
        {
            Debug.LogError("[WaypointDebug] waypointsContainer is NULL!");
            return;
        }

        if (waypointsContainer.waypoints == null)
        {
            Debug.LogError("[WaypointDebug] waypointsContainer.waypoints is NULL!");
            return;
        }

        if (waypointsContainer.waypoints.Count == 0)
        {
            Debug.LogError("[WaypointDebug] waypointsContainer.waypoints.Count is 0!");
            return;
        }

        Debug.Log("[WaypointDebug] currentWaypoint: " + currentWaypoint);
        Vector3 wpPos = transform.InverseTransformPoint(
            new Vector3(
                waypointsContainer.waypoints[currentWaypoint].position.x,
                transform.position.y,
                waypointsContainer.waypoints[currentWaypoint].position.z
            )
        );

        lastWavePointDistance = wpPos.magnitude;
        previousNumber = wpPos.magnitude;

        Debug.Log("[WaypointDebug] ResetWaypointTracking completed. lastWavePointDistance=" + lastWavePointDistance);
    }
}

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class PlayerPositionController : MonoBehaviour
// {
//     public HorseAiWaypointsContainer waypointsContainer; // Waypoints Container.
//     public int currentWaypoint = 0;
//     public int lap = 0;
//     public int totalWaypointPassed = 0;
//     public int nextWaypointPassRadius = 40;
//     float lastWavePointDistance = 0;
//     public int WrongWayThreshHold = 5;
//     public int GrandTalWaypointPassed = 0;
//     float percentAhead = 0;
//     public float TotalDistanecPoints = 0;

//     public string PlayerName;
//     public string playerCountry;

//     public bool isLocalPlayer;
//     //Photon Removal private PhotonView photonView;
//     public int playerPoisition = 5;
//     public Transform cameraTargetFront;
//     public Transform cameraTargetBack;
//     public Transform cameraTargetOrbit;
//     public bool isAI;
//     public int awatarID;


//     public bool isAtFirst = false;

//     // Store the previous value of the number
//     private float previousNumber;

//     // Store whether the number is currently increasing or decreasing
//     private string trend;

//     void Start()
//     {
//         playerPoisition = 5;
//         photonView = GetComponent<PhotonView>();

//         setWavePoint(DemoGameManagers.Instance.aiWaypointsContainer);

      

//         if (photonView)
//         {
//             DemoGameManagers.Instance.addAiPlayer(this);
//         }

//                                                                                                                                                                                            //Photon Removal
//         object countryflag;
//         if (!isAI && photonView.Owner.CustomProperties.TryGetValue(MultiPlayerGame.PLAYER_COUNTRY, out countryflag))
//         {
//             playerCountry = (string)countryflag;
//         }
//         if (!isAI && photonView.Owner.CustomProperties.TryGetValue(MultiPlayerGame.PLAYER_AWATAR, out countryflag))
//         {
//             awatarID = (int)countryflag;
//         }
//         if (MultiPlayerGame.isSinglePlayer && !isAI)
//         {
//             playerCountry = PlayerDataController.instance.playerStats.countryCode;
//            awatarID = PlayerDataController.instance.playerStats.playerAwatarId;

//         }
//         trend = "None"; // Initial trend
//     }

//     private void OnDestroy()
//     {
//                 DemoGameManagers.Instance.aiPlayersLis.Remove(this);
//     }

//     public void setWavePoint(HorseAiWaypointsContainer waypointsContainer)
//     {
//         this.waypointsContainer = waypointsContainer;
//         Vector3 nextWaypointPosition = transform.InverseTransformPoint(new Vector3(waypointsContainer.waypoints[currentWaypoint].position.x, transform.position.y, waypointsContainer.waypoints[currentWaypoint].position.z));
//         GrandTalWaypointPassed = 0;
//         lastWavePointDistance = nextWaypointPosition.magnitude;
//         previousNumber = nextWaypointPosition.magnitude;
//         TotalDistanecPoints = GrandTalWaypointPassed;
//     }
//     bool isDecremented = false;
//     void FixedUpdate()
//     {
//         Navigation();           // Feeds steerInput based on navigator.     

//     }
//     void Navigation()
//     {
//         if (MConstants.isRaceOver)
//         {
//             return;
//         }

//         // If our scene doesn't have a Waypoint Container, return with error.
//         if (!waypointsContainer)
//         {

//             ConstantsData_M.Log("Waypoints Container Couldn't Found!");
//             return;

//         }

//         // If our scene has Waypoints Container and it doesn't have any waypoints, return with error.
//         if (waypointsContainer && waypointsContainer.waypoints.Count < 1)
//         {

//             ConstantsData_M.Log("Waypoints Container Doesn't Have Any Waypoints!");
//             return;

//         }

//         // Next waypoint's position.
//         Vector3 currentWaypointPosition = transform.InverseTransformPoint(new Vector3(waypointsContainer.waypoints[currentWaypoint].position.x, transform.position.y, waypointsContainer.waypoints[currentWaypoint].position.z));


//         // Checks for the distance to next waypoint. If it is less than written value, then pass to next waypoint.
//         if (currentWaypointPosition.magnitude < nextWaypointPassRadius)
//         {
//             currentWaypoint++;
//             totalWaypointPassed++;
//             Vector3 nextWaypointPosition1 = transform.InverseTransformPoint(new Vector3(waypointsContainer.waypoints[currentWaypoint].position.x, transform.position.y, waypointsContainer.waypoints[currentWaypoint].position.z));

//             GrandTalWaypointPassed++;
//             if (photonView && photonView.IsMine && !MConstants.isRaceOver)
//             {
//                 photonView.Owner.AddScore(1);
//             }
//             DemoGameManagers.Instance.updatePosition();

//             // If all waypoints were passed, sets the current waypoint to first waypoint and increase lap.
//             if (currentWaypoint >= waypointsContainer.waypoints.Count)
//             {

//                 currentWaypoint = 0;

//                 lap++;
//                 if (isLocalPlayer && !MultiPlayerGame.isLastManStandingMode && lap < LevelsManager.instance.playerCompletedLaps)
//                 {
//                     HudMenuManager.instance.notifyLapCompletion();

//                 }
//             }
//             currentWaypointPosition = transform.InverseTransformPoint(new Vector3(waypointsContainer.waypoints[currentWaypoint].position.x, transform.position.y, waypointsContainer.waypoints[currentWaypoint].position.z));
//             lastWavePointDistance = currentWaypointPosition.magnitude;
//             previousNumber = currentWaypointPosition.magnitude;
//         }

//         percentAhead = (1 - (currentWaypointPosition.magnitude / lastWavePointDistance));
//         if (MultiPlayerGame.isSinglePlayer)
//         {
//             TotalDistanecPoints = GrandTalWaypointPassed + percentAhead;

//         }
//          else if (photonView && photonView.IsMine)
//         {
//             TotalDistanecPoints = GrandTalWaypointPassed + percentAhead;

//         }

//         if (isLocalPlayer)
//         {
           
//             CheckWrongWay(currentWaypointPosition.magnitude);
//         }


       
//     }

//     private void CheckWrongWay(float trackedNumber)
//     {


//         if (Mathf.Abs(trackedNumber - previousNumber) < 3f)
//         {
//             return;
//         }

//         if (trackedNumber > previousNumber)
//         {
//             HudMenuManager.instance.showWrongWayWarning();
//         }
//         else if (trackedNumber < previousNumber)
//         {
//             HudMenuManager.instance.clearWrongWayWarning();
//         }


//         previousNumber = trackedNumber;
//     }
// }

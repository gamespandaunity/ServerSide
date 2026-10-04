namespace CarRace 
{
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class PlayerPositionSystem : MonoBehaviour
{
    public static PlayerPositionSystem _Instance;

    [HideInInspector] public List<Transform> waypoints = new List<Transform>(); // List to store the waypoint objects
    List<PlayerDataForPositionSystem> playersDataForPositions = new List<PlayerDataForPositionSystem>();
    List<CarController> carControllers = new List<CarController>();
    //public TextMeshProUGUI posTest;
    private void OnEnable()
    {
        EventManager.OnPositionUpdateEvent += UpdatePositions;
        EventManager.OnGameFinishEvent += SendResult;
    }


    private void OnDisable()
    {
        EventManager.OnPositionUpdateEvent -= UpdatePositions;
        EventManager.OnGameFinishEvent -= SendResult;
    }

    private void Start()
    {
        _Instance = this;
    }

    public void RegisterPlayer(PlayerDataForPositionSystem playerData, CarController cc)
    {
        if (!playersDataForPositions.Contains(playerData))
        {
            playersDataForPositions.Add(playerData);
            carControllers.Add(cc);
            // //Debug.Log(playerData.playerTrasnform.name + " Registered with ID : " + playerData.playerID);
        }
    }

    public void DeregisterPlayer(byte playerID)
    {
        PlayerDataForPositionSystem playerData = playersDataForPositions.Find(data => data.playerID == playerID);
        if (playerData != null)
        {
            playersDataForPositions.Remove(playerData);
        }
    }

    public void PlayerCrossedWaypoint(byte playerID)
    {
        PlayerDataForPositionSystem playerData = playersDataForPositions.Find(data => data.playerID == playerID);
        if (playerData != null)
        {
            playerData.wayPointCount = (byte)((playerData.wayPointCount + 1) % waypoints.Count);

            if (playerData.wayPointCount == 0)// this means this is the last checkPoint ans it has been reset, so Increment lapcount
            {
                playerData.lapCount += 1;
            }
        }
    }
    public void PlayerLapComplete(byte playerID, int lapCunt)
    {
        PlayerDataForPositionSystem playerData = playersDataForPositions.Find(data => data.playerID == playerID);
        if (playerData != null)
        {
            // Reset WayPoint Count to 0 and Increment LapCount 
            playerData.lapCount = (byte)lapCunt;
            playerData.wayPointCount = 0;
        }
    }

    private void UpdatePositions()
    {
        //Update Data fro each player at once
        foreach (var playerData in playersDataForPositions)
        {
            UpDatePlayerPosition(playerData);
        }
        //Sort All values only once at a time, 
        UpdatePlayersPositions();
        //posTest.text = "";
        //get player Position and send to the player
        foreach (var CC in carControllers)
        {
            CC.UpdatePositions(GetPlayerPosition(CC.playerID));
        }
    }

    public void UpDatePlayerPosition(PlayerDataForPositionSystem playerData)
    {
        int currentWaypointIndex = playerData.wayPointCount;

        // Calculate the distance from the player to the next waypoint
        float distanceToNextWaypoint = Vector3.Distance(playerData.playerTrasnform.position, waypoints[currentWaypointIndex].position);

        //Update  distance covered by the player
        playerData.distanceCovered = distanceToNextWaypoint;

    }

    private void UpdatePlayersPositions()
    {

        playersDataForPositions.Sort((a, b) =>
        {
            // Sort by LapCount in descending order
            int lapComparison = b.lapCount.CompareTo(a.lapCount);
            if (lapComparison == 0)
            {
                // If LapCount is the same, sort by wayPointCount in descending order
                int waypointComparison = b.wayPointCount.CompareTo(a.wayPointCount);
                if (waypointComparison == 0)
                {
                    // If wayPointCount is the same, sort by distanceToNextWaypoint in ascending order
                    return a.distanceCovered.CompareTo(b.distanceCovered);
                }
                return waypointComparison;
            }
            return lapComparison;
        });

        // Assign playerPosition based on the sorted order (1 for the largest distanceCovered, 2 for the next, and so on)
        for (int i = 0; i < playersDataForPositions.Count; i++)
        {
            playersDataForPositions[i].playerPosition = (byte)(i + 1);
        }
    }

    public byte GetPlayerPosition(byte playerID)
    {
        PlayerDataForPositionSystem playerData = playersDataForPositions.Find(data => data.playerID == playerID);

        if (playerData != null)
        {
            //posTest.text += "\nLC :" + playerData.lapCount + " WP : " + playerData.wayPointCount + " Score :" + playerData.distanceCovered +
            //    " Pos: " + playerData.playerPosition;
            return playerData.playerPosition;
        }
        return 0;
    }

    void SendResult()
    {
        UpdatePlayersPositions();
        EventManager.PrepareResults.Invoke(playersDataForPositions);
        playersDataForPositions.Clear();
    }

    public Transform GetPlayersLastCheckPoint(int playerID)
    {
        PlayerDataForPositionSystem player = playersDataForPositions.FirstOrDefault(p => p.playerID == playerID);
        if (player != null)
        {
            return player.wayPointCount > 0 ? waypoints[player.wayPointCount - 1] : waypoints[0];
        }
        return null;
    }
}

public class PlayerDataForPositionSystem
{
    public string playerName;
    public byte playerID;
    public Transform playerTrasnform;
    public byte lapCount;
    public byte wayPointCount;
    public byte playerPosition;
    public float distanceCovered;

    public PlayerDataForPositionSystem(byte pID, Transform playerTrasnform, string playerName)
    {
        this.playerName = playerName;
        this.playerID = pID;
        this.playerTrasnform = playerTrasnform;
        this.lapCount = 0;
        this.wayPointCount = 0;
        this.playerPosition = 0;
        this.distanceCovered = 0f;
    }

}
}
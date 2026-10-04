using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class LevelTrack : MonoBehaviour
{
    [FormerlySerializedAs("Track")] public GameObject raceTrack;
    [FormerlySerializedAs("AiWaypointsContainer")] public HorseAiWaypointsContainer aiWaypointContainer;
    [FormerlySerializedAs("EnvPoint")] public Transform environmentPoint;

}

//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using System.Collections;

/// <summary>
/// General Highway Racer settings.
/// </summary>
[System.Serializable]
public class HR_HighwayRacerProperties : ScriptableObject {

    public static HR_HighwayRacerProperties instance;
    public static HR_HighwayRacerProperties Instance {

        get {

            if (instance == null)
                instance = Resources.Load("HR_HighwayRacerProperties") as HR_HighwayRacerProperties;

            return instance;

        }

    }

    public int _minimumSpeedForGainScore = 80;       //  Minimum speed limit for score.
    public int _minimumSpeedForHighSpeed = 100;       //  Minimum speed limit for high speed score.
    public int _minimumCollisionForGameOver = 4;        //  Minimum collision impulse for crashes.

    public Color _defaultBodyColor = Color.gray;     //  Default body color will be applied at first gameplay.
    public bool _tiltCamera = true;        //  Tilts camera on FPS mode.
    public int _totalDistanceMoneyMP = 360;       //  Multiplier for distance score.
    public int _totalNearMissMoneyMP = 30;       //  Multiplier for near miss score.
    public int _totalOverspeedMoneyMP = 20;      //  Multiplier for high speed score.
    public int _totalOppositeDirectionMP = 30;       //  Multiplier for opposite direction score.

    public bool _1MMoneyForTesting = false;     //  Gives 1000000$ when enabled for testing purposes only.

    public GameObject[] selectablePlayerCars;       //  All selectable player vehicles.
    public GameObject[] upgradableWheels;       //  All selectable upgradable wheels.
    public GameObject explosionEffect;      //  Explosion prefab used on bomb mode.

    public AudioClip[] mainMenuClips;       //  Main menu soundtracks.
    public AudioClip[] gameplayClips;       //  Gameplay soundtracks.
    public AudioClip buttonClickAudioClip;      //  Button click sound.
    public AudioClip nearMissAudioClip;     //  Near miss sound.
    public AudioClip labelSlideAudioClip;       //  Label slide sound.
    public AudioClip countingPointsAudioClip;       //  Counting score sound.
    public AudioClip bombTimerAudioClip;        //  Bomb timer sound.
    public AudioClip hornClip;      //  Normal horn sound.
    public AudioClip sirenAudioClip;        //  Siren horn sound.

    public LayerMask trafficCarsLayer;      //  Traffic cars will use this layer.

}

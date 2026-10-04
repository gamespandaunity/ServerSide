using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
namespace Snake_Ladder
{
    public class SnakeReferenceManager : MonoBehaviour
    {
        public static SnakeReferenceManager Instance;

        private void Awake()
        {
           // if (NetworkManager.Instance == null)
            {
                networkManager.SetActive(true);
            }

            Instance = this;
        }
        [Header("Network")]
        public GameObject networkManager;
        public Transform avatar1Transform, avatar2Transform;
        public Transform Player1AvatarGP, Player2AvatarGP;
        //[Header("Network")]
        //public Transform RoomPlayerUIParent;
        //public Transform GameplayPlayerUiParent;

        //[Header("SpinWheel")]
        //public Transform SpinWheelParent;

        //[Space]
        //[Header("Sprites")]
        public List<Sprite> Avatars;


        //[Space]
        //[Header("Categories")]
        //public List<String> Categories;

        //[Header("SPINResult")]
        //public TextMeshProUGUI ResultText;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
namespace Snake_Ladder
{
    public class ReferenceManager : MonoBehaviour
    {
        public static ReferenceManager Instance;

        private void Awake()
        {
            //Photon Removal   if (NetworkManager.Instance == null)
            {
                networkManagerGO.SetActive(true);
            }

            Instance = this;
        }
        [Header("Network")]
        [FormerlySerializedAs("networkManager")] public GameObject networkManagerGO;
        [FormerlySerializedAs("avatar1Transform")] public Transform playerAvatarTransform;
        [FormerlySerializedAs("avatar2Transform")] public Transform opponentAvatarTransform;
        [FormerlySerializedAs("Player1AvatarGP")] public Transform gameplayPlayerAvatar;
        [FormerlySerializedAs("Player2AvatarGP")] public Transform gameplayOpponentAvatar;

        [FormerlySerializedAs("Avatars")] public List<Sprite> avatarSprites;




    }
}
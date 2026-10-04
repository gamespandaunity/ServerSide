using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POKER
{

    public class AllScriptsManager : MonoBehaviour
    {
        public static AllScriptsManager Instance;

        public POKER.GameManager GameManager;
        public POKER.GameStartManager GameStartManager;
        public POKER.GameResetManager GameResetManager;
        public POKER.Game_Play Game_Play;
        public POKER.RoomStateManager RoomStateManager;
        public POKER.PositionsManager PositionsManager;
        public POKER.PlayerStateManager PlayerStateManager;
        //public POKER.

        public void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            if(Instance == null)
            {
                Instance = this;
            }
        }
    }
}
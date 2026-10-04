
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace POKER
{
    public class PlayerProperties : MonoBehaviour
    {

        public PlayerCustomProperties player; //photon removal



        public void SettingProperty(string key, int value)
        {
            player.SetCustomData(key, value);
        }


        public int GettingProperty(string key)
        {
            return player.GetCustomData(key);
        }


    }
}
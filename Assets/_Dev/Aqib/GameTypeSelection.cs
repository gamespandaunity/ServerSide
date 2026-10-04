using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameTypeSelection : MonoBehaviour
{
    public enum _GameTypeEnum
    {
        OpenChallenge, DirectInvite,SocialInvite, WithAI
    }
    public static _GameTypeEnum gameTypeEnum;

   
}

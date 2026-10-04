using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Snake_Ladder;

namespace Twelve
{
    public class PlayerAvatarControllerTwelve : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI PlayerName;
    [SerializeField] Image Avatar;
    // Start is called before the first frame update
    void Start()
    {
            //Photon Removal if (!PhotonNetwork.IsMasterClient)
            {
                transform.localPosition = ReferenceManager.Instance.opponentAvatarTransform.localPosition;
        }
        

    }
}
}

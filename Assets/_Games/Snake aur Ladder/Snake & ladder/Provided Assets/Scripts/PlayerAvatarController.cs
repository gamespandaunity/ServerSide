using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
//using Photon.Pun;
using UnityEngine.UI;
namespace Snake_Ladder
{

    public class PlayerAvatarController : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI PlayerName;
        [SerializeField] Image Avatar;
        // Start is called before the first frame update
        void Start()
        {
          //  if (PhotonNetwork.IsMasterClient)
            {
                //transform.localPosition = ReferenceManager.Instance.avatar1Transform.localPosition;
            }
          //  else
            {
              //Wasi    transform.localPosition = FusionGameReferenceManager.Instance.avatar2Transform.localPosition;
            }
        }
    }
}
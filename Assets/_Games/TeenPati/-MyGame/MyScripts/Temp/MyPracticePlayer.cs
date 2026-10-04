using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyPracticePlayer : MonoBehaviour
{
    //photon removal   private PhotonView photonView;
    private bool controllable;
    public static MyPracticePlayer MyLocalPlayerInstance;
    private void Awake()
    {
        //photon removal  photonView = GetComponent<PhotonView>();
    }
    private void Update()
    {
        //photon removal  if (!photonView.AmOwner || !controllable)
        return;
    }
    private void FixedUpdate()
    {
        //photon removal if (!photonView.IsMine)
        return;
        if (!controllable) return;

    }


}

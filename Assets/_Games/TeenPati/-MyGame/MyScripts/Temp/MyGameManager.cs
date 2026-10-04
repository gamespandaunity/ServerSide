using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyGameManager : MonoBehaviour
{
    public static MyGameManager instance;


    private void Awake()
    {
        instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        //float angularStart = PhotonNetwork.CurrentRoom.PlayerCount * PhotonNetwork.LocalPlayer.GetPlayerNumber();
        Vector3 position = Vector3.zero;
        if (MirrorNetwork.Instance.isMasterClient)

            position = new Vector3(3, 3f, 0);                                                                                //photon removal

        else
            position = new Vector3(3, 0, 0);



        print("finding player");
        if (MyPracticePlayer.MyLocalPlayerInstance == null)
        {
            Debug.Log("Instantiating player");
            //PhotonNetwork.Instantiate("PlayerTemp", position, Quaternion.identity, 0);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}

 
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DestroyPlayer : MonoBehaviour
{
    //Photon Removal  public PhotonView photonView;
    public TextMeshProUGUI textMeshProUGUI;
    public GameObject player;
    // Start is called before the first frame update
    void Start()
    {
        //Photon Removal    photonView = GetComponent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        
    }

    public void changePOS()
    {
        //Photon Removal    if (PhotonNetwork.IsConnected)
        {
            //Photon Removal        photonView.RPC("RPC_ChangePos", RpcTarget.AllBuffered);
        }


    }

    //Photon Removal  [PunRPC]
    public void RPC_ChangePos()
    {
        gameObject.transform.localPosition = new Vector3(gameObject.transform.localPosition.x, gameObject.transform.localPosition.y+1, gameObject.transform.localPosition.z);
    }

}

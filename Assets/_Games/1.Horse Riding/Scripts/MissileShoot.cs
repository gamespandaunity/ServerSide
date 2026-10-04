using UnityEngine;
using System.Collections;
 

public class MissileShoot : MonoBehaviour {
	public Transform gun;
	public float bulletVelocity =50;
   Collider collider;
    float missileCheckTime;
    Transform target;
    public static MissileShoot instance;
    //Photon Removal  private PhotonView photonView;

    public void Awake()
    {
        //Photon Removal  photonView = GetComponent<PhotonView>();

        instance = this;
        missileCheckTime = Time.time;

    }

  
   
}

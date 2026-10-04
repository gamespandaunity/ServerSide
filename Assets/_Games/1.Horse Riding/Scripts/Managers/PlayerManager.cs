using System;
using System.Collections;
using System.Collections.Generic;
using Tanks.UI;
using UnityEngine;
using UnityEngine.Networking;
using CarRace;
public class PlayerManager : MonoBehaviour
{
    public RCC_CarControllerV3 controllerV3;

    public PlayerDamage playerDamage;
    private float minimumCollisionForce = 5f;
    bool isDamageEnabled = true;
    //Photon Removal private PhotonView photonView;
    private MissileShoot missileShoot;
    public Transform gun;
    Collider collider;
    float missileCheckTime;
    Transform target;
    protected int m_RoundCurrencyCollected = 0;
    public event System.Action<string> onPickupCollected;
    //Fired when the round currency changes
    public event Action<int> onCurrencyChanged;
    public event Action<int> onMissileChanged;
    public HealthAndDamage health;
    protected int m_MissileCollected = 0;
    public bool isDead = false;
    float lag;
    public bool isAI;
    public int ActorNumber;
    public string PlayerName;

    //Photon Removal  [HideInInspector]
    //Photon Removal   public Player player;

    public void Awake()
    {
        //Photon Removal   photonView = GetComponent<PhotonView>();
        missileShoot = GetComponent<MissileShoot>();


    }
    private void Start()
    {
        if (isAI)
        {
            ////Debug.Log("Player "+PlayerName);
            //Photon Removal   player = new Player(PlayerName, ActorNumber, false, null);
        }
    }
    public int missileCollected
    {
        get
        {
            return m_MissileCollected;
        }
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Fire(transform.position, transform.rotation);
        }

        if (!isDead)
        {
            if (Time.time >= missileCheckTime + 5 && isAI && missileCollected > 0)
            {
                missileCheckTime = Time.time;

                target = null;
                RayCast();
                if (target != null)
                {
                    //Constants_M.Log("AI Fire Missile=====================================");
                    Fire(transform.position, transform.rotation);
                    m_MissileCollected--;

                }

            }
        }
        //RayCast ();
    }

    public void FireMissile()
    {
        //if (m_MissileCollected > 0)
        //{
        //    lag = 1;
        //    if (photonView.IsMine)
        //    {
        //        Fire(transform.position, transform.rotation);

        //    }
        //    else                                                                                                               //Photon Removal
        //    {
        //        photonView.RPC("RPCFire", RpcTarget.Others, transform.position, transform.rotation);

        //    }
        //    m_MissileCollected--;
        //    if (onMissileChanged != null)
        //    {
        //        onMissileChanged(m_MissileCollected);
        //    }
        //}


    }
    //Photon Removal  [PunRPC]
    //public void RPCFire(Vector3 position, Quaternion rotation, PhotonMessageInfo info)
    //{                                                                                                                                 //Photon Removal
    //    float lag = (float)(PhotonNetwork.Time - info.timestamp);
    //    Fire(position, rotation);
    //}

    void Fire(Vector3 position, Quaternion rotation)
    {

    }

    void RayCast()
    {
        Transform enemy = null;
        float targetRadius = 5;

        if (MConstants.CurrentGameMode == MConstants.GAME_MODES.MULTI_PLAYER)
        {
            targetRadius = 12;
        }
        RaycastHit[] hits = Physics.SphereCastAll(gun.transform.position, targetRadius, gun.transform.forward, Mathf.Infinity);
        //Debug.DrawRay(transform.position, transform.forward*100, Color.green);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("AICarCollider") && hit.distance > 5)
            {
                //Debug.Log ("Hit Name "+ hit.collider.name);
                target = hit.collider.transform;
                break;
            }
            else if (MConstants.CurrentGameMode == MConstants.GAME_MODES.MULTI_PLAYER && hit.collider.GetComponentInParent<RCC_CarControllerV3>() && !hit.collider.GetComponentInParent<RCC_CarControllerV3>().Equals(gameObject.GetComponent<RCC_CarControllerV3>()) && hit.distance > 4)
            {
                //Debug.Log ("Hit Name "+ hit.collider.name);
                target = hit.collider.transform;
                break;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        
        RCC_CarControllerV3 temp = collision.gameObject.GetComponent<RCC_CarControllerV3>();

        if (temp && isDamageEnabled)
        {
            if (collision.contacts.Length < 1 || collision.relativeVelocity.magnitude < minimumCollisionForce)
                return;
            if (controllerV3.speed > temp.speed)
            {
                if (isAI)
                {
                    //Photon Removal   CmdHitPlayer(collision.gameObject, playerDamage.hitDamage, player);

                }
                else
                {
                    //Photon Removal  CmdHitPlayer(collision.gameObject, playerDamage.hitDamage, photonView.Owner);

                }
            }

        }

       
    }


  

    void EnableDamage()
    {
        isDamageEnabled = true;

    }

    void OnTriggerEnter(Collider other)
    {
        //Photon Removal if (photonView && !photonView.IsMine)
        {
            return;
        }

       

    }

    public void AddPickupCurrency(int addCurrency)
    {
        m_RoundCurrencyCollected += addCurrency;
        if (onCurrencyChanged != null)
        {
            onCurrencyChanged(m_RoundCurrencyCollected);
        }
    }

    public void AddPickupName(string pickupName)
    {
        //Photon Removal  if (photonView)
        {
            //Photon Removal     photonView.RPC("PickupCollected", RpcTarget.AllViaServer, pickupName);

        }

    }

    //Photon Removal [PunRPC]
    //public void PickupCollected(string pickupName, PhotonMessageInfo info)
    //{                                                                                                                       //Photon Removal
    //    if (onPickupCollected != null)
    //    {
    //        onPickupCollected(pickupName);
    //    }
    //}

    //[PunRPC]
    //public void RPCAddMissilePickup(int missileCount, PhotonMessageInfo info)
    //{

    //    m_MissileCollected += missileCount;
    //    //Debug.Log("AddMissilePickup");                                                                                                //Photon Removal

    //    if (onMissileChanged != null)
    //    {
    //        onMissileChanged(m_MissileCollected);
    //    }
    //}

    public void AddMissilePickup(int missileCount)
    {
        if (isAI)
        {
            m_MissileCollected += missileCount;
            ////Debug.Log("AddMissilePickup");

            if (onMissileChanged != null)
            {
                onMissileChanged(m_MissileCollected);
            }
        }
        else
        {
            //Photon Removal  photonView.RPC("RPCAddMissilePickup", RpcTarget.AllViaServer, missileCount);

        }

    }
    public void playerDead()
    {
        isDead = true;
        if (!isAI)
        {
            controllerV3.SetCanControl(false);
            //controllerV3.gameObject.GetComponent<RCC_PhotonNetwork>().isDead = true;

        }
        Invoke("resetPos", 0.01f);
        controllerV3.gameObject.GetComponent<Rigidbody>().isKinematic = true;
        //GameManagers.s_Instance.PlayerDead(this);
    }
    void resetPos()
    {
        transform.position = Vector3.zero;

    }

    //Photon Removal [PunRPC]
    public void RespawnPlayer()
    {
        Transform tr = null;
        isDead = false;
        if (!isAI)
        {
            //Photon Removal    tr = SpawnManager.s_Instance.GetSpawnTransformAtIndex(PhotonNetwork.LocalPlayer.ActorNumber);
            transform.position = tr.position;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            //controllerV3.gameObject.GetComponent<RCC_PhotonNetwork>().isDead = false;

        }
        else
        {
            tr = SpawnManager.s_Instance.GetSpawnTransformAtIndex(ActorNumber);
            transform.position = tr.position;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            controllerV3.SetCanControl(true);
        }

        //Photon Removal   controllerV3.gameObject.GetComponent<Rigidbody>().isKinematic = false;
        //Photon Removal   gameObject.GetComponent<HealthAndDamage>().setDefaultValues();
    }

    //Photon Removal  [PunRPC]
    //private void RpcAnnounceKill(string msg, PhotonMessageInfo info)
    //{
    //    InGameNotificationManager.s_Instance.Notify(msg);                                                      //Photon Removal
    //}
}

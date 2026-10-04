using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class HealthAndDamage : MonoBehaviour
{
    public float health;
    public bool m_ZeroHealthHappened;
    public PlayerManager networkPlayer;
    //Field that stores the index of the last player to do damage to this tank.
    public float mLastTime = 0;
    //Field that stores the index of the last player to do damage to this tank.
    //Photon Removal   private Player m_LastDamagedByPlayerNumber = null;

    [SerializeField]
    public int maxHP = 100;
    //Photon Removal   private PhotonView photonView;
    private void Start()
    {
        //Photon Removal photonView = GetComponent<PhotonView>();

        SetHealth(maxHP);
    }

    void SetHealth(float newHealth)
    {
        health = newHealth;
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
    }

    //public void TakeDamageAI(float damage, float lastTime, Player playerNumber)
    //{
    //    mLastTime = lastTime;
    //    m_LastDamagedByPlayerNumber = playerNumber;
    //    health -= damage;

    //    if (health <= 0 && !networkPlayer.isDead)
    //    {
    //        if (networkPlayer.Equals(m_LastDamagedByPlayerNumber))
    //        {
    //            networkPlayer.player.AddScore(-15);

    //        }                                                                                                                                                               //Photon Removal
    //        else
    //        {
    //            m_LastDamagedByPlayerNumber.AddScore(100);

    //        }
    //        DemoGameManagers.Instance.announceKill(m_LastDamagedByPlayerNumber, networkPlayer.player, null);


    //        StartCoroutine(ExplodeTankAI());


    //    }
    //}

    //[PunRPC]
    //public void TakeDamage(float damage,float lastTime,Player playerNumber, PhotonMessageInfo info)
    //{
    //    mLastTime = lastTime;
    //    m_LastDamagedByPlayerNumber = playerNumber;
    //    health -= damage;
    //    if (photonView.IsMine)
    //    {

    //    }
    //    if (health <= 0 && !networkPlayer.isDead)
    //    {
    //        if (photonView.Owner.Equals(m_LastDamagedByPlayerNumber))
    //        {
    //Photon Removal
    //            photonView.Owner.AddScore(-15);

    //        }
    //        else
    //        {
    //            m_LastDamagedByPlayerNumber.AddScore(100);

    //        }
    //        if (photonView.IsMine)
    //        {
    //            StartCoroutine(ExplodeTank());
    //            DemoGameManagers.Instance.announceKill(m_LastDamagedByPlayerNumber, photonView.Owner, photonView);

    //        }
    //    }
    //}

    //public Player lastDamagedByPlayerNumber
    //{
    //    get
    //    {
    //        return m_LastDamagedByPlayerNumber;                                                                                                         //Photon Removal
    //    }

    //}

    public float LastTime
    {
        get
        {
            return mLastTime;
        }

    }




    //public void setDefaultValues()
    //{
    //    m_ZeroHealthHappened = false;
    //    SetHealth(maxHP);
    //    if (photonView && photonView.IsMine)                                                                                                                    //Photon Removal
    //    {

    //    }
    //}



    IEnumerator ExplodeTankAI()
    {

        AI();
        yield return new WaitForSeconds(4f);
        // var spawnPoint = GameManager.instance.GetClearSpawnPoint();
        networkPlayer.RespawnPlayer();
    }

    private void AI()
    {

       

    }
    IEnumerator ExplodeTank()
    {

        //photonView.RPC("RPC_Explode", RpcTarget.All);


        yield return new WaitForSeconds(4f);                                                             
        //photonView.RPC("RespawnPlayer", RpcTarget.All);                                              //Photon Removal
    }



    //Photon Removal    [PunRPC]
    private void RPC_Explode()
    {

    }




}

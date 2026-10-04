
using System.Collections;
using System.Collections.Generic;
using System.Linq;
//using System.Runtime.Remoting.Messaging;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace BEKStudio
{
    public class PuckOffline :MonoBehaviour
    {
     //   public PhotonView photonView;
        Rigidbody2D rb;
        SpriteRenderer spriteRenderer;
        Vector2 puckStartPos;
        Vector2 lastVel;
        public bool practiceMode;
        public bool isMoving;
        public GameObject animator;
        public bool puckInHole;

        private Vector2 XBoundry = new Vector2(-2.04f, 2.04f);
        private Vector2 YBoundry = new Vector2(-2.04f, 2.04f);

        private SpriteRenderer AnimRenderer;
       // private PhotonTransformView photonTransformView;
        private bool isCollided;
        private int puckHoleIndex;
        private bool checkVel;
        private bool isInHole;

        public CircleCollider2D circleCollider2D;

        void Start()
        {
          //  photonView = GetComponent<PhotonView>();
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            puckStartPos = transform.localPosition;

            lastVel = Vector2.zero;
            isMoving = false;
            AnimRenderer = animator.GetComponent<SpriteRenderer>();
           // photonTransformView = GetComponent<PhotonTransformView>();
            if (practiceMode)
            {
                ResetPosition();
            }

            GameControllerOffline.Instance.Event_SwitchingMasterClient += OnSwitchingMasterClient;
            GameControllerOffline.Instance.Event_MasterClientSwithced += OnMasterClientSwitched;
            circleCollider2D = GetComponent<CircleCollider2D>();
        }

        private void OnDestroy()
        {
            GameControllerOffline.Instance.Event_SwitchingMasterClient -= OnSwitchingMasterClient;
            GameControllerOffline.Instance.Event_MasterClientSwithced -= OnMasterClientSwitched;
        }

        void OnSwitchingMasterClient()
        {
            
                RPC_PuckState(puckInHole, puckHoleIndex, GameManager.Instance.masterClient);

            if (puckInHole) return;
            rb.isKinematic = true;
           // photonTransformView.enabled = false;
        }

        void OnMasterClientSwitched()
        {
            if (puckInHole) return;
         //   photonTransformView.enabled = true;
            Invoke(nameof(delay), 0.8f);
        }

        void delay()
        {
            rb.isKinematic = false;

        }

        void RPC_PuckState(bool puckInHole, int puckHoleIndex, bool isOrignalMasterClient) // OrignalMasterClient has white target always
        {
            GameControllerOffline Controller = GameControllerOffline.Instance;

            if (puckInHole)
            {
                if (tag == "Red") return;

                if (isOrignalMasterClient) 
                {
                    //Debug.Log("RPC FROM MASTER....");
                    if (tag.Equals("Black") && !Controller.awayPucksCollected.Contains(gameObject))
                    {
                        Controller.awayPucksCollected.Add(gameObject);
                        OnDisableProperties(true);
                    }
                    else if(tag.Equals("White") && !Controller.homePucksCollected.Contains(gameObject))
                    {
                        Controller.homePucksCollected.Add(gameObject);
                        OnDisableProperties(true);
                    }
                }
                else
                {
                    //Debug.Log("RPC FROM NON MASTER....");
                    if (tag.Equals("Black") && !Controller.awayPucksCollected.Contains(gameObject))
                    {
                        Controller.awayPucksCollected.Add(gameObject);
                        OnDisableProperties(true);
                    }
                    else if (tag.Equals("White") && !Controller.homePucksCollected.Contains(gameObject))
                    {
                        Controller.homePucksCollected.Add(gameObject);
                        OnDisableProperties(true);
                    }
                }
                Controller.UpdateScoreText();
            }
            else
            {
                if (Controller.homePucksCollected.Contains(gameObject))
                {
                    ResetPosition();
                    Controller.homePucksCollected.Remove(gameObject);
                }
                else if (Controller.awayPucksCollected.Contains(gameObject))
                {
                    ResetPosition();
                    Controller.awayPucksCollected.Remove(gameObject);
                }
                //OnActiveProperties();
                Controller.UpdateScoreText();
            }


            ////Handling Worst Case
            //if (!this.puckInHole && isInHole)
            //{
            //    if (Controller.homePucksCollected.Contains(gameObject))
            //    {
            //        ResetPosition();
            //        Controller.homePucksCollected.Remove(gameObject);
            //    }
            //    else if (Controller.awayPucksCollected.Contains(gameObject))
            //    {
            //        ResetPosition();
            //        Controller.awayPucksCollected.Remove(gameObject);
            //    }
            //}
            //else if(this.puckInHole)
            //{
            //    //Debug.Log("RPC FROM NON MASTER....");
            //    if (tag.Equals("Black") && !Controller.awayPucksCollected.Contains(gameObject))
            //    {
            //        Controller.awayPucksCollected.Add(gameObject);
            //        OnDisableProperties(true);
            //    }
            //    else if (tag.Equals("White") && !Controller.homePucksCollected.Contains(gameObject))
            //    {
            //        Controller.homePucksCollected.Add(gameObject);
            //        OnDisableProperties(true);
            //    }

            //    Controller.UpdateScoreText();
            //}

        }
        float movingTime;

        void Update()
        {
            if (movingTime > 0.5f && lastVel.magnitude < 2)
            {
                rb.linearDamping = 4;
            }
            if (movingTime > 0.5f && lastVel.magnitude < 0.5)
            {
                // setVelCheck(false);
                rb.linearDamping = 2;
                rb.linearVelocity = Vector2.zero;
            }
            isMoving = lastVel != Vector2.zero;

            if (isMoving)
            {
                movingTime += Time.deltaTime;
            }
            else
            {
                movingTime = 0;

            }


            CheckOutOfBoundry();
        }

        void FixedUpdate()
        {
            
                lastVel = rb.linearVelocity;
            
        }

       

        void OnTriggerEnter2D(Collider2D col)
        {
            if (col.CompareTag("Hole"))
            {
                if (GameControllerOffline.Instance.gameState == GameControllerOffline.GameState.SWITCH_MASTER)
                {
                    ResetPosition();
                    return;
                }


                GetComponent<CircleCollider2D>().enabled = false;

                    //For AI
                    RPC_HandleCollision(col.gameObject.GetComponent<PuckHoleOffline>().index);
                

            }
        }


        

        private void RPC_HandleCollision(int index)
        {
            Collider2D col;
            //col = GameController.Instance.PuckHolesMaster[index].GetComponent<Collider2D>();
            //if (photonView.IsMine)
            //{
            //    col = GameController.Instance.PuckHolesMaster[index].GetComponent<Collider2D>();
            //}
            //else
            //{
            //    col = GameController.Instance.PuckHolesNonMaster[index].GetComponent<Collider2D>();
            //}

            col = GameControllerOffline.Instance.PuckHolesMaster[index].GetComponent<Collider2D>();

            OnDisableProperties();
            col.GetComponent<PuckHoleOffline>().PutPuckOnPuckHole(gameObject);
            PuckOnHole(gameObject.tag, gameObject.name);
        }

        public void OnDisableProperties(bool HideSprite = false)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
            lastVel = Vector2.zero;
            isMoving = false;
            AnimRenderer.color = new Color32(255, 255, 255, 0);
            if (HideSprite)
                spriteRenderer.color = new Color32(255, 255, 255, 0);
            puckInHole = true;
        }

        public void ResetPosition()
        {
           
            if (practiceMode)
            {
                transform.localPosition = new Vector2(Random.Range(-1.89f, 1.89f), Random.Range(-1.16f, 1.16f));
            }
            else
            {
                
                    transform.localPosition = puckStartPos;
                
            }
            OnActiveProperties();
            lastVel = Vector2.zero;
        }

        public void ResetAndRemove()
        {
            
                RPCResetAndRemove();
            
           
        }
        void RPCResetAndRemove()
        {
            GameControllerOffline Controller = GameControllerOffline.Instance;

            if (Controller.homePucksCollected.Contains(gameObject))
            {
                Controller.homePucksCollected.Remove(gameObject);
            }
            else if (Controller.awayPucksCollected.Contains(gameObject))
            {
                Controller.awayPucksCollected.Remove(gameObject);
            }
            ResetPosition();
            Controller.UpdateScoreText();
        }


        private void OnActiveProperties()
        {
            puckInHole = false;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true;
            spriteRenderer.color = new Color32(255, 255, 255, 255);
            AnimRenderer.color = new Color32(255, 255, 255, 255);
            isMoving = false;
            GetComponent<CircleCollider2D>().enabled = true;
            transform.localPosition = puckStartPos;

        }

        void PuckOnHole(string puckTag, string puckName)
        {
            GameControllerOffline.Instance.PuckOnHole(puckTag, puckName);
        }

        void OnCollisionEnter2D(Collision2D col)
        {
            if (col.gameObject.CompareTag("Player"))
            {
                if (!GetComponent<AudioSource>().isPlaying)
                {
                    GetComponent<AudioSource>().Play();
                }
            }
        }

        //private void OnTriggerStay2D(Collider2D collision)
        //{
        //    if (collision.CompareTag("Hole"))
        //    {
        //        isInHole = true;
        //    }
        //    else
        //    {
        //        isInHole = false;
        //    }
        //}



        public void CheckOutOfBoundry()
        {
            if (puckInHole)
                return;

            Vector3 clampedPosition = transform.localPosition;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, XBoundry.x, XBoundry.y);
            clampedPosition.y = Mathf.Clamp(clampedPosition.y, YBoundry.x, YBoundry.y);
            transform.localPosition = clampedPosition;
        }
        public void RPC_ResetRedPuck()
        {
            if (practiceMode)
            {
                transform.localPosition = new Vector2(Random.Range(-1.89f, 1.89f), Random.Range(-1.16f, 1.16f));
            }
            else
            {
                    transform.localPosition = puckStartPos;
                
            }
            GameControllerOffline.Instance.leftRedPuckIcon.SetActive(false);
            GameControllerOffline.Instance.rightRedPuckIcon.SetActive(false);
        
            puckInHole = false;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true;
            spriteRenderer.color = new Color32(255, 255, 255, 255);
            AnimRenderer.color = new Color32(255, 255, 255, 255);
            lastVel = Vector2.zero;
            isMoving = false;
        }


        public void setVelCheck(bool state)
        {
            //Debug.Log($"Setting.... {state}");
            checkVel = state;
        }
    }
}

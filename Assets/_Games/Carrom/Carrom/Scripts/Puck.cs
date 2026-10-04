using Mirror;
using System.Linq;
using UnityEngine;
using UnityExtensions;

namespace BEKStudio
{
    public class Puck : NetworkBehaviour
    {
        public Rigidbody2D rb;
        public SpriteRenderer spriteRenderer;
        public Vector2 puckStartPos;

        [SyncVar]
        public Vector2 lastVel;

        public bool practiceMode;
        public bool isMoving;
        public GameObject animator;

        [SyncVar]
        public bool puckInHole;

        private Vector2 XBoundry = new Vector2(-2.04f, 2.04f);
        private Vector2 YBoundry = new Vector2(-2.04f, 2.04f);

        public SpriteRenderer AnimRenderer;
        public NetworkTransformUnreliable networkTransform;
        private SpriteRenderer shadowRenderer;
        private bool isCollided;

        [SyncVar]
        private int puckHoleIndex;

        private bool checkVel;
        private bool isInHole;

        public CircleCollider2D circleCollider2D;

        // Captured in Awake (before Mirror's spawn message applies the synced
        // transform) so reconnecting clients don't pick up a hole position as
        // the puck's "start" and end up resetting pucks into a corner.
        void Awake()
        {
            puckStartPos = transform.localPosition;
            shadowRenderer = transform.Find("Shadow")?.GetComponent<SpriteRenderer>();
        }

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            lastVel = Vector2.zero;
            isMoving = false;
            AnimRenderer = animator.GetComponent<SpriteRenderer>();
            networkTransform = GetComponent<NetworkTransformUnreliable>();



            GameController.Instance.Event_SwitchingMasterClient += OnSwitchingMasterClient;
            GameController.Instance.Event_MasterClientSwithced += OnMasterClientSwitched;
            circleCollider2D = GetComponent<CircleCollider2D>();
        }

        private void OnDestroy()
        {
            GameController.Instance.Event_SwitchingMasterClient -= OnSwitchingMasterClient;
            GameController.Instance.Event_MasterClientSwithced -= OnMasterClientSwitched;
        }

        void OnSwitchingMasterClient()
        {
            if (GameController.Instance.isCarronMultiplayerGame)
                CarromNetworkManager.instance.CmdPuckState(this.gameObject, puckInHole, puckHoleIndex, GameManager.Instance.masterClient);
            else
                CarromNetworkManager.instance.RpcPuckState(this.gameObject, puckInHole, puckHoleIndex, GameManager.Instance.masterClient);

            if (puckInHole) return;
            rb.isKinematic = true;
            if (networkTransform != null)
                networkTransform.enabled = false;
        }

        void OnMasterClientSwitched()
        {
            if (puckInHole) return;
            if (networkTransform != null)
                networkTransform.enabled = true;
            Invoke(nameof(delay), 0.8f);
        }

        void delay()
        {
            rb.isKinematic = false;
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
                GameController.Instance.gameState.ToString().Show("GameState");
                if (GameController.Instance.gameState == GameController.GameState.SWITCH_MASTER)
                {
                    ResetPosition();
                    return;
                }


                GetComponent<CircleCollider2D>().enabled = false;

                if (GameController.Instance.isCarronMultiplayerGame)
                {
                    if (NetworkServer.active)
                    {
                        // For Online
                        puckHoleIndex = col.gameObject.GetComponent<PuckHole>().index;
                        rb.linearVelocity = Vector2.zero;
                        ProcessCollisionWithRPC(puckHoleIndex);
                        CarromNetworkManager.instance.RpcHandleCollision(puckHoleIndex, this.gameObject.name);
                    }
                }
                else
                {
                    // For AI
                    CarromNetworkManager.instance.RpcHandleCollision(col.gameObject.GetComponent<PuckHole>().index, this.gameObject.name);
                }
            }
        }

        public void ProcessCollisionWithRPC(int index)
        {
            "OnDisableProperties".Show();
            Collider2D col = BEKStudio.GameController.Instance.PuckHolesMaster[index].GetComponent<Collider2D>();

            OnDisableProperties();
            col.GetComponent<PuckHole>().PutPuckOnPuckHole(gameObject);
            PuckOnHole(gameObject.tag, gameObject.name);
        }


        public void BroadCastResetPosition()
        {
            if (!NetworkServer.active)
                CarromNetworkManager.instance.CmdResetRedPuck();
            else
                CarromNetworkManager.instance.RpcResetRedPuck();
        }



        public void OnDisableProperties(bool HideSprite = false)
        {
            if (networkTransform != null)
                networkTransform.enabled = false;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
            lastVel = Vector2.zero;
            isMoving = false;
            AnimRenderer.color = new Color32(255, 255, 255, 0);
            SetShadowVisible(false);
            if (HideSprite)
                spriteRenderer.color = new Color32(255, 255, 255, 0);
            puckInHole = true;
        }

        public void ResetPosition()
        {
            transform.localPosition = puckStartPos;
            OnActiveProperties();
            lastVel = Vector2.zero;
        }

        public void ResetAndRemove()
        {
            "ResetAndRemove Puck Called".Show();
            // Multiplayer mode
            if (GameController.Instance.isCarronMultiplayerGame)
            {
                if (NetworkServer.active) // Only server can call ClientRpc
                {
                    "ResetAndRemove Puck on Server".Show();
                    // Reset locally on the server
                    CarromNetworkManager.instance.DoResetAndRemove(gameObject.name);
                    CarromNetworkManager.instance.RpcResetAndRemove(gameObject.name);
                    this.Delay(0.5f, () =>
                    {
                        string[] homeNames = BEKStudio.GameController.Instance.homePucksCollected.Select(go => go.name).ToArray();
                        string[] awayNames = BEKStudio.GameController.Instance.awayPucksCollected.Select(go => go.name).ToArray();
                        CarromNetworkManager.instance.RpcApplyDataFromServer(homeNames, awayNames, BEKStudio.GameController.currentHomeScore, BEKStudio.GameController.currentAwayScore);

                    });
                    // Tell all clients to do the same
                }
            }
            else
            {
                // Offline/local mode
                CarromNetworkManager.instance.DoResetAndRemove(gameObject.name);
            }
        }



        private void OnActiveProperties()
        {
            if (networkTransform != null)
                networkTransform.enabled = true;
            puckInHole = false;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true;
            spriteRenderer.color = new Color32(255, 255, 255, 255);
            AnimRenderer.color = new Color32(255, 255, 255, 255);
            SetShadowVisible(true);
            isMoving = false;
            GetComponent<CircleCollider2D>().enabled = true;
            transform.localPosition = puckStartPos;
        }

        private void SetShadowVisible(bool visible)
        {
            if (shadowRenderer == null)
                shadowRenderer = transform.Find("Shadow")?.GetComponent<SpriteRenderer>();

            if (shadowRenderer != null)
                shadowRenderer.enabled = visible;
        }

        public void PuckOnHole(string puckTag, string puckName)
        {
            //if (CarromNetworkManager.instance.PlayercurrentTurnId==staticVariables.UserProfiledata.user._id.ToString())
            {
                // CarromNetworkManager.instance.CmdPuckOnHole(puckTag, puckName);
                BEKStudio.GameController.Instance.PuckOnHole(puckTag, puckName);
            }
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

        public void CheckOutOfBoundry()
        {
            if (puckInHole)
                return;

            Vector3 current = transform.localPosition;
            Vector3 clampedPosition = current;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, XBoundry.x, XBoundry.y);
            clampedPosition.y = Mathf.Clamp(clampedPosition.y, YBoundry.x, YBoundry.y);

            // Only write when the clamp actually moved something. The Rigidbody2D now
            // interpolates, so transform.localPosition is the *interpolated* (a fraction
            // of a step behind) pose — assigning it back every frame teleported the body
            // slightly backwards each frame and made the pucks crawl/jitter.
            if (clampedPosition != current)
                transform.localPosition = clampedPosition;
        }



        public void setVelCheck(bool state)
        {
            checkVel = state;
        }
    }
}

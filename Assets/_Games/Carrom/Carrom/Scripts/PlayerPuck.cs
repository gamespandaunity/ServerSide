using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using BEKStudio;
using UnityEngine.PlayerLoop;
using UnityExtensions;

namespace BEKStudio
{
    public class PlayerPuck : NetworkBehaviour
    {
        public static PlayerPuck Instance;
        public Rigidbody2D rb;
        SpriteRenderer spriteRenderer;
        public PhysicsMaterial2D physicMaterial;
        public Vector2 startPos;
        public Vector2 endPos;
        public bool isTouch;

        [SyncVar]
        float opponentSliderValue;

        public Vector2 puckStartPos;
        public GameObject arrow;
        public float force;
        public float forceMultiplier;

        [SyncVar]
        public Vector2 lastVel;

        public bool isMoving;
        private Vector2 arrowLocalScale;
        public GameObject tutorial;
        public GameObject Highligher;
        public float tutorialShowDelay = 2;
        public GameObject warningStrikerPuck;

        private Vector2 XBoundry = new Vector2(-2.118f, -2.118f);
        private Vector2 YBoundry = new Vector2(-2.160f, 2.160f);
        public Vector3 masterPosition, clientPosition;

        [SyncVar]
        private bool arrowActive;

        [SyncVar]
        private Vector3 arrowScale;

        [SyncVar]
        private Quaternion arrowRotation;

        [SyncVar]
        private float playerSliderValue;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            // Initialize baseline striker positions for master and client players
            // Master player shoots from bottom of board (y = -1.67f)
            // Client player shoots from top of board (y = 1.67f), but their board is rotated 180°
            masterPosition = new Vector3(0f, -1.67f, 0f);
            clientPosition = new Vector3(0f, 1.67f, 0f);

            //puckStartPos = new Vector3(0, -1.649f, 0);
        }


        void Start()
        {
            transform.SetParent(GameController.Instance.strikerPositionAdjuster.transform);
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            // Snap to our baseline immediately so the striker never appears
            // floating in the middle of the board at game start. ResetPosition's
            // multiplayer branch is gated on a DelayUntil(syncDirection ==
            // ClientToServer) which doesn't unblock until CmdSwitchTurn finishes
            // assigning authority — until then the prefab default position is
            // what the player sees.
            //
            // Skip on the dedicated server: it has no `isMasterClient` role and
            // would force the wrong baseline, which NetworkTransform might then
            // sync back to a client before authority flips. Only the local
            // viewer needs this snap.
            if (NetworkClient.active && !NetworkServer.active
                && GameManager.Instance.currentGameMode != GameManager.GameMode.Ai
                && MirrorNetwork.Instance != null)
            {
                float startY = MirrorNetwork.Instance.isMasterClient
                    ? masterPosition.y
                    : clientPosition.y;
                transform.localPosition = new Vector3(0f, startY, 0f);
            }

            puckStartPos = transform.localPosition;

            opponentSliderValue = 0.5f;

            lastVel = Vector2.zero;
            isMoving = false;
            arrowLocalScale = arrow.transform.localScale;

            GameController.Instance.Event_MasterClientSwithced += OnMasterClientSwitched;
        }

        private void OnDestroy()
        {
            Instance = null;

            if (LeanTween.isTweening(gameObject))
            {
                LeanTween.cancel(gameObject);
                LeanTween.cancelAll();
            }
            StopAllCoroutines();

            GameController.Instance.Event_MasterClientSwithced -= OnMasterClientSwitched;
        }

        void OnMasterClientSwitched()
        {
            Debug.Log("OnMasterClientSwitchedPP");
            // Re-arm the per-turn striker hint ("golden turn indicator"). The highlighter only
            // shows once tutorialShowDelay counts down to 0 AND the tutorial object is currently
            // hidden — but both were only ever reset by a local mouse-up. After a TURN SWITCH the
            // leftover values kept the show-condition false, so the golden highlight above the
            // striker never appeared on the new player's turn. Reset them on every turn change.
            tutorialShowDelay = 2;
            if (tutorial != null) tutorial.SetActive(false);
            ResetPosition();
            GameController.Instance.strikerPositionAdjuster.SetPosition();
            GameController.Instance.strikerPositionAdjuster.SetSliderPosition(GameController.Instance.playerSlider);
        }

        void Update()
        {
            if (checkVel)
            {
                float speed = lastVel.magnitude;

                if (speed < 0.5f)
                {
                    setVelCheck(false);
                    rb.linearDamping = 2f;
                    rb.linearVelocity = Vector2.zero;
                }
                else if (speed < 4f)
                {
                    rb.linearDamping = 4f;
                }
            }

            isMoving = lastVel != Vector2.zero;

            if (GameController.Instance.opponentSlider != null)
            {
                GameController.Instance.opponentSlider.value = Mathf.MoveTowards(
                    GameController.Instance.opponentSlider.value, opponentSliderValue, 5 * Time.deltaTime);
            }

            if (isOwned)
            {
                opponentSliderValue = 0.5f;

                if (rb.sharedMaterial == null)
                {
                    rb.sharedMaterial = physicMaterial;
                }

                if (arrow.activeInHierarchy)
                {
                    arrow.transform.localScale =
                        Vector2.MoveTowards(arrow.transform.localScale, arrowLocalScale, 5 * Time.deltaTime);
                }

                if (!isTouch && GameController.Instance.gameState == GameController.GameState.READY)
                {
                    if (tutorialShowDelay > 0)
                    {
                        tutorialShowDelay -= 1 * Time.deltaTime;
                    }
                    else
                    {
                        if (!tutorial.activeInHierarchy && GameController.Instance.CurrentTurn == GameController.CurrentPlayer.ME)
                        {
                            Highligher.SetActive(true);
                            tutorial.SetActive(true);
                        }
                    }
                }
            }
            else
            {
                tutorial.SetActive(false);
                Highligher.SetActive(false);
                if (rb.sharedMaterial != null)
                {
                    rb.sharedMaterial = null;
                }

                if (arrow.activeInHierarchy)
                {
                    arrow.transform.localScale = Vector2.MoveTowards(arrow.transform.localScale, arrowLocalScale,
                        50 * Time.deltaTime);
                }

                return;
            }

            if (isTouch)
            {
                endPos = Input.mousePosition;

                forceMultiplier = Mathf.InverseLerp(0, 500, ((endPos - startPos).magnitude) * 2.3f);

                if (forceMultiplier >= 0.2f)
                {
                    arrow.GetComponent<SpriteRenderer>().enabled = true;
                }
                else
                {
                    arrow.GetComponent<SpriteRenderer>().enabled = false;
                }

                arrow.transform.localScale = new Vector3(forceMultiplier * 3f, forceMultiplier * 3f, 1);

                float AngleRad = Mathf.Atan2(-(endPos.y - startPos.y), -(endPos.x - startPos.x));
                float AngleDeg = (180 / Mathf.PI) * AngleRad;
                transform.rotation = Quaternion.Euler(0, 0, AngleDeg);
            }
        }

        void FixedUpdate()
        {
            if (isOwned)
            {
                lastVel = rb.linearVelocity;

                // Move-area guard (belt-and-suspenders): while it's MY turn and I'm still
                // positioning/aiming (READY), keep the striker pinned to its baseline Y and
                // clamped inside the lane X. FreezePositionY (set in ResetPosition) is the
                // primary lock; this corrects any transform-level drift in LOCAL space before
                // NetworkTransform samples it. Authority is removed at shot start (CmdShoot),
                // so isOwned turns false and this stops — it never fights the actual shot.
                if (GameController.Instance != null
                    && GameController.Instance.gameState == GameController.GameState.READY
                    && GameController.Instance.CurrentTurn == GameController.CurrentPlayer.ME)
                {
                    float baselineY = (MirrorNetwork.Instance != null && MirrorNetwork.Instance.isMasterClient)
                        ? masterPosition.y
                        : clientPosition.y;

                    Vector3 lp = transform.localPosition;
                    float clampedX = Mathf.Clamp(lp.x,
                        GameController.Instance.playerPuckMinX,
                        GameController.Instance.playerPuckMaxX);

                    if (lp.x != clampedX || lp.y != baselineY || lp.z != 0f)
                    {
                        transform.localPosition = new Vector3(clampedX, baselineY, 0f);
                    }

                    if (rb.linearVelocity != Vector2.zero)
                    {
                        rb.linearVelocity = Vector2.zero;
                        lastVel = Vector2.zero;
                    }
                }
            }
        }

        public void OnMouseDown()
        {
            if (!isOwned || GameController.Instance.gameState != GameController.GameState.READY || GameController.Instance.CurrentTurn != GameController.CurrentPlayer.ME) return;

            Highligher.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);
            arrow.transform.localScale = Vector2.zero;
            arrow.SetActive(true);
            startPos = Input.mousePosition;
            endPos = startPos;

            isTouch = true;

            tutorial.SetActive(false);
            tutorialShowDelay = 2;
        }

        public void OnMouseUp()
        {
            if (!isOwned || GameController.Instance.gameState != GameController.GameState.READY || GameController.Instance.CurrentTurn != GameController.CurrentPlayer.ME) return;
            Highligher.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);

            arrow.SetActive(false);
            isTouch = false;
            forceMultiplier.Show("Force Multiplier");
            if (forceMultiplier >= 0.2f)
            {
                if (GameManager.Instance.currentGameMode.Equals(GameManager.GameMode.Ai))
                {
                    // AI mode: calculate shootAngle from local transform
                    float shootAngle = Mathf.Atan2(transform.right.y, transform.right.x) * Mathf.Rad2Deg;
                    Shoot(shootAngle);
                }
                else
                {
                    // Calculate shoot direction accounting for parent table rotation
                    Vector3 shootDirection = transform.right;
                    if (MirrorNetwork.Instance != null && !MirrorNetwork.Instance.isMasterClient)
                    {
                        shootDirection = -shootDirection;
                    }
                    // Encode direction as angle (0 = forward/right, 180 = backward/left)
                    float shootAngle = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
                    CarromNetworkManager.instance.CmdShoot(forceMultiplier, transform.localPosition, transform.eulerAngles.z, shootAngle);
                }
            }
        }



        public void Shoot(float shootAngle)
        {
            if (Highligher != null)
            {
                Highligher.SetActive(false);
            }

            if (rb != null)
            {
                // Release the READY-phase move-area lock so the shot can travel freely in Y.
                // Centralised here (shot start) — never in UI code — so no turn/authority edge
                // case (reconnect, shoot-again, scratch respot) can leave the striker frozen. On
                // the dedicated server this Shoot() runs after CmdShoot removes authority; in AI
                // mode it runs locally. Either way the striker that fires is the one unlocked.
                rb.constraints = RigidbodyConstraints2D.None;
                rb.isKinematic = false;
                Debug.Log("forceMultiplier: " + forceMultiplier);
                Debug.Log("force: " + force);
                
                // Convert shootAngle to direction vector
                // shootAngle is pre-calculated on client accounting for table rotation
                Vector3 shootDirection = new Vector3(Mathf.Cos(shootAngle * Mathf.Deg2Rad), Mathf.Sin(shootAngle * Mathf.Deg2Rad), 0f);
                
                rb.AddForce(shootDirection * (forceMultiplier * force), ForceMode2D.Impulse);
            }

            if (GameController.Instance != null)
            {
                GameController.Instance.Shoot();
            }
        }

        public void ResetPosition()
        {
            Debug.Log("ResetPlayePuckos");
            Highligher.SetActive(false);
            Highligher.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);

            transform.rotation = Quaternion.Euler(0, 0, 90);
            if (GameManager.Instance.currentGameMode == GameManager.GameMode.Ai)
            {
                transform.localPosition = new Vector2(0f, -1.67f);

                if (GameController.Instance.CurrentTurn == GameController.CurrentPlayer.OTHER)
                {
                    arrowLocalScale = Vector2.zero;
                    arrow.transform.localScale = Vector2.zero;
                    transform.localPosition = new Vector2(puckStartPos.x, -puckStartPos.y - 0.08f);
                    Debug.Log("OtherpuckRest");
                }
                else
                {
                    Debug.Log("MepuckRest");
                    transform.localPosition = new Vector2(puckStartPos.x, puckStartPos.y);
                }
                GameController.Instance.strikerPositionAdjuster.SetPosition();
            }
            else
            {
                GetComponent<NetworkIdentity>().isOwned.Show("IsOwned");
                if (!NetworkServer.active)
                {
                    // Striker-position fix ("striker at the TOP bar on my own turn" / "striker not
                    // updated after reconnection"): ownership (AssignClientAuthority) often lands
                    // AFTER the RpcSwitchTurn that triggered this reset, so the old
                    // `if (isOwned)` ONE-TIME gate skipped the snap entirely — the striker then
                    // stayed wherever the OPPONENT's last shot left it (visually at the active
                    // player's top). WAIT for ownership + sync direction instead of sampling once;
                    // bail out harmlessly if the turn isn't (or stops being) ours.
                    this.DelayUntil(() => GameController.Instance == null
                                          || GameController.Instance.CurrentTurn != GameController.CurrentPlayer.ME
                                          || (GetComponent<NetworkIdentity>().isOwned
                                              && GetComponent<NetworkTransformUnreliable>().syncDirection == SyncDirection.ClientToServer), () =>
                    {
                        if (GameController.Instance == null) return;
                        if (GameController.Instance.CurrentTurn != GameController.CurrentPlayer.ME) return;
                        if (!GetComponent<NetworkIdentity>().isOwned) return;
                        rb.linearVelocity = Vector2.zero;
                        if (GameController.Instance.CurrentTurn == GameController.CurrentPlayer.ME)
                        {
                            GameController.Instance.CurrentTurn.Show("CurrentTurn");
                            float resetY;
                            if (MirrorNetwork.Instance.isMasterClient)
                            {
                                Debug.Log("IsMasterpuckRest");
                                resetY = masterPosition.y;
                            }
                            else
                            {
                                Debug.Log("NotIsMasterpuckRest");
                                resetY = clientPosition.y;
                            }

                            // Set center position first
                            transform.localPosition = new Vector3(0, resetY, 0);

                            // Adjust X to avoid overlap with pucks
                            GameController.Instance.strikerPositionAdjuster.SetPosition();
                            GameController.Instance.strikerPositionAdjuster.SetSliderPosition(GameController.Instance.playerSlider);

                            // Send final adjusted position to server
                            CarromNetworkManager.instance.CmdSetPosition(transform.localPosition.x, transform.localPosition.y);
                            Debug.Log("Localtransform" + transform.localPosition);
                        }
                    });
                }

                Debug.Log("SetPosition");

            }

            rb.linearVelocity = Vector2.zero;
            rb.simulated = true;
            // Re-arm the move-area lock for the upcoming READY/aiming phase: freeze Y (and
            // rotation) so a puck sitting in the striker's lane can't shove it off the baseline.
            // Released again only at shot start in Shoot(). (See codex review.)
            rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            spriteRenderer.color = new Color32(255, 255, 255, 255);
            lastVel = Vector2.zero;
            isMoving = false;
        }
        public void ShootBot()
        {
            StartCoroutine(ShootBotCoroutine());
        }

        IEnumerator ShootBotCoroutine()
        {
            "bot shoot started".ShowBlue();
            arrowLocalScale = Vector2.zero;
            arrow.transform.localScale = Vector2.zero;
            transform.localPosition = new Vector2(puckStartPos.x, -puckStartPos.y - 0.08f);
            GameController.Instance.strikerPositionAdjuster.SetPosition();

            yield return new WaitForSeconds(0.5f);

            Vector2 newPos = transform.position;
            forceMultiplier = 0;
            arrow.SetActive(true);

            Puck[] blackPucks = GameController.Instance.allPucks.Where(x => x.CompareTag("Black") && !x.gameObject.GetComponent<Puck>().puckInHole).ToArray();

            Puck target = blackPucks[Random.Range(0, blackPucks.Length)];

            if ((blackPucks.Length == 1 || Random.Range(0, 1000) > 600) && !GameController.Instance.redPuckCollected && !GameController.Instance.redPuckWaiting)
            {
                target = GameController.Instance.redPuck.GetComponent<Puck>();
            }

            if (target.transform.position.x > 0)
            {
                newPos.x += Random.Range(0f, 1f);
            }
            else
            {
                newPos.x -= Random.Range(0f, 1f);
            }

            newPos.x = Mathf.Clamp(newPos.x, -1.25f, 1.25f);

            yield return new WaitForSeconds(Random.Range(1.5f, 4f));

            GameController.Instance.OnDown();

            LeanTween.move(gameObject, newPos, 0.5f)
                .setIgnoreTimeScale(false)
                .setOnComplete(() =>
                {
                    GameController.Instance.OnUp();
                    Vector2 diff = target.transform.position - transform.position;
                    diff.Normalize();
                    float zRot = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0, 0, zRot);

                    LeanTween.value(0, 1f, 1f).setOnUpdate((float val) =>
                    {
                        if (arrow != null)
                        {
                            arrow.transform.localScale = new Vector3(val * 2.3f, val * 2.3f, 1);
                        }
                    }).setIgnoreTimeScale(false)
                    .setOnComplete(() =>
                    {
                        if (arrow != null)
                        {
                            arrow.SetActive(false);
                        }
                        forceMultiplier = 1f;
                        // Calculate shootAngle from current transform rotation
                        float shootAngle = Mathf.Atan2(transform.right.y, transform.right.x) * Mathf.Rad2Deg;
                        if (GameManager.Instance.currentGameMode.Equals(GameManager.GameMode.Ai))
                        {
                            Shoot(shootAngle);
                        }
                        else
                        {
                            CarromNetworkManager.instance.CmdShoot(forceMultiplier, transform.localPosition, transform.eulerAngles.z, shootAngle);
                        }
                    });
                });
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            if (col.CompareTag("Hole"))
            {
                if (GameController.Instance.gameState == GameController.GameState.SWITCH_MASTER)
                    return;
                if (GameController.Instance.CurrentTurn.Equals(GameController.CurrentPlayer.ME))
                {
                    GameController.Instance.statusPanelText.text = "Oops! Foul";
                    GameController.Instance.StatusPanelActive();
                }

                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
                lastVel = Vector2.zero;
                isMoving = false;

                col.GetComponent<PuckHole>().PutStrikerOnPuckHole(gameObject, () =>
                {
                    PuckOnHole(gameObject.tag, gameObject.name);
                });
            }
        }


        void PuckOnHole(string puckTag, string puckName)
        {
            if (NetworkServer.active)
            {
                BEKStudio.GameController.Instance.PuckOnHole(puckTag, puckName);
                CarromNetworkManager.instance.RpcPuckOnHolePlayerpuck(puckTag, puckName);
            }
        }



        void CheckOutOfBoundry()
        {
            Vector3 current = transform.localPosition;
            Vector3 clampedPosition = current;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, XBoundry.x, XBoundry.y);
            clampedPosition.y = Mathf.Clamp(clampedPosition.y, YBoundry.x, YBoundry.y);

            // Only write when the clamp actually moved something — see Puck.cs: with
            // Rigidbody2D interpolation on, re-assigning the interpolated pose every
            // frame drags the body backwards and makes the striker feel sticky.
            if (clampedPosition != current)
                transform.localPosition = clampedPosition;
        }

        public void SendData(bool[] dataArray, bool IsOrigMaster)
        {
            if (isOwned)
            {
                CarromNetworkManager.instance.CmdSendData(dataArray, IsOrigMaster);
            }
        }



        public void BroadCastSliderUp()
        {
            if (isOwned)
            {
                CarromNetworkManager.instance.CmdSliderUp();
            }
        }



        public void BroadCastSliderDown()
        {
            if (isOwned)
            {
                CarromNetworkManager.instance.CmdSliderDown();
            }
        }



        private bool checkVel;

        public void setVelCheck(bool state)
        {
            checkVel = state;
        }
    }
}

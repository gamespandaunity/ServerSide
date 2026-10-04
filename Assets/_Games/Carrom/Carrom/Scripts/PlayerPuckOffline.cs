using BEKStudio;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static UnityEngine.RuleTile.TilingRuleOutput;

namespace BEKStudio{
    public class PlayerPuckOffline : MonoBehaviour{
        public static PlayerPuckOffline Instance;
        [SerializeField] Rigidbody2D rb;
        [SerializeField] SpriteRenderer spriteRenderer;
       // PhotonView photonView;
        public PhysicsMaterial2D physicMaterial;
        public Vector2 startPos;
        public Vector2 endPos;
        public bool isTouch;
        float opponentSliderValue;
       [SerializeField] Vector2 puckStartPos;
        public GameObject arrow;
        public float force;
        float forceMultiplier;
        public Vector2 lastVel;
        public bool isMoving;
        private Vector2 arrowLocalScale;
        public GameObject tutorial;
        public GameObject Highligher;
        public float tutorialShowDelay = 2;
        public GameObject warningStrikerPuck;
        //public CircleCollider2D playerCollider;

        private Vector2 XBoundry = new Vector2(-2.118f, -2.118f);
        private Vector2 YBoundry = new Vector2(-2.160f, 2.160f);
        void Awake(){
            if (Instance == null){
                Instance = this;
            }
        }

        void Start(){
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            //  photonView = GetComponent<PhotonView>();
            transform.localPosition = new Vector2(0f, -1.67f);
           puckStartPos = transform.localPosition;

            Debug.Log("puckStartPos" + puckStartPos);
            opponentSliderValue = 0.5f;

            lastVel = Vector2.zero;
            isMoving = false;
            arrowLocalScale = arrow.transform.localScale;

            GameControllerOffline.Instance.Event_MasterClientSwithced += OnMasterClientSwitched;
        }

        private void OnDestroy()
        {
            if (LeanTween.isTweening(gameObject))
            {
                LeanTween.cancel(gameObject);
                LeanTween.cancelAll();
            }
            StopAllCoroutines();

            GameControllerOffline.Instance.Event_MasterClientSwithced -= OnMasterClientSwitched;
        }

        void OnMasterClientSwitched()
        {
            ResetPosition();
            GameControllerOffline.Instance.strikerPositionAdjuster.SetPosition();
        }

        void Update(){


            if (checkVel && lastVel.magnitude < 4)
            {
                rb.linearDamping = 4;
            }
            if (checkVel && lastVel.magnitude < 0.5)
            {
                setVelCheck(false);
                rb.linearDamping = 2;
                rb.linearVelocity = Vector2.zero;
            }

            isMoving = lastVel != Vector2.zero;

            if (GameControllerOffline.Instance.opponentSlider != null){
                GameControllerOffline.Instance.opponentSlider.value = Mathf.MoveTowards(
                    GameControllerOffline.Instance.opponentSlider.value, opponentSliderValue, 5 * Time.deltaTime);
            }

            // if (photonView.IsMine){
            //     opponentSliderValue = 0.5f;

            //     if (rb.sharedMaterial == null){
            //         rb.sharedMaterial = physicMaterial;
            //     }

            //     if (arrow.activeInHierarchy){
            //         arrow.transform.localScale =
            //             Vector2.MoveTowards(arrow.transform.localScale, arrowLocalScale, 5 * Time.deltaTime);
            //     }

            //     if (!isTouch && GameControllerOffline.Instance.gameState == GameControllerOffline.GameState.READY){
            //         if (tutorialShowDelay > 0){
            //             tutorialShowDelay -= 1 * Time.deltaTime;
            //         }
            //         else{
            //             if (!tutorial.activeInHierarchy && GameControllerOffline.Instance.whichPlayer == GameControllerOffline.WhichPlayer.ME){
            //                 Highligher.SetActive(true);
            //                 tutorial.SetActive(true);
            //             }
            //         }
            //     }
            // }
            // else{
                tutorial.SetActive(false);
                Highligher.SetActive(false);
                if (rb.sharedMaterial != null){
                    rb.sharedMaterial = null;
                }

                if (arrow.activeInHierarchy){
                    arrow.transform.localScale = Vector2.MoveTowards(arrow.transform.localScale, arrowLocalScale,
                        50 * Time.deltaTime);
                }

               // return;
           // }

            if (isTouch){
                endPos = Input.mousePosition;
                
                forceMultiplier = Mathf.InverseLerp(0, 500, ((endPos - startPos).magnitude)*2.3f);
                //Debug.LogError((endPos - startPos).magnitude);

                if (forceMultiplier >= 0.2f){
                    arrow.GetComponent<SpriteRenderer>().enabled = true;
                }
                else{
                    arrow.GetComponent<SpriteRenderer>().enabled = false;
                }

                arrow.transform.localScale = new Vector3(forceMultiplier * 3f, forceMultiplier * 3f, 1);

                float AngleRad = Mathf.Atan2(-(endPos.y - startPos.y), -(endPos.x - startPos.x));
                float AngleDeg = (180 / Mathf.PI) * AngleRad;
                transform.rotation = Quaternion.Euler(0, 0, AngleDeg);
            }

            //CheckOutOfBoundry();
        }

        void FixedUpdate(){
            
                lastVel = rb.linearVelocity;
            
        }

        public void OnMouseDown(){
            if ( GameControllerOffline.Instance.gameState != GameControllerOffline.GameState.READY || GameControllerOffline.Instance.whichPlayer != GameControllerOffline.WhichPlayer.ME) return;

            Highligher.transform.localScale = new Vector3(3.5f,3.5f,3.5f);
            arrow.transform.localScale = Vector2.zero;
            arrow.SetActive(true);
            startPos = Input.mousePosition;
            endPos = startPos;

            isTouch = true;

            tutorial.SetActive(false);
            tutorialShowDelay = 2;

        }

        public void OnMouseUp(){
            if ( GameControllerOffline.Instance.gameState != GameControllerOffline.GameState.READY || GameControllerOffline.Instance.whichPlayer != GameControllerOffline.WhichPlayer.ME) return;
            Highligher.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);

            arrow.SetActive(false);
            //Highligher.SetActive(false);
            isTouch = false;

            if (forceMultiplier >= 0.2f){
                if (GameManager.Instance.currentGameMode.Equals(GameManager.GameMode.Ai))
                {
                    Shoot();
                }
            }
        }

        void Shoot(){
            if (Highligher != null)
            {
                Highligher.SetActive(false);
            }
            //playerCollider.enabled = true;
            if (rb != null){
                rb.isKinematic = false;

                rb.AddForce(transform.right * (forceMultiplier * force), ForceMode2D.Impulse);
            }
            if (GameControllerOffline.Instance != null)
            {
                GameControllerOffline.Instance.Shoot();
            }
        }

        public void ResetPosition(){
            Highligher.SetActive(false);
            Highligher.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
            //Debug.Log($"With Bots {PhotonController.Instance.playWithBot} " +
            //    $"&& State {GameController.Instance.whichPlayer.ToString()}");
            transform.rotation = Quaternion.Euler(0, 0, 90);
            if (GameManager.Instance.currentGameMode== GameManager.GameMode.Ai)
            {
                if (GameControllerOffline.Instance.whichPlayer == GameControllerOffline.WhichPlayer.OTHER){
                    arrowLocalScale = Vector2.zero;
                    arrow.transform.localScale = Vector2.zero;
                    transform.localPosition = new Vector2(puckStartPos.x, -puckStartPos.y - 0.08f);
                } else {
                    transform.localPosition = new Vector2(puckStartPos.x, puckStartPos.y);
                }
                GameControllerOffline.Instance.strikerPositionAdjuster.SetPosition();

            }
            
            //playerCollider.enabled = true;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true;
            spriteRenderer.color = new Color32(255, 255, 255, 255);
            lastVel = Vector2.zero;
            isMoving = false;
        }

        public void ShootBot(){
            StartCoroutine(ShootBotCoroutine());
        }

        IEnumerator ShootBotCoroutine(){
            //playerCollider.enabled = false;
            arrowLocalScale = Vector2.zero;
            arrow.transform.localScale = Vector2.zero;
            transform.localPosition = new Vector2(puckStartPos.x, -puckStartPos.y-0.08f);
            //Debug.LogError("Striker ");
            GameControllerOffline.Instance.strikerPositionAdjuster.SetPosition();

            yield return new WaitForSeconds(0.5f);

            Vector2 newPos = transform.position;
            forceMultiplier = 0;
            arrow.SetActive(true);
            
            PuckOffline[] blackPucks = GameControllerOffline.Instance.allPucks.Where(x => x.CompareTag("Black") && !x.gameObject.GetComponent<PuckOffline>().puckInHole).ToArray();

            PuckOffline target = blackPucks[Random.Range(0, blackPucks.Length)];

            if ((blackPucks.Length==1 || Random.Range(0,1000)>600) && !GameControllerOffline.Instance.redPuckCollected && !GameControllerOffline.Instance.redPuckWaiting)
            {
                target = GameControllerOffline.Instance.redPuck.GetComponent<PuckOffline>();
            }


            if (target.transform.position.x > 0){
                newPos.x += Random.Range(0f, 1f);
            } else {
                newPos.x -= Random.Range(0f, 1f);
            }

            newPos.x = Mathf.Clamp(newPos.x, -1.25f, 1.25f);

            yield return new WaitForSeconds(Random.Range(1.5f, 4f));

            GameControllerOffline.Instance.OnDown();

            LeanTween.move(gameObject, newPos, 0.5f)
                .setIgnoreTimeScale(false)
                .setOnComplete(() =>
                {

                    GameControllerOffline.Instance.OnUp();
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
                        if (GameManager.Instance.currentGameMode.Equals(GameManager.GameMode.Ai))
                        {
                            Shoot();
                        }
                    });
                });
        }

        // public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info){
        //     if (stream.IsWriting){
        //         stream.SendNext(rb.linearVelocity);
        //         stream.SendNext(GameControllerOffline.Instance.playerSlider.value);
        //         stream.SendNext(arrow.activeInHierarchy);
        //         stream.SendNext(arrow.transform.localScale);
        //         stream.SendNext(arrow.transform.localRotation);
        //     }
        //     else{
        //         lastVel = (Vector2)stream.ReceiveNext();
        //         opponentSliderValue = (float)stream.ReceiveNext();
        //         arrow.SetActive((bool)stream.ReceiveNext());
        //         arrowLocalScale = (Vector3)stream.ReceiveNext();
        //         arrow.transform.localRotation = (Quaternion)stream.ReceiveNext();
        //     }
        // }

        void OnTriggerEnter2D(Collider2D col){
            if (col.CompareTag("Hole")){
                if (GameControllerOffline.Instance.gameState == GameControllerOffline.GameState.SWITCH_MASTER)
                    return;
                if (GameControllerOffline.Instance.whichPlayer.Equals(GameControllerOffline.WhichPlayer.ME))
                //if (true)
                {
                    //GameController.Instance.statusPanelText.text = "FOUL! Opponent Turn";
                    GameControllerOffline.Instance.statusPanelText.text = "Oops! Foul";
                    GameControllerOffline.Instance.StatusPanelActive();
                }
                //playerCollider.enabled = false;
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
                lastVel = Vector2.zero;
                isMoving = false;

                col.GetComponent<PuckHoleOffline>().PutStrikerOnPuckHole(gameObject, () =>
                {
                    PuckOnHole(gameObject.tag, gameObject.name);
                });
            }
        }

        //private void OnCollisionEnter2D(Collision2D col)
        //{
        //    if (col.gameObject.CompareTag("Red") || col.gameObject.CompareTag("White") || col.gameObject.CompareTag("Black") && GameController.Instance.isSliding)
        //    {
        //        warningStrikerPuck.SetActive(true);
        //    }
        //}

        //private void OnCollisionExit2D(Collision2D col)
        //{
        //    if (col.gameObject.CompareTag("Red") || col.gameObject.CompareTag("White") || col.gameObject.CompareTag("Black") && GameController.Instance.isSliding)
        //    {
        //        warningStrikerPuck.SetActive(false);
        //    }
        //}

        void PuckOnHole(string puckTag, string puckName){
            GameControllerOffline.Instance.PuckOnHole(puckTag, puckName);
        }


        void CheckOutOfBoundry()
        {
            Vector3 clampedPosition = transform.localPosition;
            clampedPosition.x = Mathf.Clamp(clampedPosition.x, XBoundry.x, XBoundry.y);
            clampedPosition.y = Mathf.Clamp(clampedPosition.y, YBoundry.x, YBoundry.y);
            transform.localPosition = clampedPosition;
        }



        // public void SendData(bool[] dataArray, bool IsOrigMaster)
        // {
        //     GetComponent<PhotonView>().RPC(nameof(RPCSendData),RpcTarget.Others, dataArray, IsOrigMaster);

        // }

        // [PunRPC]
        // void RPCSendData(bool[] dataArray, bool IsOrigMaster)
        // {
        //     GameControllerOffline.Instance.SyncData(dataArray,  IsOrigMaster);

        // }

        // public void BroadCastSliderUp()
        // {
        //     GetComponent<PhotonView>().RPC(nameof(RPC_SliderUp), RpcTarget.All);
        // }

        // [PunRPC]
        // void RPC_SliderUp()
        // {
        //     GameControllerOffline.Instance.HandleOnSliderPointerUp();
        // }

        // public void BroadCastSliderDown()
        // {
        //     GetComponent<PhotonView>().RPC(nameof(RPC_SliderDown), RpcTarget.All);
        // }

        // [PunRPC]
        // void RPC_SliderDown()
        // {
        //     GameControllerOffline.Instance.HandleOnSliderPointerDown();
        // }
        private bool checkVel;

        public void setVelCheck(bool state)
        {
            //Debug.Log($"Setting.... {state}");
            checkVel = state;
        }
    }
}

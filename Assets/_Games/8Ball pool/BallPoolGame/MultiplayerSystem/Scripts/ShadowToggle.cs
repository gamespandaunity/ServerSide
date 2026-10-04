using BallPool.Mechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace BallPool
{
    public class ShadowToggle : MonoBehaviour
    {
        [FormerlySerializedAs("ballShadow")] public List<GameObject> ballShadows = new List<GameObject>();
        [FormerlySerializedAs("manager2d")] public VisualModeManager visualModeManager2D;
        [FormerlySerializedAs("activeGb")] public List<GameObject> activeGameObjects = new List<GameObject>();
        [FormerlySerializedAs("rb")] List<Rigidbody> rigidbodies = new List<Rigidbody>();
        // Start is called before the first frame update
        private void OnEnable()
        {
            SetBallShadows();


        }
        private void Start()
        {
            foreach (BallDetector bl in GameManager.instance.physicsHandler.BallListeners)
            {
                rigidbodies.Add(bl.GetComponent<Rigidbody>());
            }
        }
        public void CacheActiveObjects()
        {
            for (int i = 0; i < ballShadows.Count; i++)
            {
                if (ballShadows[i].gameObject.activeSelf)
                {
                    activeGameObjects.Add(ballShadows[i]);
                }
            }
        }

        private void Update()
        {
            if (ShotController.shotControllerInstance.isBallInHand)
            {
                foreach (GameObject gb in ballShadows)
                {
                    if (gb != null) gb.SetActive(false);
                }
            }
            else
            {
                foreach (GameObject gb in ballShadows)
                {
                    if (gb != null) gb.SetActive(true);
                }
                // Never resurrect a POCKETED ball's shadow. The blanket SetActive(true) above
                // re-enabled the shadow that Ball.OnState(EnterInPocket) had just hidden — only
                // the reconnect restore ever removed a shadow from this list, so after a normal
                // pocket a "ghost" shadow could reappear on the table at the pocket mouth and
                // sit there until something else hid it again. Re-hide pocketed balls' shadows
                // AFTER the blanket enable so they can never ghost.
                var gmInst = GameManager.instance;
                if (gmInst != null && gmInst.allBalls != null)
                {
                    foreach (var ball in gmInst.allBalls)
                    {
                        if (ball != null && ball.inPocket && ball.ballShadow != null)
                            ball.ballShadow.gameObject.SetActive(false);
                    }
                }
            }

        }

        public void SetBallShadows()
        {


            ballShadows.Clear(); activeGameObjects.Clear();
            foreach (Transform t in visualModeManager2D.ballShadows)
            {
                if (t.gameObject.activeSelf)
                {
                    ballShadows.Add(t.gameObject);
                }
            }
            CacheActiveObjects();// safety purpose
        }

    }
}

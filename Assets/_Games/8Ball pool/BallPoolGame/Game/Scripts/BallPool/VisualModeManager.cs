using UnityEngine;
using NetworkManagement;
using BallPool.Mechanics;
using UnityEngine.Serialization;

namespace BallPool
{
    /// <summary>
    /// 3D and 2D mode manager.
    /// </summary>
    public class VisualModeManager : MonoBehaviour
    {
        
        [FormerlySerializedAs("shotController")] public ShotController shotController;
        [FormerlySerializedAs("cueBallTargetngImage")] public RectTransform cueBallTargetImage;
        [FormerlySerializedAs("twoDTable")] public GameObject table2D;
        [FormerlySerializedAs("ballsConteyner")] public Transform ballContainer;
        [FormerlySerializedAs("ballsShadow")] public Transform[] ballShadows;
        [FormerlySerializedAs("ballShadow")] public Transform singleBallShadow;
        [FormerlySerializedAs("ballBlick")] public Transform ballHighlight;
        [FormerlySerializedAs("balls3DMaterial")] public Material ballMaterial3D;
        [FormerlySerializedAs("balls2DMaterial")] public Material ballMaterial2D;
        [FormerlySerializedAs("ballChecker")] public MeshRenderer ballCheckerRenderer;
        [FormerlySerializedAs("ballChecker3DMaterial")] public Material checkerMaterial3D;
        [FormerlySerializedAs("ballChecker2DMaterial")] public Material checkerMaterial2D;
        [FormerlySerializedAs("hand")] public MeshRenderer handRenderer;
        [FormerlySerializedAs("hand3DMaterial")] public Material[] handMaterials3D;
        [FormerlySerializedAs("hand2DMaterial")] public Material[] handMaterials2D;
        [FormerlySerializedAs("cue3D")] public GameObject cue3DObject;
        [FormerlySerializedAs("cue2D")] public GameObject cue2DObject;
        [FormerlySerializedAs("hideOn2D")] public RectTransform[] hideWhileIn2D;
        [FormerlySerializedAs("hideOn3D")] public RectTransform[] hideWhileIn3D;

        private Transform[] ballTransforms;
        private Transform[] ballGlowEffects;

        void Awake()
        {
            if (!EightBallPoolNetworkManager.initialized)
            {
                return;
            }
            //threDTable.SetActive(AightBallPoolNetworkGameAdapter.is3DGraphics);
            table2D.SetActive(!AightBallPoolNetworkGameAdapter.is3DGraphics);
            ballTransforms = new Transform[ballContainer.childCount];
            ballShadows = new Transform[ballTransforms.Length];
            ballGlowEffects = new Transform[ballTransforms.Length];
            ballCheckerRenderer.sharedMaterial = AightBallPoolNetworkGameAdapter.is3DGraphics ? checkerMaterial3D : checkerMaterial2D;
            handRenderer.sharedMaterials = AightBallPoolNetworkGameAdapter.is3DGraphics ? handMaterials3D : handMaterials2D;
            cue3DObject.SetActive(AightBallPoolNetworkGameAdapter.is3DGraphics);
            cue2DObject.SetActive(!AightBallPoolNetworkGameAdapter.is3DGraphics);

            for (int i = 0; i < ballContainer.childCount; i++)
            {
                ballTransforms[i] = ballContainer.GetChild(i);
                ballTransforms[i].GetComponent<MeshRenderer>().sharedMaterial = AightBallPoolNetworkGameAdapter.is3DGraphics ? ballMaterial3D : ballMaterial2D;

                ballShadows[i] = Transform.Instantiate(singleBallShadow) as Transform;
                ballShadows[i].transform.parent = transform;
                ballShadows[i].transform.position = ballTransforms[i].position;

                Ball ball = ballTransforms[i].GetComponent<Ball>();
                ball.lightCentre = this.transform;
                ball.ballShadow = ballShadows[i];

                if (!AightBallPoolNetworkGameAdapter.is3DGraphics)
                {
                    ballGlowEffects[i] = Transform.Instantiate(ballHighlight) as Transform;
                    ballGlowEffects[i].transform.parent = transform;
                    ballGlowEffects[i].transform.position = ballTransforms[i].position;

                    ball.ballBlick = ballGlowEffects[i];
                }
            }
            foreach (var item in hideWhileIn3D)
            {
                item.gameObject.SetActive(!AightBallPoolNetworkGameAdapter.is3DGraphics);
            }
            foreach (var item in hideWhileIn2D)
            {
                item.gameObject.SetActive(AightBallPoolNetworkGameAdapter.is3DGraphics);
            }

            if (shotController.cueControlMode == ShotController.CueViewMode.ThirdPerson)
            {
                cueBallTargetImage.gameObject.SetActive(true);
            }

            Destroy(singleBallShadow.gameObject);
            Destroy(ballHighlight.gameObject);
            enabled = false;
        }
    }
}

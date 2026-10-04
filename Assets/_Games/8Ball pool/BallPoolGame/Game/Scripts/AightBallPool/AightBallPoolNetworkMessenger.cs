using System.Collections;
using UnityEngine;
using BallPool;

namespace NetworkManagement
{
    //public interface AightBallPoolMessenger
    //{
    //    void OnSendCueControl(float cuePivotLocalRotationY, float cueVerticalLocalRotationX, Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force);
    //}
    public class AightBallPoolNetworkMessenger : NetworkMessenger
    {
        private ShotController _shotController;
        public ShotController shotController
        {
            get
            {
                if (!_shotController)
                {
                    _shotController = ShotController.FindObjectOfType<ShotController>();
                }
                return _shotController;
            }
        }
        private GameManager _gameManager;
        public GameManager gameManager
        {
            get
            {
                if (!_gameManager)
                {
                    _gameManager = GameManager.FindObjectOfType<GameManager>();
                }
                return _gameManager;
            }
        }
        #region sended from network
        public void SetTime(float time01)
        {
            if (BallPoolGameLogic.controlFromNetwork)
            {
                BallPoolGameManager.instance.SetPlayTime(time01);
            }
        }
        public void SetOpponentCueURL(string url)
        {
            shotController.AssignOpponentCueURL(url);
        }
        public void SetOpponentTableURLs(string boardURL, string clothURL, string clothColor)
        {
            shotController.AssignOpponentTableURLs(boardURL, clothURL, clothColor);
        }

        public IEnumerator OnOpponenInGameScene()
        {
            while (!shotController)
            {
                yield return null;
            }
            shotController.OpponentReadyToPlay();
        }
        public void OnOpponentForceGoHome()
        {
            ConstantsData_M.LogInfo("OnOpponentForceGoHome");
            BallPoolGameManager.instance.OnForceGoHome(AightBallPoolPlayer.mainPlayer.playerId);
        }
        
        public void OnSendCueControl(float cuePivotLocalRotationY, float cueVerticalLocalRotationX, Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force)
        {
            if (shotController)
            {
                shotController.SyncCueControlFromNetwork(cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
            }
        }
        public void OnForceSendCueControl(float cuePivotLocalRotationY, float cueVerticalLocalRotationX, Vector2 cueDisplacementLocalPositionXY, float cueSliderLocalPositionZ, float force)
        {
            if (shotController)
            {
                shotController.ForceSyncCueControlFromNetwork(cuePivotLocalRotationY, cueVerticalLocalRotationX, cueDisplacementLocalPositionXY, cueSliderLocalPositionZ, force);
            }
        }
        public void OnMoveBall(Vector3 ballPosition)
        {
            if (shotController)
            {
                shotController.NetworkBallMovement(ballPosition);
            }
        }
        public void SelectBallPosition(Vector3 ballPosition)
        {
            shotController.GetBallPositionFromNetwork(ballPosition);
        }
        public void SetBallPosition(Vector3 ballPosition)
        {
            shotController.SetBallPositionViaNetwork(ballPosition);
        }
 
        public void SetMechanicalStatesFromNetwork(int ballId, string mechanicalStateData)
        {
            if (shotController != null && shotController.cuePhysicsManager != null
                && shotController.cuePhysicsManager.PlaybackActive)
            {
                gameManager.allBalls[ballId].EnqueuePlaybackKeyframe(mechanicalStateData);
            }
            else
            {
                StartCoroutine(gameManager.allBalls[ballId].SetMechanicalStatesFromNetwork(mechanicalStateData));
            }
        }
        // Dense position frame from the shooter (relayed by server). Buffer into each ball's playback
        // keyframes so the watcher interpolates the real path. Only while this client is the watcher.
        public void SetShotPlaybackFrameFromNetwork(int shotSequence, float time, int[] ballIds, Vector3[] positions, Vector3[] velocities, int[] pocketIds)
        {
            if (shotController == null || shotController.cuePhysicsManager == null
                || !shotController.cuePhysicsManager.PlaybackActive)
            {
                return;
            }
            if (ballIds == null || positions == null || velocities == null || pocketIds == null || gameManager == null)
            {
                return;
            }
            int count = Mathf.Min(Mathf.Min(ballIds.Length, positions.Length), Mathf.Min(velocities.Length, pocketIds.Length));
            // Watcher self-heal: a keyframe arrived — keep the starvation timer fresh so the local-physics
            // resume only fires when the shooter's stream has genuinely stopped (disconnect mid-shot).
            shotController.cuePhysicsManager.NotifyPlaybackFrameReceived();
            for (int i = 0; i < count; i++)
            {
                int id = ballIds[i];
                if (id >= 0 && id < gameManager.allBalls.Length && gameManager.allBalls[id] != null)
                {
                    gameManager.allBalls[id].EnqueuePlaybackKeyframe(time, positions[i], velocities[i], pocketIds[i]);
                }
            }
        }
        public void SetFinalShotPlaybackFrameFromNetwork(int shotSequence, float time, int[] ballIds, Vector3[] positions, Vector3[] velocities, int[] pocketIds)
        {
            if (shotController == null || shotController.cuePhysicsManager == null
                || !shotController.cuePhysicsManager.PlaybackActive)
            {
                return;
            }
            if (ballIds == null || positions == null || velocities == null || pocketIds == null || gameManager == null)
            {
                return;
            }
            int count = Mathf.Min(Mathf.Min(ballIds.Length, positions.Length), Mathf.Min(velocities.Length, pocketIds.Length));
            shotController.cuePhysicsManager.NotifyPlaybackFrameReceived();
            for (int i = 0; i < count; i++)
            {
                int id = ballIds[i];
                if (id >= 0 && id < gameManager.allBalls.Length && gameManager.allBalls[id] != null)
                {
                    gameManager.allBalls[id].EnqueueFinalPlaybackKeyframe(time, positions[i], velocities[i], pocketIds[i]);
                }
            }
        }
        public void WaitAndStopMoveFromNetwork(float time)
        {
            StartCoroutine(shotController.cuePhysicsManager.WaitForMoveStopFromNetwork(time));
        }
        public void StartSimulate(string impulse)
        {
            shotController.cuePhysicsManager.StartShotFromNetwork(impulse);
        }
        public void EndSimulate(string data)
        {
            shotController.cuePhysicsManager.VerifyShotEnd(data);
        }
        #endregion
    }
}

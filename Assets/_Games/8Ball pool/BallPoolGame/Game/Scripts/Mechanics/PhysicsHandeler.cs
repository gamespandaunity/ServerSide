using Mirror;
using NetworkManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BallPool.Mechanics
{
    public enum ShapeType
    {
        Non = 0,
        Ball,
        Board,
        Cloth,
    }

    public enum BallExitType
    {
        Sleep = 0,
        Reactivate}
    ;

    public enum BallState
    {
        Non = 0,
        SetState,
        StartMove,
        Move,
        HitBall,
        HitBoard,
        EnterInPocket,
        MoveInPocket,
        ExitFromPocket,
        EndMove}
    ;
    public delegate void BallShotHandler<String>(string impulse);
    public delegate void BallMoveHandler<Int,Vector3>(int ballId,Vector3 position,Vector3 velocity,Vector3 angularVelocity);
    public delegate void BallSleepHandler<Int,Vector3>(int ballId,Vector3 position);
    public delegate void SetStateHandler();
    public delegate void BallHitBallHandler<Ball,Boolean>(Ball ball,Ball hitBall,bool inMove);
    public delegate void BallHitBoardHandler<Ball,Boolean>(Ball ball,bool inMove);
    public delegate void BallHitPocketHandler<Ball,Pocket,Boolean>(Ball ball,Pocket pocket,bool inMove);
    public delegate void BallExitFromPocketHandler<Ball,Pocket,BallExitType,Boolean>(Ball ball,Pocket pocket,BallExitType exitType,bool inMove);

    public struct Impulse
    {
        public readonly Vector3 point;
        public readonly Vector3 impulse;

        public Impulse(Vector3 point, Vector3 impulse)
        {
            this.point = point;
            this.impulse = impulse;
        }
    }

    public struct HitInfo
    {
        public Vector3 point { get; private set; }

        public Vector3 normal { get; private set; }

        public Vector3 positionInHit { get; private set; }

        public ShapeType shapeType { get; private set; }

        public HitInfo(Vector3 point, Vector3 normal, Vector3 positionInHit, ShapeType shapeType)
        {
            this.point = point;
            this.normal = normal;
            this.positionInHit = positionInHit;
            this.shapeType = shapeType;
        }
    }

    public class PhysicsHandeler : MonoBehaviour
    {
        private const float NetworkStopSleepConfirmSeconds = 0.25f;
        private const float NetworkStopLocalSettleTimeout = 12.0f;
        private const float NetworkStopStartWaitTimeout = 2.0f;

        public event BallMoveHandler<int, Vector3> OnBallMotion;
        public event BallSleepHandler<int, Vector3> OnBallStopped;
        public event BallShotHandler<string> OnCueStrike;
        public event BallShotHandler<string> OnReplaySegmentSaved;
        public event BallShotHandler<string> OnShotComplete;
        public event SetStateHandler OnGameStateSet;
        public event BallHitBallHandler<BallDetector, bool> OnBallCollision;
        public event BallHitBoardHandler<BallDetector, bool> OnBallWallCollision;
        public event BallHitPocketHandler<BallDetector, PocketDetector, bool> OnBallPocketed;
        public event BallExitFromPocketHandler<BallDetector, PocketDetector, BallExitType, bool> OnBallEjectedFromPocket;

        public float shotDuration{ get; set; }

        [SerializeField] private float _massOfBall;
        [SerializeField] private float _maxBallVelocity;
        [SerializeField] private float _maxBallAngularVelocity;

        public float BallMass{ get { return _massOfBall; } }

        public float BallMaxVelocity{ get { return _maxBallVelocity; } }

        public float BallMaxAngularVelocity{ get { return _maxBallAngularVelocity; } }

        public Transform ClothSpace;
        public BallDetector[] BallListeners;
        public PocketDetector[] PocketListeners;
        public ReplayManager ReplayManager;
        private Impulse BallImpulse;

        public bool IsInMove{ get; private set; }
        public bool IsEndFromNetwork{ get; set; }

        public static bool UseNetworkPlayback = true;
        public static float PlaybackRenderDelay = 0.25f;
        public static float PlaybackSendInterval = 0.025f;





        public static bool UseVisualInterpolation = true;





        public static bool UsePostShotReconcileGlide = true;
        public static float PostShotReconcileGlideSeconds = 0.15f;
        private bool playbackActive;
        private float lastPlaybackFrameReceivedTime;
        private float playbackSendAccum;
        public bool PlaybackActive { get { return playbackActive && UseNetworkPlayback; } }

        private bool IsWatcherPlaybackContext()
        {
            if (!UseNetworkPlayback || !BallPoolGameLogic.isOnLine)
                return false;
            // Dedicated server drives its board from the shooter's frames EXACTLY like a watcher
            // (playback), instead of running its own independent PhysX sim.
            if (NetworkServer.active && !NetworkClient.active)
                return true;
            return BallPoolGameLogic.controlFromNetwork
                && NetworkClient.active
                && !NetworkServer.active;
        }

        private bool TryCapturePlaybackFrame(bool includePocketed, out int[] ballIds, out Vector3[] positions, out Vector3[] velocities, out int[] pocketIds)
        {
            ballIds = null;
            positions = null;
            velocities = null;
            pocketIds = null;
            if (BallPoolGameManager.instance == null || BallPoolGameManager.instance.balls == null) return false;

            Ball[] balls = BallPoolGameManager.instance.balls;
            List<int> ids = new List<int>(balls.Length);
            List<Vector3> pos = new List<Vector3>(balls.Length);
            List<Vector3> vel = new List<Vector3>(balls.Length);
            List<int> pockets = new List<int>(balls.Length);
            for (int i = 0; i < balls.Length; i++)
            {
                Ball b = balls[i];
                if (b == null || b.listener == null || b.listener.body == null) continue;
                if (!includePocketed && b.inPocket) continue;
                ids.Add(b.id);
                pos.Add(b.listener.body.position);
                vel.Add(b.listener.body.linearVelocity);
                pockets.Add(b.inPocket ? b.pocketId : -1);
            }
            if (ids.Count == 0) return false;
            ballIds = ids.ToArray();
            positions = pos.ToArray();
            velocities = vel.ToArray();
            pocketIds = pockets.ToArray();
            return true;
        }

        private void TrySendPlaybackFrame(bool force = false)
        {
            if (!UseNetworkPlayback) return;
            if (!BallPoolGameLogic.isOnLine || !BallPoolGameLogic.controlInNetwork) return;
            if (MyEightBallNetwork.Instance == null || !NetworkClient.active) return;

            playbackSendAccum += Time.fixedDeltaTime;
            if (!force && playbackSendAccum < PlaybackSendInterval) return;
            playbackSendAccum = 0f;

            int[] ids;
            Vector3[] pos;
            Vector3[] vel;
            int[] pockets;
            // Repeat pocket state so a dropped pocket event/final packet cannot leave a ghost ball
            // on the server. Receivers already ignore duplicate pots and never resurrect a ball.
            if (!TryCapturePlaybackFrame(true, out ids, out pos, out vel, out pockets)) return;
            MyEightBallNetwork.Instance.SetShotPlaybackFrameFromNetwork(MyEightBallNetwork.Instance.ActiveShotSequence,
                shotDuration, ids, pos, vel, pockets);
        }

        void Awake()
        {
            if (!EightBallPoolNetworkManager.initialized)
            {
                enabled = false;
                return;
            }
            Physics.simulationMode = SimulationMode.Script;
            IsInMove = false;

            Time.fixedDeltaTime = 0.005f;
            Physics.bounceThreshold = 0.01f;
            Physics.sleepThreshold = 0.01f;
            Physics.defaultContactOffset = 0.0005f;
            Physics.defaultSolverIterations = 1;
            Physics.defaultSolverVelocityIterations = 1;


            foreach (var listener in BallListeners)
            {
                listener.body.linearDamping = 0.35f;
                listener.body.angularDamping = 0.7f;
                listener.body.mass = _massOfBall;
                listener.body.maxDepenetrationVelocity = _maxBallVelocity;
                listener.body.maxAngularVelocity = _maxBallAngularVelocity;
                listener.body.Sleep();
            }
            ReplayManager = new ReplayManager();
            if (BallPoolGameLogic.playMode != PlayMode.Replay)
            {
                ReplayManager.DeleteReplayData();
            }
        }

        void FixedUpdate()
        {
            if (playbackActive && UseNetworkPlayback)
            {
                if (IsInMove && BallPoolGameManager.instance != null && BallPoolGameManager.instance.balls != null)
                {
                    float playbackTime = shotDuration - PlaybackRenderDelay;
                    foreach (Ball playbackBall in BallPoolGameManager.instance.balls)
                    {
                        if (playbackBall != null)
                        {
                            playbackBall.DrivePlayback(playbackTime);
                        }
                    }
                }
                return;
            }
            if (IsInMove)
            {
                Physics.Simulate(Time.fixedDeltaTime);
                TrySendPlaybackFrame();
            }
            else
            {
                Physics.Simulate(Time.fixedDeltaTime);
            }
        }

        IEnumerator Start()
        {
            while (true)
            {
                yield return new WaitForFixedUpdate();
                if (IsInMove)
                {
                    shotDuration += Time.fixedDeltaTime;
                    if (!BallPoolGameLogic.controlFromNetwork)
                    {
                        if (IsSleeping(false))
                        {





                            // Online uses the tightened 0.2s sleep-confirm window; offline/AI keeps
                            // the original 1s so slow-rolling / pocket-lip balls are not force-slept
                            // early and shot outcomes match the pre-overhaul behaviour.
                            yield return new WaitForSeconds(BallPoolGameLogic.isOnLine ? 0.2f : 1.0f);

                            if (IsSleeping(true))
                            {
                                yield return StartCoroutine("EndMove");
                            }
                            else
                            {
                                StopCoroutine("EndMove");
                            }
                        }
                    }
                }
            }
        }
        public IEnumerator WaitForMoveStopFromNetwork(float time)
        {
            if (!IsInMove)
            {
                float startDeadline = Time.realtimeSinceStartup + NetworkStopStartWaitTimeout;
                while (!IsInMove && Time.realtimeSinceStartup < startDeadline)
                {
                    yield return new WaitForFixedUpdate();
                }

                if (!IsInMove)
                {
                    if (BallPoolGameLogic.isOnLine && BallPoolGameLogic.controlFromNetwork && Mirror.NetworkServer.active)
                    {
                        Debug.LogWarning("[8Ball][NetworkReplay] Completing network shot stop before local move started.");
                        yield return StartCoroutine(EndMove());
                    }
                    IsEndFromNetwork = true;
                    yield break;
                }
            }

            while (IsInMove && shotDuration < time)
            {
                yield return new WaitForFixedUpdate();
            }

            if (!IsInMove)
            {
                IsEndFromNetwork = true;
                yield break;
            }

            float settleDeadline = Time.realtimeSinceStartup + NetworkStopLocalSettleTimeout;
            while (IsInMove && Time.realtimeSinceStartup < settleDeadline)
            {
                if (!IsSleeping(false))
                {
                    yield return new WaitForFixedUpdate();
                    continue;
                }

                yield return new WaitForSeconds(NetworkStopSleepConfirmSeconds);
                if (IsSleeping(true))
                    break;
            }

            if (IsInMove && !IsSleeping(false))
            {
                Debug.LogWarning("[8Ball][NetworkReplay] Forcing network shot stop after local settle timeout.");
            }

            if (!IsInMove)
            {
                IsEndFromNetwork = true;
                yield break;
            }

            if (BallPoolGameLogic.isOnLine && BallPoolGameLogic.controlFromNetwork
                && BallPoolGameManager.instance != null && BallPoolGameManager.instance.balls != null)
            {
                foreach (Ball networkBall in BallPoolGameManager.instance.balls)
                {
                    if (networkBall != null)
                    {
                        networkBall.FlushFinalNetworkState(time);
                    }
                }
            }

            yield return StartCoroutine(EndMove());
            IsEndFromNetwork = true;
        }
        private IEnumerator EndMove()
        {
            IsInMove = false;
            foreach (BallDetector ball in BallListeners)
            {
                if (!ball.body.isKinematic)
                {
                    ball.body.linearVelocity = Vector3.zero;
                    ball.body.angularVelocity = Vector3.zero;
                    ball.body.Sleep();
                }

                TriggerBallSleep(ball.id, ball.body.position);
            }


            if (BallPoolGameLogic.controlInNetwork)
            {
                TrySendPlaybackFrame(true);
                int[] finalBallIds = null;
                Vector3[] finalPositions = null;
                Vector3[] finalVelocities = null;
                int[] finalPocketIds = null;
                bool hasFinalFrame = false;
                if (UseNetworkPlayback)
                    hasFinalFrame = TryCapturePlaybackFrame(true, out finalBallIds, out finalPositions, out finalVelocities, out finalPocketIds);
                yield return new WaitForSeconds(0.05f);
                if (MyEightBallNetwork.Instance != null && Mirror.NetworkClient.active)
                {
                    if (hasFinalFrame)
                    {
                        MyEightBallNetwork.Instance.CmdSubmitEightBallShotFinalFrameV2(shotDuration,
                            MyEightBallNetwork.Instance.ActiveShotSequence, finalBallIds, finalPositions, finalVelocities, finalPocketIds);
                    }
                    else
                    {
                        MyEightBallNetwork.Instance.WaitAndStopMoveFromNetwork(shotDuration);
                    }
                }
            }
            if (BallPoolGameLogic.playMode != PlayMode.Replay)
            {
                ReplayManager.AddReplayDataCount();
            }
            EndPlaybackBeforeShotComplete();
            if (OnShotComplete != null)
            {
                OnShotComplete("");
            }
            shotDuration = 0.0f;
        }

        private void EndPlaybackBeforeShotComplete()
        {
            if (!playbackActive)
                return;

            if (BallPoolGameManager.instance != null && BallPoolGameManager.instance.balls != null)
            {
                foreach (Ball playbackBall in BallPoolGameManager.instance.balls)
                {
                    if (playbackBall != null)
                    {
                        playbackBall.EndPlayback();
                    }
                }
            }
            playbackActive = false;
        }







        public void StopPlaybackForReconcile()
        {
            if (BallPoolGameManager.instance != null && BallPoolGameManager.instance.balls != null)
            {
                foreach (Ball b in BallPoolGameManager.instance.balls)
                {
                    if (b == null) continue;
                    b.EndPlayback();
                    if (b.listener != null && b.listener.body != null && !b.listener.body.isKinematic)
                    {
                        b.listener.body.linearVelocity = Vector3.zero;
                        b.listener.body.angularVelocity = Vector3.zero;
                        b.listener.body.Sleep();
                    }
                }
            }
            playbackActive = false;
            IsInMove = false;
            IsEndFromNetwork = true;
            shotDuration = 0.0f;
        }

        bool IsSleeping(bool forceSleep)
        {
            float minEnergy = 0.1f;
            bool isSleep = true;
            foreach (BallDetector ball in BallListeners)
            {

                bool ballIsSleeping = (!BallGeometry.SphereInCube(ball.body.position, ball.Radius, ClothSpace) || ball.body.isKinematic || (ball.body.linearVelocity.magnitude < minEnergy && ball.Radius * ball.body.angularVelocity.magnitude < minEnergy));
                if (!ballIsSleeping)
                {
                    isSleep = false;
                }
            }
            return isSleep;
        }

        public void TriggerBallSleep(int ballId, Vector3 position)
        {
            if (OnBallStopped != null)
            {
                OnBallStopped(ballId, position);
            }
        }

        public void TriggerBallMove(int ballId, Vector3 position, Vector3 velocity, Vector3 angularVelocity)
        {
            if (OnBallMotion != null)
            {
                OnBallMotion(ballId, position, velocity, angularVelocity);
            }
        }

        public void TriggerBallCollision(BallDetector ball, BallDetector hitBall, bool inReplay)
        {
            if (OnBallCollision != null)
            {
                OnBallCollision(ball, hitBall, inReplay);
            }
        }

        public void TriggerBallWallHit(BallDetector ball, bool inReplay)
        {
            if (OnBallWallCollision != null)
            {
                OnBallWallCollision(ball, inReplay);
            }
        }

        public void TriggerBallPocketed(BallDetector ball, PocketDetector pocket, bool inReplay)
        {
            if (OnBallPocketed != null)
            {
                OnBallPocketed(ball, pocket, inReplay);
            }
        }

        public void TriggerBallEjectionFromPocket(BallDetector ball, PocketDetector pocket, bool inReplay)
        {
            if (ball == null || pocket == null)
            {
                Debug.LogWarning("[8Ball][PhysicsHandeler] Ignored pocket ejection with missing ball/pocket reference.");
                return;
            }
            if (OnBallEjectedFromPocket != null)
            {
                OnBallEjectedFromPocket(ball, pocket, BallExitType.Reactivate, inReplay);
            }
        }

        public void RepositionBallInCube(float ballRadius, Transform cube, int clothMask, int ballsMask, ref bool canReactivate, ref Vector3 ballNewPosition)
        {
            RaycastHit clothHit;
            Vector3 origin = cube.position + 0.5f * cube.lossyScale.y * cube.up;
            Vector3 direction = -cube.up;
            canReactivate = false;
            ballNewPosition = cube.position;

            for (float x = 0.0f; x < 0.5f * cube.lossyScale.x - 3.0f * ballRadius; x += 3.0f * ballRadius)
            {
                for (float z = 0.0f; z < 0.5f * cube.lossyScale.z - 3.0f * ballRadius; z += 3.0f * ballRadius)
                {
                    origin = cube.position + 0.5f * cube.lossyScale.y * cube.up + x * cube.right + z * cube.forward;

                    if (Physics.Raycast(origin, direction, out clothHit, cube.lossyScale.y, clothMask))
                    {
                        RaycastHit ballHit;
                        if (!Physics.SphereCast(origin, ballRadius, direction, out ballHit, cube.lossyScale.y, ballsMask))
                        {
                            canReactivate = true;
                            ballNewPosition = clothHit.point + ballRadius * clothHit.normal;
                            break;
                        }
                    }
                }
                if (canReactivate)
                {
                    break;
                }
            }
        }

        public void HideBallTrajectory()
        {

        }

        public void ApplyImpulse(Impulse impulse)
        {
            this.BallImpulse = impulse;
        }

        public void StartShotFromNetwork(string impulse)
        {
            IsEndFromNetwork = false;
            playbackActive = IsWatcherPlaybackContext();
            lastPlaybackFrameReceivedTime = playbackActive ? Time.time : 0f;
            if (playbackActive)
            {
                if (BallPoolGameManager.instance != null && BallPoolGameManager.instance.balls != null)
                {
                    foreach (Ball playbackBall in BallPoolGameManager.instance.balls)
                    {
                        if (playbackBall != null)
                        {
                            playbackBall.BeginPlayback();
                        }
                    }
                }
                Debug.Log("[8Ball][Playback] watcher interpolated playback started");
            }
            StartReplayShot(impulse);
        }



        public void NotifyPlaybackFrameReceived()
        {
            lastPlaybackFrameReceivedTime = Time.time;
        }

        public void StartReplayShot(string impulse)
        {
            Debug.Log("StartRaplayShot");
            if (OnReplaySegmentSaved != null)
            {
                OnReplaySegmentSaved(impulse);
            }
        }

        public void InitiateShot(BallDetector ball)
        {
            IsEndFromNetwork = false;
            IsInMove = true;
            shotDuration = 0.0f;
            playbackSendAccum = 0f;
            if (playbackActive)
                lastPlaybackFrameReceivedTime = Time.time;

            if (BallPoolGameLogic.isOnLine && BallPoolGameManager.instance != null
                && BallPoolGameManager.instance.balls != null)
            {
                foreach (Ball networkBall in BallPoolGameManager.instance.balls)
                {
                    if (networkBall != null)
                    {
                        networkBall.ResetPendingNetworkState();
                    }
                }
            }

            if (!(playbackActive && UseNetworkPlayback))
            {
                ball.body.AddForceAtPosition(BallImpulse.impulse, BallImpulse.point, ForceMode.Impulse);
            }

            if (OnCueStrike != null)
            {
                OnCueStrike("");
            }
        }

        public void VerifyShotEnd(string data)
        {

        }

        public void UpdateState(int state)
        {

        }

        public void Deactivate()
        {

        }
    }
}

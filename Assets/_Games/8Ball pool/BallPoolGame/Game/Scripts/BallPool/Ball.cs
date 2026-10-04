using NetworkManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BallPool.Mechanics
{
    public class Ball : MonoBehaviour
    {



        public struct MechanicalState
        {
            public readonly float time;
            public readonly int pocketId;
            public readonly int hitShapeId;
            public readonly Vector3 position;
            public readonly Vector3 velocity;
            public readonly Vector3 angularVelocity;

            public MechanicalState(float time, int pocketId, int hitShapeId, Vector3 position, Vector3 velocity, Vector3 angularVelocity)
            {
                this.time = time;
                this.pocketId = pocketId;
                this.hitShapeId = hitShapeId;
                this.position = position;
                this.velocity = velocity;
                this.angularVelocity = angularVelocity;
            }
            public static string StateToString(MechanicalState state)
            {
                return "[" + state.time.ToString4() + ";" + state.pocketId + ";" + state.hitShapeId + ";" + DataManager.Vector3ToString(state.position) + "; " +  DataManager.Vector3ToString(state.velocity) + "; " +  DataManager.Vector3ToString(state.angularVelocity) + "]";
            }
            public static MechanicalState StateFromString (string state)
            {
                if (state == "")
                {
                    return new MechanicalState();
                }
                string[] values = DataManager.ConvertDataToStringArray(state);
                float time = values[0].ToFloat4();
                int pocketId = int.Parse(values[1], System.Globalization.NumberStyles.Integer);
                int hitShapeId = int.Parse(values[2], System.Globalization.NumberStyles.Integer);
                Vector3 position = DataManager.Vector3FromString(values[3]);
                Vector3 velocity = DataManager.Vector3FromString(values[4]);
                Vector3 angularVelocity = DataManager.Vector3FromString(values[5]);

                return new MechanicalState(time, pocketId, hitShapeId, position, velocity, angularVelocity);
            }
        }
        [System.NonSerialized] public Transform lightCentre;
       public Transform ballShadow;
        [System.NonSerialized] public Transform ballBlick;
        private MeshRenderer ballRenderer;
        [SerializeField] private AudioClip ballHitBallClip;
        [SerializeField] private AudioClip ballHitPocketClip;
        [SerializeField] private AudioClip ballHitBoardClip;
        public BallDetector listener;

        private AudioSource ballHitBall;
        private AudioSource ballHitBoard;
        private AudioSource ballHitPocket;
        private static int hitBallClipPlayingCount;
        private static int hitBoardClipPlayingCount;

        public int id;

        public float radius{ get; private set; }

        public bool isActive{ get; set; }

        public bool inPocket{ get{ return listener.body.isKinematic;} set{ listener.body.isKinematic = value;}}

        public int pocketId{ get{ return listener.PocketID; } set{ listener.PocketID = value; } }

        public int hitShapeId{ get{ return listener.HitShapeID; } set{ listener.HitShapeID = value; } }

        public bool inSpace{ get{ return BallGeometry.SphereInCube(position, radius, listener.physicsManager.ClothSpace); } }

        public Impulse impulse { get; set; }

        public bool isCast{ get{ return listener.GetComponent<SphereCollider>().enabled;} set{ listener.GetComponent<SphereCollider>().enabled = value; }}


        public Vector3 position{ get { return listener.body.position; } set { transform.position = value; listener.body.position = value; } }

        public HitInfo firstHitInfo{ get; internal set; }

        private float savedTime = -1.0f;
        private Vector3 savedPosition;
        private Vector3 savedVelocity;
        private Vector3 savedlarAnguVelocity;
        public Vector3 savedSleepPosition{ get; set; }





        private string lastNetState;
        private float lastNetStateTime = -1.0f;
        private bool lastNetStateApplied = true;

        private bool NeedToSave()
        {
            return false;











        }

        public IEnumerator WaitAndStopBall(float moveTime)
        {
            while (listener.physicsManager.shotDuration < moveTime)
            {
                yield return null;
            }
            if (!listener.body.isKinematic)
            {
                position = savedSleepPosition;
                listener.body.linearVelocity = Vector3.zero;
                listener.body.angularVelocity = Vector3.zero;
                listener.body.Sleep();
            }
        }

        public string mechanicalStateData
        {
            get
            {
                MechanicalState state = new MechanicalState(DataManager.CutValue(listener.physicsManager.shotDuration), pocketId, hitShapeId, position, listener.body.linearVelocity, listener.body.angularVelocity);
                return  MechanicalState.StateToString(state);
            }
            set
            {
                MechanicalState state = MechanicalState.StateFromString(value);
                inPocket = state.pocketId != -1;
                pocketId = state.pocketId;
                position = state.position;
                if (!inPocket)
                {
                    listener.body.linearVelocity = state.velocity;
                    listener.body.angularVelocity = state.angularVelocity;
                }
                if (BallPoolGameLogic.playMode == PlayMode.Replay)
                {
                    OnState(BallState.SetState);
                }
            }
        }
        public string moveData;

        void Awake ()
        {
            if (!EightBallPoolNetworkManager.initialized)
            {
                enabled = false;
                return;
            }

            hitBallClipPlayingCount = 0;
            hitBoardClipPlayingCount = 0;

            ballHitBall = gameObject.AddComponent<AudioSource>();
            ballHitBall.playOnAwake = false;
            ballHitBall.clip = ballHitBallClip;

            ballHitBoard = gameObject.AddComponent<AudioSource>();
            ballHitBoard.playOnAwake = false;
            ballHitBoard.clip = ballHitBoardClip;

            ballHitPocket = gameObject.AddComponent<AudioSource>();
            ballHitPocket.playOnAwake = false;
            ballHitPocket.clip = ballHitPocketClip;

            radius = listener.body.GetComponent<SphereCollider>().radius;
            listener.body.position = transform.position;
            ballRenderer = GetComponentInChildren<MeshRenderer>();
        }
        void Start ()
        {
            if (!AightBallPoolNetworkGameAdapter.is3DGraphics)
            {
                ballBlick.position = CalculateBallBlickPosition();
            }
            ballShadow.position = CalculateBallShadowPosition();

        }

        void Update()
        {
            if (!listener.physicsManager.IsInMove || inPocket || listener.body.isKinematic)
            {




                ballShadow.gameObject.SetActive(!inPocket && position.y > -0.05f);






                if (!AightBallPoolNetworkGameAdapter.is3DGraphics && ballBlick != null)
                    ballBlick.gameObject.SetActive(!inPocket);







                if (ballRenderer != null)
                {
                    bool hideMesh = inPocket && BallSpawner.HasTrayBall(id);
                    if (ballRenderer.enabled == hideMesh)
                        ballRenderer.enabled = !hideMesh;
                }

                return;
            }

            SetBallShadowAndBlickBlick();
        }
        public void SetBallShadowAndBlickBlick()
        {
            if (!AightBallPoolNetworkGameAdapter.is3DGraphics)
            {
                ballBlick.position = CalculateBallBlickPosition();
            }
            ballShadow.position = CalculateBallShadowPosition();
        }
        public void SetMechanicalState(int number)
        {
            listener.pocket = null;
            moveData = listener.physicsManager.ReplayManager.GetReplay(id, number);

            string[] states = DataManager.ConvertArrayDataToStringArray(moveData);
            if (states != null && states.Length > 0)
            {
                this.mechanicalStateData = states[0];
            }
        }
        Vector3 CalculateBallShadowPosition()
        {
            Vector3 positionInCloth = new Vector3(position.x, 0.1f * radius, position.z);
            return positionInCloth + 0.5f * radius * (position - lightCentre.position);
        }
        Vector3 CalculateBallBlickPosition()
        {
            return position + 1.1f * radius * Vector3.up;
        }

        private string[] mechanicalStates;
        private  float currentTime = 0.0f;
        private float deltaTime = 0.0f;
        private bool isFollow = false;
        private int mechanicalStateId = 0;
        private int oldMechanicalStateId = -1;
        private  MechanicalState currentState;

        public void SrartFollow()
        {
            if (string.IsNullOrEmpty(moveData))
            {
                return;
            }
            mechanicalStates = DataManager.ConvertArrayDataToStringArray(moveData);
            currentTime = 0.0f;
            oldMechanicalStateId = -1;
            mechanicalStateId = 0;
            deltaTime = 0.0f;
            currentState =  MechanicalState.StateFromString(mechanicalStates[mechanicalStateId]);
            if (currentState.pocketId != -1)
            {
                return;
            }
            isFollow = true;
        }

        public IEnumerator SetMechanicalStatesFromNetwork(string state)
        {
            if (playbackSuppressedUntilNextBegin)
            {
                yield break;
            }

            MechanicalState mState = MechanicalState.StateFromString(state);


            if (mState.time >= lastNetStateTime)
            {
                lastNetState = state;
                lastNetStateTime = mState.time;
                lastNetStateApplied = false;
            }

            while (!listener.physicsManager.IsEndFromNetwork && listener.physicsManager.shotDuration < mState.time)
            {
                yield return new WaitForFixedUpdate();
            }
            if (playbackSuppressedUntilNextBegin)
            {
                yield break;
            }
            if (!listener.physicsManager.IsEndFromNetwork)
            {
                FollowMoveFromNetwork(mState);
                if (ShouldDeferNetworkSnapWhileFreeSimulating(mState))
                {
                    yield break;
                }
                mechanicalStateData = state;
                if (ReferenceEquals(state, lastNetState))
                {
                    lastNetStateApplied = true;
                }
            }
        }

        private bool ShouldDeferNetworkSnapWhileFreeSimulating(MechanicalState state)
        {
            return BallPoolGameLogic.isOnLine
                && BallPoolGameLogic.controlFromNetwork
                && listener != null
                && listener.physicsManager != null
                && listener.physicsManager.IsInMove
                && !listener.physicsManager.PlaybackActive
                && state.pocketId == -1;
        }







        public void FlushFinalNetworkState(float stopTime)
        {
            if (playbackSuppressedUntilNextBegin)
            {
                return;
            }
            if (lastNetStateApplied || lastNetState == null)
            {
                return;
            }
            if (lastNetStateTime > stopTime + Time.fixedDeltaTime)
            {
                return;
            }
            mechanicalStateData = lastNetState;
            lastNetStateApplied = true;
        }

        public void ResetPendingNetworkState()
        {
            lastNetState = null;
            lastNetStateTime = -1.0f;
            lastNetStateApplied = true;
        }







        private readonly List<MechanicalState> playbackKeys = new List<MechanicalState>();
        private readonly List<bool> playbackEventFired = new List<bool>();
        private bool playbackPocketed;
        private bool playbackSuppressedUntilNextBegin;
        private Vector3 playbackPos;
        private Vector3 playbackVel;
        private float playbackLastT;
        private int playbackConsumed;
        private const float MaxPlaybackExtrapolationSeconds = 0.15f;

        public void BeginPlayback()
        {
            playbackSuppressedUntilNextBegin = false;
            playbackKeys.Clear();
            playbackEventFired.Clear();
            playbackPocketed = false;
            playbackPos = position;
            playbackVel = Vector3.zero;
            playbackLastT = 0f;
            playbackConsumed = 0;
            if (listener != null && listener.body != null && !listener.body.isKinematic)
            {
                listener.body.linearVelocity = Vector3.zero;
                listener.body.angularVelocity = Vector3.zero;
                listener.body.Sleep();
            }

            playbackKeys.Add(new MechanicalState(0f, -1, -2, position, Vector3.zero, Vector3.zero));
            playbackEventFired.Add(true);
            RefreshPlaybackVisuals();
        }

        public void EnqueuePlaybackKeyframe(string state)
        {
            if (playbackSuppressedUntilNextBegin)
            {
                return;
            }
            MechanicalState k = MechanicalState.StateFromString(state);
            int idx = playbackKeys.Count;
            while (idx > 0 && playbackKeys[idx - 1].time > k.time)
            {
                idx--;
            }
            if (idx <= playbackConsumed)
            {
                if (k.pocketId != -1)
                {
                    ApplyPlaybackPocketKeyframe(k);
                }
                return;
            }
            playbackKeys.Insert(idx, k);
            playbackEventFired.Insert(idx, false);
        }




        public void EnqueuePlaybackKeyframe(float time, Vector3 pos, Vector3 vel, int pocketId)
        {
            if (playbackSuppressedUntilNextBegin)
            {
                return;
            }
            if (time <= playbackLastT + 0.001f && pocketId == -1)
            {
                return;
            }
            MechanicalState k = new MechanicalState(time, pocketId, -2, pos, vel, Vector3.zero);
            int idx = playbackKeys.Count;
            while (idx > 0 && playbackKeys[idx - 1].time > k.time)
            {
                idx--;
            }
            if (idx <= playbackConsumed)
            {
                if (k.pocketId != -1)
                {
                    ApplyPlaybackPocketKeyframe(k);
                }
                return;
            }
            playbackKeys.Insert(idx, k);
            playbackEventFired.Insert(idx, pocketId == -1);
        }

        public void EnqueueFinalPlaybackKeyframe(float time, Vector3 pos, Vector3 vel, int pocketId)
        {
            if (playbackSuppressedUntilNextBegin)
            {
                return;
            }
            float finalTime = time;
            if (playbackKeys.Count > 0)
                finalTime = Mathf.Max(finalTime, playbackKeys[playbackKeys.Count - 1].time + 0.002f);
            finalTime = Mathf.Max(finalTime, playbackLastT + 0.002f);

            MechanicalState k = new MechanicalState(finalTime, pocketId, -2, pos, vel, Vector3.zero);
            playbackKeys.Add(k);
            playbackEventFired.Add(pocketId == -1);
        }

        private bool IsValidPocketId(int id)
        {
            return listener != null
                && listener.physicsManager != null
                && listener.physicsManager.PocketListeners != null
                && id >= 0
                && id < listener.physicsManager.PocketListeners.Length
                && listener.physicsManager.PocketListeners[id] != null;
        }

        private bool ApplyPlaybackPocketKeyframe(MechanicalState state)
        {
            if (playbackSuppressedUntilNextBegin)
            {
                return false;
            }
            if (state.pocketId == -1)
            {
                return false;
            }

            playbackPos = state.position;
            playbackVel = Vector3.zero;
            playbackLastT = Mathf.Max(playbackLastT, state.time);
            position = state.position;

            if (!inPocket && IsValidPocketId(state.pocketId))
            {
                FollowMoveFromNetwork(state);
            }

            mechanicalStateData = MechanicalState.StateToString(state);
            playbackPocketed = true;

            if (listener != null && listener.body != null && !listener.body.isKinematic)
            {
                listener.body.linearVelocity = Vector3.zero;
                listener.body.angularVelocity = Vector3.zero;
                listener.body.Sleep();
            }
            if (ballShadow != null)
                ballShadow.gameObject.SetActive(false);
            if (!AightBallPoolNetworkGameAdapter.is3DGraphics && ballBlick != null)
                ballBlick.gameObject.SetActive(false);
            if (ballRenderer != null)
                ballRenderer.enabled = false;
            if (id != 0)
                BallSpawner.EnqueueTrayBall(id);
            return true;
        }

        public void DrivePlayback(float t)
        {
            if (playbackSuppressedUntilNextBegin || playbackPocketed || playbackKeys.Count == 0)
            {
                return;
            }
            if (t < playbackLastT)
            {
                t = playbackLastT;
            }



            while (playbackConsumed + 1 < playbackKeys.Count && playbackKeys[playbackConsumed + 1].time <= t)
            {
                playbackConsumed++;
                MechanicalState reached = playbackKeys[playbackConsumed];
                playbackPos = reached.position;
                playbackVel = reached.velocity;
                playbackLastT = reached.time;
                if (reached.pocketId != -1)
                {
                    ApplyPlaybackPocketKeyframe(reached);
                    return;
                }
                if (StopPlaybackIfObjectBallHasPocketed(playbackPos))
                {
                    return;
                }
                if (!playbackEventFired[playbackConsumed])
                {
                    playbackEventFired[playbackConsumed] = true;
                    FollowMoveFromNetwork(reached);
                }
            }

            if (playbackConsumed + 1 < playbackKeys.Count)
            {

                MechanicalState a = playbackKeys[playbackConsumed];
                MechanicalState b = playbackKeys[playbackConsumed + 1];
                float span = b.time - a.time;
                float f = span > 0.0001f ? Mathf.Clamp01((t - a.time) / span) : 1f;
                playbackPos = Vector3.Lerp(a.position, b.position, f);
                playbackLastT = t;
            }
            else
            {



                float dt = t - playbackLastT;
                if (dt > 0f)
                {


                    float age = t - playbackKeys[playbackConsumed].time;
                    if (age > MaxPlaybackExtrapolationSeconds)
                    {

                        playbackVel = Vector3.zero;
                    }
                    else
                    {
                        Vector3 next = playbackPos;
                        next.x += playbackVel.x * dt;
                        next.z += playbackVel.z * dt;
                        Transform cloth = listener.physicsManager.ClothSpace;
                        if (cloth != null && !BallGeometry.SphereInCube(next, radius, cloth))
                        {

                            next = BallGeometry.ClampPositionInCube(next, radius, cloth);
                            playbackVel = Vector3.zero;
                        }
                        playbackPos = next;
                    }
                    float drag = listener.body.linearDamping;
                    playbackVel *= Mathf.Clamp01(1f - drag * dt);
                    playbackLastT = t;
                }
            }
            Vector3 prevPos = position;
            if (StopPlaybackIfObjectBallHasPocketed(playbackPos))
            {
                return;
            }
            position = playbackPos;





            Vector3 disp = playbackPos - prevPos;
            disp.y = 0f;
            float dist = disp.magnitude;
            if (dist > 1e-5f && dist < 0.5f && radius > 1e-4f)
            {
                Vector3 axis = Vector3.Cross(Vector3.up, disp);
                if (axis.sqrMagnitude > 1e-8f)
                {
                    float angleDeg = (dist / radius) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.AngleAxis(angleDeg, axis.normalized) * transform.rotation;
                    if (listener != null && listener.body != null)
                        listener.body.rotation = transform.rotation;
                }
            }
            RefreshPlaybackVisuals();
        }

        public void EndPlayback()
        {
            if (!playbackSuppressedUntilNextBegin && !playbackPocketed && playbackKeys.Count > 0)
            {
                position = playbackKeys[playbackKeys.Count - 1].position;
            }
            playbackKeys.Clear();
            playbackEventFired.Clear();
            playbackPocketed = false;
            RefreshPlaybackVisuals();
        }

        public void SuppressPlaybackForAuthoritativeCueRespot(Vector3 authoritativePosition)
        {
            if (id != 0 || !BallPoolGameLogic.isOnLine)
            {
                return;
            }

            // The respot can arrive while this shot's delayed watcher/server playback is still
            // draining. From here until the next BeginPlayback, the authoritative ball-in-hand
            // position must win over every queued or late frame from the completed shot.
            playbackSuppressedUntilNextBegin = true;
            playbackKeys.Clear();
            playbackEventFired.Clear();
            playbackPocketed = false;
            playbackPos = authoritativePosition;
            playbackVel = Vector3.zero;
            playbackLastT = 0f;
            playbackConsumed = 0;
            ResetPendingNetworkState();
            isFollow = false;
        }

        private void RefreshPlaybackVisuals()
        {
            bool visible = !inPocket;
            if (!AightBallPoolNetworkGameAdapter.is3DGraphics && ballBlick != null)
            {
                ballBlick.gameObject.SetActive(visible);
                if (visible)
                {
                    ballBlick.position = CalculateBallBlickPosition();
                }
            }
            if (ballShadow != null)
            {
                ballShadow.gameObject.SetActive(visible);
                if (visible)
                {
                    ballShadow.position = CalculateBallShadowPosition();
                }
            }
        }

        private bool StopPlaybackIfObjectBallHasPocketed(Vector3 framePosition)
        {
            // Position-only shooter playback can continue to report an object ball after it has
            // dropped below the cloth. Do not render those below-table/tray positions as the table
            // ball flying from the pocket to the tray. Cue-ball placement smoothing lives in
            // ShotController and is intentionally excluded here.
            if (id == 0 || framePosition.y >= -0.05f || inPocket)
            {
                return false;
            }

            playbackPocketed = true;
            playbackVel = Vector3.zero;
            inPocket = true;
            position = framePosition;
            if (listener != null && listener.body != null)
            {
                listener.body.linearVelocity = Vector3.zero;
                listener.body.angularVelocity = Vector3.zero;
                listener.body.Sleep();
            }
            if (ballShadow != null)
                ballShadow.gameObject.SetActive(false);
            if (!AightBallPoolNetworkGameAdapter.is3DGraphics && ballBlick != null)
                ballBlick.gameObject.SetActive(false);
            if (ballRenderer != null)
                ballRenderer.enabled = false;
            BallSpawner.EnqueueTrayBall(id);
            return true;
        }

        void FollowMoveFromNetwork(MechanicalState mState)
        {
            if (mState.hitShapeId >= 0)
            {
                listener.OnBallCollision(listener.physicsManager.BallListeners[mState.hitShapeId]);
            }
            else if (mState.hitShapeId == -1)
            {
                listener.OnBallHitWall();
            }
            else if (mState.pocketId != -1)
            {
                listener.OnBallEnterPocket(listener.physicsManager.PocketListeners[mState.pocketId]);
            }
        }
        void FollowMoveInReplay()
        {
            if (currentState.hitShapeId >= 0)
            {
                OnState(BallState.HitBall);
            }
            else if (currentState.hitShapeId == -1)
            {
                OnState(BallState.HitBoard);
            }
            else if (currentState.pocketId != -1)
            {
                listener.physicsManager.TriggerBallPocketed(this.listener, listener.physicsManager.PocketListeners[currentState.pocketId], true);
                OnState(BallState.EnterInPocket);
            }
        }

        void FixedUpdate()
        {
            if (isFollow)
            {
                if (oldMechanicalStateId != mechanicalStateId)
                {
                    oldMechanicalStateId = mechanicalStateId;
                    currentTime = currentState.time;
                    deltaTime = 0.0f;
                    if (listener.physicsManager.IsInMove)
                    {
                        FollowMoveInReplay();
                        mechanicalStateData = MechanicalState.StateToString(currentState);
                    }

                    if (mechanicalStateId + 1 < mechanicalStates.Length)
                    {
                        currentState = MechanicalState.StateFromString(mechanicalStates[mechanicalStateId + 1]);
                    }
                    else
                    {
                        isFollow = false;
                    }
                }
                else if(isFollow)
                {
                    deltaTime += Time.fixedDeltaTime;
                    if (deltaTime >= currentState.time - currentTime)
                    {
                        mechanicalStateId++;
                    }
                }
            }

        }

        public void OnState(BallState state)
        {

            switch (state)
            {
                case BallState.SetState:
                    if (BallPoolGameLogic.playMode != PlayMode.Replay)
                    {
                        hitShapeId = -2;
                        moveData = mechanicalStateData;
                    }
                    if (!AightBallPoolNetworkGameAdapter.is3DGraphics)
                    {
                        ballBlick.position = CalculateBallBlickPosition();
                    }
                    ballShadow.position = CalculateBallShadowPosition();
                    if (inPocket)
                    {
                        ballShadow.gameObject.SetActive(false);
                    }
                    break;
                case BallState.StartMove:
                    break;
                case BallState.Move:
                    position = listener.body.position;
                    transform.rotation = listener.body.rotation;
                    if (!AightBallPoolNetworkGameAdapter.is3DGraphics)
                    {
                        ballBlick.position = CalculateBallBlickPosition();
                    }
                    ballShadow.position = CalculateBallShadowPosition();
                    if (NeedToSave() && BallPoolGameLogic.playMode != PlayMode.Replay)
                    {
                        if (!inPocket)
                        {
                            moveData += mechanicalStateData;
                            if (BallPoolGameLogic.controlInNetwork)
                            {
                                if (MyEightBallNetwork.Instance != null && Mirror.NetworkClient.isConnected) MyEightBallNetwork.Instance.SetMechanicalStatesFromNetwork(id, mechanicalStateData);


                            }
                        }
                    }
                    break;
                case BallState.EndMove:

                    if (BallPoolGameLogic.playMode != PlayMode.Replay)
                    {
                        if (!inPocket)
                        {
                            moveData += mechanicalStateData;
                            if (BallPoolGameLogic.controlInNetwork)
                            {
                                if (MyEightBallNetwork.Instance != null && Mirror.NetworkClient.isConnected) MyEightBallNetwork.Instance.SetMechanicalStatesFromNetwork(id, mechanicalStateData);


                            }
                        }
                        listener.physicsManager.ReplayManager.SaveReplay(id, moveData);
                        SetBallShadowAndBlickBlick();
                    }
                    break;
                case BallState.EnterInPocket:
                    if (BallPoolGameLogic.playMode != PlayMode.Replay)
                    {







                        inPocket = true;
                        moveData += mechanicalStateData;
                        if (BallPoolGameLogic.controlInNetwork)
                        {
                            if (MyEightBallNetwork.Instance != null && Mirror.NetworkClient.isConnected) MyEightBallNetwork.Instance.SetMechanicalStatesFromNetwork(id, mechanicalStateData);


                        }
                    }
                    ballShadow.gameObject.SetActive(false);
                    if (!AightBallPoolNetworkGameAdapter.is3DGraphics)
                    {
                        ballBlick.gameObject.SetActive(false);
                    }
                    if (listener.physicsManager.IsInMove)
                    {
                        StartCoroutine(WaitAndPlayBallInPocket());
                    }
                    break;
                case BallState.MoveInPocket:
                    position = listener.body.position;
                    transform.rotation = listener.body.rotation;
                    break;
                case BallState.ExitFromPocket:
                    if (!AightBallPoolNetworkGameAdapter.is3DGraphics)
                    {
                        ballBlick.gameObject.SetActive(true);
                    }
                    if (!AightBallPoolNetworkGameAdapter.is3DGraphics)
                    {
                        ballBlick.position = CalculateBallBlickPosition();
                    }
                    ballShadow.position = CalculateBallShadowPosition();
                    ballShadow.gameObject.SetActive(!inPocket);
                    break;
                case BallState.HitBall:
                    if (BallPoolGameLogic.playMode != PlayMode.Replay)
                    {
                        if (savedTime != listener.physicsManager.shotDuration)
                        {
                            savedTime = listener.physicsManager.shotDuration;
                            if (!inPocket)
                            {
                                moveData += mechanicalStateData;
                                if (BallPoolGameLogic.controlInNetwork)
                                {
                                    if (MyEightBallNetwork.Instance != null && Mirror.NetworkClient.isConnected) MyEightBallNetwork.Instance.SetMechanicalStatesFromNetwork(id, mechanicalStateData);

                                }
                            }
                        }
                    }
                    if (!ballHitBall.isPlaying && hitBallClipPlayingCount < 3 && listener.physicsManager.IsInMove)
                    {
                        hitBallClipPlayingCount++;
                        ballHitBall.volume = Mathf.Clamp01(2.0f * listener.NormalizedVelocity.magnitude);
                        ballHitBall.Play();
                        StartCoroutine(WaitWhenHitBallClipIsPlaying());
                    }
                    break;
                case BallState.HitBoard:
                    if (BallPoolGameLogic.playMode != PlayMode.Replay)
                    {
                        if (savedTime != listener.physicsManager.shotDuration)
                        {
                            savedTime = listener.physicsManager.shotDuration;
                            if (!inPocket)
                            {
                                moveData += mechanicalStateData;
                                if (BallPoolGameLogic.controlInNetwork)
                                {
                                    if (MyEightBallNetwork.Instance != null && Mirror.NetworkClient.isConnected) MyEightBallNetwork.Instance.SetMechanicalStatesFromNetwork(id, mechanicalStateData);


                                }
                            }
                        }
                    }
                    if (!ballHitBoard.isPlaying && hitBoardClipPlayingCount < 3 && listener.physicsManager.IsInMove)
                    {
                        hitBoardClipPlayingCount++;
                        ballHitBoard.volume = Mathf.Clamp01(2.0f * listener.NormalizedVelocity.magnitude);
                        ballHitBoard.Play();
                        StartCoroutine(WaitWhenHitBoardClipIsPlaying());
                    }
                    break;

                default:
                    break;
            }
        }

        IEnumerator WaitWhenHitBoardClipIsPlaying()
        {
            while (ballHitBoard.isPlaying)
            {
                yield return null;
            }
            hitBoardClipPlayingCount--;
        }

        IEnumerator WaitWhenHitBallClipIsPlaying()
        {
            while (ballHitBall.isPlaying)
            {
                yield return null;
            }
            hitBallClipPlayingCount--;
        }
        IEnumerator WaitAndPlayBallInPocket()
        {
            yield return new WaitForSeconds(0.2f);
            ballHitPocket.volume = Mathf.Clamp(3.0f * listener.NormalizedVelocity.magnitude, 0.3f, 1.0f);
            ballHitPocket.Play();
        }
    }
}

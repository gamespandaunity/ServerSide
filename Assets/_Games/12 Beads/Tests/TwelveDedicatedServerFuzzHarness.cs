#if UNITY_SERVER && TWELVE_DEDICATED_TESTS
using System;
using System.Collections;
using System.Reflection;
using Mirror;
using UnityEngine;

namespace Twelve.Tests
{
    // Runs inside a separately built headless player. This deliberately never starts a
    // NetworkClient, so command hardening is tested in the Edgegap server process, not a host.
    public sealed class TwelveDedicatedServerFuzzHarness : MonoBehaviour
    {
        private const string Argument = "--twelve-dedicated-fuzz";
        private const string PassMarker = "TWELVE_DEDICATED_FUZZ: PASS";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), Argument) < 0) return;
            GameObject runner = new GameObject(nameof(TwelveDedicatedServerFuzzHarness));
            DontDestroyOnLoad(runner);
            runner.AddComponent<TwelveDedicatedServerFuzzHarness>();
        }

        private IEnumerator Start()
        {
            float deadline = Time.realtimeSinceStartup + 15f;
            while (!NetworkServer.active && Time.realtimeSinceStartup < deadline)
                yield return null;

            try
            {
                Require(NetworkServer.active, "NetworkServer did not start");
                Require(!NetworkClient.active, "Host mode is not a dedicated-server test");
                FuzzRulesEngine();
                FuzzCommandHandler();
                Debug.Log(PassMarker);
                Application.Quit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"TWELVE_DEDICATED_FUZZ: FAIL {exception}");
                Application.Quit(1);
            }
        }

        private static void FuzzRulesEngine()
        {
            TwelveState state = default;
            TwelveRulesEngine.CreateInitialState(ref state);
            uint revision = state.revision;
            for (int from = TwelveBoardTopology.CellCount; from <= byte.MaxValue; from++)
            {
                for (int to = 0; to < TwelveBoardTopology.CellCount; to++)
                {
                    TwelveApplyResult result = TwelveRulesEngine.TryApplyMove(ref state,
                        PLAYERS.PLAYER1, (byte)from, (byte)to);
                    Require(!result.applied && result.rejectReason == TwelveMoveRejectReason.NodeOutOfRange,
                        $"Out-of-range move accepted from={from} to={to}");
                    Require(state.revision == revision, "Rejected move changed revision");
                }
            }
        }

        private static void FuzzCommandHandler()
        {
            NetworkGameManager ngm = NetworkGameManager.Instance;
            if (ngm == null)
                ngm = new GameObject("TwelveFuzzNetworkGameManager").AddComponent<NetworkGameManager>();

            MultiPlayerGameManagerTwelve manager = MultiPlayerGameManagerTwelve.instance;
            if (manager == null)
                manager = new GameObject("TwelveFuzzGameManager").AddComponent<MultiPlayerGameManagerTwelve>();

            TwelveBeadNetworkManager turnManager = TwelveBeadNetworkManager.instance;
            if (turnManager == null)
                turnManager = new GameObject("TwelveFuzzTurnManager").AddComponent<TwelveBeadNetworkManager>();

            SinkConnection creator = new SinkConnection(41) { isReady = true };
            SinkConnection joiner = new SinkConnection(42) { isReady = true };
            ngm.currentPlayerCount = 2;
            ngm.CreatorRef = creator;
            ngm.JoinerRef = joiner;
            manager.matchFinalized = false;
            turnManager.ServerSetCurrentPlayer(PLAYERS.PLAYER1);

            MethodInfo handler = typeof(MultiPlayerGameManagerTwelve).GetMethod(
                "ServerHandleTwelveSubmitMove", BindingFlags.Instance | BindingFlags.NonPublic);
            Require(handler != null, "Server command handler not found");

            Invoke(handler, manager, 250, 251, 0, 100, creator);
            Invoke(handler, manager, 250, 251, 0, 100, creator);
            Require(manager.duplicateRequestCount == 1, "Duplicate request was not counted");

            for (uint id = 101; id < 133; id++)
                Invoke(handler, manager, 250, 251, 999, id, creator);
            Require(manager.staleRequestCount == 32, "Stale revisions were not counted");

            for (uint id = 200; id < 264; id++)
                Invoke(handler, manager, 250, 251, 0, id, creator);
            Require(manager.RejectedMoveCounters.ContainsKey("Duplicate or rate-limited request"),
                "Submit flood did not hit the rate limiter");

            // Reusing the transport connection id must not inherit the disconnected object's
            // duplicate window. The dictionaries are intentionally keyed by connection object.
            SinkConnection reconnectedCreator = new SinkConnection(41) { isReady = true };
            ngm.CreatorRef = reconnectedCreator;
            Invoke(handler, manager, 250, 251, 0, 100, reconnectedCreator);
            Require(manager.duplicateRequestCount == 1, "Reconnect reused stale duplicate state");

            manager.matchFinalized = true;
            manager.ServerFinalize(TwelveEndReason.ScoreLimit, PLAYERS.PLAYER1);
            manager.ServerFinalize(TwelveEndReason.Timeout, PLAYERS.PLAYER2);
            Require(manager.duplicateFinalizationCount == 2, "Duplicate finalization guard failed");
        }

        private static void Invoke(MethodInfo handler, MultiPlayerGameManagerTwelve manager,
            int from, int to, uint revision, uint requestId, NetworkConnectionToClient sender)
        {
            handler.Invoke(manager, new object[] { (byte)from, (byte)to, revision, requestId, sender });
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class SinkConnection : NetworkConnectionToClient
        {
            public SinkConnection(int connectionId) : base(connectionId) { }

            protected override void SendToTransport(ArraySegment<byte> segment, int channelId)
            {
                // The fuzz harness only needs Mirror's injected sender identity. Replies are sunk.
            }
        }
    }
}
#endif

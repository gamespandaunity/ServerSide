using System.Collections.Generic;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    public static BallSpawner instance;

    // Registry of the spawned pocket-tray display balls, keyed by ball id. Lets the server
    // reconciliation CORRECT the tray when the local physics pocketed a different ball than the
    // server did (hard-break divergence: "ball 6 in the tray on one device, ball 8 on the other").
    // Without it the Instantiates were fire-and-forget and a wrong tray icon could never be removed.
    public static readonly Dictionary<int, GameObject> spawnedTrayBalls = new Dictionary<int, GameObject>();

    // Single GLOBAL spawn queue for tray balls. Every path that wants a tray icon (normal pocket,
    // reconnect restore, server reconciliation) ENQUEUES here and one runner instantiates them one
    // by one with a fixed gap — so two paths firing at the same time (e.g. BOTH players
    // reconnecting simultaneously) can never drop several balls onto the spawn point at once and
    // leave them OVERLAPPING in the pocket bar.
    static readonly Queue<int> pendingTraySpawns = new Queue<int>();
    static bool spawnRunnerActive;
    static int restoreDropsInFlight;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        spawnedTrayBalls.Clear(); // fresh scene -> fresh tray
        pendingTraySpawns.Clear();
        spawnRunnerActive = false;
        restoreDropsInFlight = 0;
    }

    public static void EnqueueTrayBall(int ballId)
    {
        if (instance == null) return;
        if (HasTrayBall(ballId) || pendingTraySpawns.Contains(ballId)) return; // dedup
        pendingTraySpawns.Enqueue(ballId);
        if (!spawnRunnerActive)
            instance.StartCoroutine(instance.TraySpawnRunner());
    }

    /// <summary>
    /// Board-restore spawn: the tray is being rebuilt to match an authoritative snapshot (reconnect,
    /// or the repair push after a rejected shot), NOT because a ball was just pocketed.
    /// </summary>
    /// <remarks>
    /// This cannot go through <see cref="EnqueueTrayBall"/> for two reasons.
    /// 1. That queue releases one ball every 0.35 s, so restoring a mid-match tray replays the whole
    ///    match's pots as a slow trickle long after the board itself is already back.
    /// 2. It hands the ball to GRAVITY, which is not guaranteed to be running. Physics.simulationMode
    ///    is Script here, so nothing falls unless PhysicsHandeler.FixedUpdate reaches its
    ///    Physics.Simulate() call — and that call is skipped for the entire duration of a network
    ///    playback. A tray ball spawned inside that window hangs at the tube top, which is the
    ///    "balls stuck, they never come down" report.
    /// A restore therefore spawns immediately and always drives the ball with the scripted kinematic
    /// drop, which needs no physics at all. Online restore paths only; nothing offline/AI calls this.
    /// </remarks>
    public static void RestoreTrayBall(int ballId)
    {
        if (instance == null || HasTrayBall(ballId)) return;
        instance.SpawnTrayBallNow(ballId, true);
    }

    System.Collections.IEnumerator TraySpawnRunner()
    {
        spawnRunnerActive = true;
        while (pendingTraySpawns.Count > 0)
        {
            SpawnTrayBallNow(pendingTraySpawns.Dequeue(), false);
            // Gap so each ball can roll down the tray tube before the next drops.
            yield return new WaitForSeconds(0.35f);
        }
        spawnRunnerActive = false;
    }

    /// <summary>
    /// Instantiates one tray ball and starts whichever descent it needs.
    /// </summary>
    GameObject SpawnTrayBallNow(int id, bool isRestore)
    {
        if (HasTrayBall(id) || ballPrefabs == null || id < 0 || id >= ballPrefabs.Length || spawnLocation == null)
            return null;

        int slotIndex = spawnedTrayBalls.Count;
        Vector3 spawnPos = spawnLocation.position;
        GameObject trayBall = Instantiate(ballPrefabs[id], spawnPos, Quaternion.identity);
        PrepareTrayBallVisuals(trayBall);
        RegisterTrayBall(id, trayBall);

        // On a WATCHER rendering an opponent's shot via playback, scene physics is paused
        // (Physics.simulationMode = Script and the playback FixedUpdate returns BEFORE
        // Physics.Simulate), so this gravity-driven tray ball would freeze at the tube top
        // until the shot ends. Drive it down with a scripted kinematic roll so it drops in
        // real time — exactly like the shooter's side. The shooter/AI (physics running) keep
        // the normal gravity roll, so this is watcher-playback only.
        bool physicsPaused = BallPool.BallPoolGameManager.instance != null
            && BallPool.BallPoolGameManager.instance.physicsManager != null
            && BallPool.BallPoolGameManager.instance.physicsManager.PlaybackActive;

        if (isRestore)
        {
            // Stagger by how many restore drops are already falling so a full tray restores as a
            // quick cascade rather than one clump landing on the same frame.
            float delay = restoreDropsInFlight * restoreDropStagger;
            restoreDropsInFlight++;
            StartCoroutine(ScriptedTrayDrop(trayBall, slotIndex, delay, true));
        }
        else if (physicsPaused)
        {
            StartCoroutine(ScriptedTrayDrop(trayBall, slotIndex, 0f, false));
        }
        else
        {
            StartCoroutine(TrayDropStuckWatchdog(trayBall, slotIndex, spawnPos));
        }

        return trayBall;
    }

    /// <summary>
    /// Safety net for the gravity descent: if playback starts (or the ball comes to rest on a ledge
    /// and the very low sleepThreshold puts it to sleep) the ball can stop before it ever reaches the
    /// rail, and nothing wakes it. Once it has clearly failed to leave the spawn point, hand it to the
    /// scripted drop, which does not depend on physics running at all.
    /// Online only — offline/AI keeps the original pure-gravity behaviour.
    /// </summary>
    System.Collections.IEnumerator TrayDropStuckWatchdog(GameObject trayBall, int slotIndex, Vector3 spawnPos)
    {
        if (!BallPool.BallPoolGameLogic.isOnLine)
            yield break;

        float waited = 0f;
        while (waited < trayStuckTimeout)
        {
            if (trayBall == null)
                yield break;
            if (Vector3.Distance(trayBall.transform.position, spawnPos) > TrayEscapedSpawnDistance)
                yield break;   // it is on its way down; leave gravity alone

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (trayBall == null)
            yield break;

        Debug.LogWarning($"[8Ball][Tray] Tray ball for slot {slotIndex} never left the spawn point in {trayStuckTimeout:F1}s (scene physics is not stepping); driving it down with the scripted drop.");
        yield return ScriptedTrayDrop(trayBall, slotIndex, 0f, false);
    }

    // Registers (replacing any duplicate) the tray object for a ball id.
    public static void RegisterTrayBall(int ballId, GameObject trayBall)
    {
        if (spawnedTrayBalls.TryGetValue(ballId, out GameObject existing) && existing != null && existing != trayBall)
            Destroy(existing);
        PrepareTrayBallVisuals(trayBall);
        spawnedTrayBalls[ballId] = trayBall;
    }

    static void PrepareTrayBallVisuals(GameObject trayBall)
    {
        if (trayBall == null)
            return;

        foreach (Transform child in trayBall.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "BallBlick" || child.name == "BallShadow")
                child.gameObject.SetActive(false);
        }
    }

    // Removes (and destroys) the tray object for a ball id — used when the server says this ball
    // is actually ON the table and the local pocket was divergent.
    public static void RemoveTrayBall(int ballId)
    {
        if (spawnedTrayBalls.TryGetValue(ballId, out GameObject existing))
        {
            if (existing != null) Destroy(existing);
            spawnedTrayBalls.Remove(ballId);
        }
    }

    public static bool HasTrayBall(int ballId)
    {
        return spawnedTrayBalls.TryGetValue(ballId, out GameObject go) && go != null;
    }

    public GameObject[] ballPrefabs;
    public Transform spawnLocation;

    [Header("Watcher scripted tray drop (playback only)")]
    [Tooltip("Rest position of the first tray ball. slot[N] = traySlot0 + traySlotStep * N. Tune in play mode if off.")]
    [SerializeField] Vector3 traySlot0 = new Vector3(-1.636f, 0.150f, -0.705f);
    [SerializeField] Vector3 traySlotStep = new Vector3(0f, 0.015f, 0.0837f);
    [SerializeField] float trayDropDuration = 0.5f;

    [Header("Restore / stuck recovery")]
    [Tooltip("Gap between consecutive scripted drops when a whole tray is restored on reconnect.")]
    [SerializeField] float restoreDropStagger = 0.07f;
    [Tooltip("How long a gravity-driven tray ball may sit at the spawn point before the scripted drop takes over.")]
    [SerializeField] float trayStuckTimeout = 2.5f;

    // A healthy gravity drop clears this much within a few frames, so still being inside it after
    // trayStuckTimeout means physics is not stepping at all rather than the ball being slow.
    const float TrayEscapedSpawnDistance = 0.1f;

    Vector3 TraySlotPosition(int slotIndex)
    {
        return traySlot0 + traySlotStep * slotIndex;
    }

    // Physics is paused during watcher playback (and during a reconnect restore there is no shot to
    // drive it either), so a freshly-spawned tray ball would hang at the tube top. Animate it
    // (kinematic) down the tube and along the rail to its slot in real time, then hand it back to
    // physics so it settles to the EXACT rail rest the instant scene physics resumes (this
    // self-corrects any small error in the scripted slot target).
    System.Collections.IEnumerator ScriptedTrayDrop(GameObject trayBall, int slotIndex, float startDelay, bool isRestore)
    {
        Rigidbody rb = trayBall != null ? trayBall.GetComponent<Rigidbody>() : null;
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Real time throughout: the drop must look identical whether or not the scene physics clock
        // is running, and the ball is kinematic for the whole animation anyway.
        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        if (trayBall != null)
        {
            Vector3 start = trayBall.transform.position;
            Vector3 target = TraySlotPosition(slotIndex);
            // Quadratic Bezier: drop straight down the tube first (control at slot height under the spawn
            // point), then roll along the rail to the slot — mimics the gravity path on the shooter side.
            Vector3 control = new Vector3(start.x, target.y, start.z);
            float dur = Mathf.Max(0.1f, trayDropDuration);
            float t = 0f;
            while (t < dur && trayBall != null)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / dur);
                float inv = 1f - u;
                trayBall.transform.position = inv * inv * start + 2f * inv * u * control + u * u * target;
                trayBall.transform.Rotate(Vector3.forward, 540f * Time.unscaledDeltaTime, Space.Self);
                yield return null;
            }

            if (trayBall != null)
            {
                trayBall.transform.position = target;
                if (rb != null) rb.isKinematic = false;
            }
        }

        if (isRestore)
            restoreDropsInFlight = Mathf.Max(0, restoreDropsInFlight - 1);
    }

}

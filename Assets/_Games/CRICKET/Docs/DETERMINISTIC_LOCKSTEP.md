# Cricket MP: Full-Local Deterministic Architecture ("plays local at 250ms ping")

**Goal:** both clients run the ENTIRE match simulation locally and identically. The network carries only
(a) per-delivery seeds/params, (b) the batter's one shot input, (c) a per-ball outcome checksum.
No position streaming, no adopt/snap corrections, no authority/follower split for the flight.
At 250ms ping the game must neither stall nor jerk — latency only delays *information*, never *simulation*.

## Why this works for cricket

- Turn-based: one delivery at a time; the only mid-flight human input is the batter's single swing.
- The flight is already **parametric** (closed-form sin-arc paths, not stateful integration) — a late-arriving
  input can be applied retroactively by re-evaluating the path functions; no rollback engine needed.
- The deterministic fixed-step foundation ALREADY EXISTS (BALL_SIM_FIXED_STEP = 1/60, NetworkTime-anchored
  StepDeliveryDeterministic / StepBallDeterministic, RPC_SyncBallRelease with startNetTime).
- Fielder/keeper state machines are deterministic given an identical ball path + seeded RNG (today's
  divergences all originate in the stream corrections, not the machines).

## Current pain (what this deletes)

Authority/follower split → 20Hz RPC_SyncBallShot stream → adopt + dead-band + unpause + freeze-reentry +
rest-skip + ghost-hide + catch-visual force + camera damping band-aids. ~90% of tester issues
(jerk, camera divergence, keeper/catch splits, "ball fielder se pichhe") live in this layer.

## The one real blocker: bat contact is not deterministic today

Contact = Unity bat colliders + animation-frame sampling → frame-rate dependent → cannot be replayed
identically on the other machine. Everything else already is (or is trivially seedable).

**Fix: replace collider contact with a deterministic timing-window model.**

## Phase 1 — Deterministic contact model (the core)

New file: `Assembly-CSharp/DeterministicShot.cs` (pure static functions, no Unity state):

```
Input:  DeliveryParams  (already synced at release: angle, hVel, arc, spin, swing, isFullToss, seed)
        ShotInput       { int swingTick; string shotType; bool loft; }
Output: ContactResult   { Miss | Edge(dir) | Hit }
        Hit = { ballAngle, launchAngle, arcHeight, hVel, contactPos } — same fields the authority
        relays today via RPC_ChangeBallAngle.
```

- **Ideal tick**: derived from the parametric flight — the tick the ball crosses the bat plane
  (z ≈ contact band 5.7–7.5 per [ShotContact] data).
- **Timing delta** = swingTick − idealTick → quality bands: perfect / good / early / late / miss.
  Early/late shifts ballAngle and scales hVel; outside the band = miss (ball continues, keeper collects —
  keeper y<1.3 collect fix already shipped); a narrow band at the edge = Edge with deflection.
- **Table extraction**: base angle/power per shotType comes from the live game — the `[ShotContact]`
  diag already logs (angle, batHeight, contactPos, shot) per real shot. Collect a few sessions of that
  data and fit the per-shot base angle + timing spread so the new model reproduces today's feel.
- Bat colliders/animations remain **presentation only**.

## Phase 2 — Input relay (replaces the whole post-shot stream)

- Batter, at swing start: `CmdShotInput(deliveryId, swingTick, shotType, loft)` — ONE packet.
- Both clients call `DeterministicShot.Resolve(...)` and continue their local sims with the identical result.
- **Late input at 250ms ping:** the bowler's sim is mid-flight when the input arrives. The flight is
  parametric, so: compute contact at swingTick (in the past), evaluate the post-shot path at the CURRENT
  tick, and **blend the rendered ball onto the new path over ~100ms** (render-only lerp; the sim state
  itself is exact). One smooth correction per shot, versus 20 snaps/second today.
- No input by ideal-tick + grace (RTT budget ~400ms): both sims resolve Miss identically at the same tick
  (the batter who genuinely didn't swing sends nothing; a swing that arrives after grace converts to the
  relayed result retroactively — same blend).

## Phase 3 — RNG seeding audit

Per-delivery `int deliverySeed` relayed inside the existing release RPC. All gameplay RNG in the delivery/
shot/fielding path draws from `new System.Random(deliverySeed)` locals: friction pick, edge windows,
fielder pick/chase offsets, keeper anim choices, fence rebound angle (Delivery.cs ~4944 — today's known
diverger). UnityEngine.Random stays for pure cosmetics only.

## Phase 4 — Checksum safety net + layer deletion

- End of each ball, both clients compute `hash(outcome, runs, wicketType, finalBallPos-quantized)`.
  Batter's ball-outcome relay (existing RpcBallOutcome) carries its hash; mismatch on the bowler → adopt the
  relayed outcome + `[DetChecksum]` log. Expected: never in practice (per-delivery sims are 2–5s; ARM/x86
  float drift over ~300 ticks with coarse 0.5u gameplay bands should not flip outcomes) — but cheap insurance.
- Then DELETE (behind the kill-switch first): RPC_SyncBallShot stream + adopt + dead-band + unpause +
  freeze-reentry + rest-skip machinery, keeper stream band-aids, camera BowlingFollowCamX damping
  (cameras read the local sim only).

## Rollout

- `ConstantsData_M.useDeterministicContact` — ships dark (false). Editors + local dedicated server first.
- Test rig: Mirror's LatencySimulation transport at 250ms/±50ms jitter, 2 editors + server — the exact
  acceptance bar: full over each side, zero visible correction.
- Old path stays intact until the new one passes the tester matrix; then the deletion phase.

## Order of work (sessions)

1. Extract the current shot tables (instrument + collect [ShotContact] data; read the bat-contact branch in
   GroundController.Delivery.cs ~430 / OnCustomTriggerEnter bat path for the exact base formulas).
2. DeterministicShot.cs + unit-style editor tests (same input ⇒ same output, both repos).
3. CmdShotInput relay + both-sides Resolve behind the flag.
4. Seed audit. 5. Checksum. 6. Latency-rig acceptance. 7. Delete the stream layer.

## Status (2026-07-11)

| Increment | State |
|---|---|
| 1. DeterministicRng + per-delivery seed plumbing (release RPC) | ✅ shipped dark |
| 2. Shot tables (115 rolls) + fence/pad deflections seeded | ✅ shipped dark |
| 3. CmdShotInput relay + both-sides tick-resolved contact (LOCKSTEP_CONTACT_Z=6.3), bat colliders presentation-only, RPC_ChangeBallAngle + RPC_SyncBallShot stream ignored under the flag | ✅ shipped dark |
| 4. Outcome safety net | ✅ existing RpcBallOutcome/RpcCorrectBallState relays stay ON under the flag — the batting outcome remains authoritative; a correction firing IS the checksum signal (watch its log during acceptance) |
| 5. Acceptance rig | ⏳ manual: NetworkManager GameObject → add Mirror `LatencySimulation` transport (wrap the KCP transport; latency 125ms each way = 250 RTT, jitter 20ms) → flip `useDeterministicContact=true` on BOTH repos → one full over each side. Watch for: `[Lockstep] Contact resolved` with IDENTICAL angle/hVel on both logs; any `RpcCorrectBallState` correction = divergence to investigate. |
| 6. Stream-layer deletion | ⏳ only after 5 passes the tester matrix |

Flip-on checklist: `useDeterministicContact = true` in BOTH repos' ConstantsData_M → rebuild both → play.
Flag off = byte-identical legacy behaviour (all paths gated).

// ════════════════════════════════════════════════════════════════════════════════════════════
// GroundController.Camera — camera switching + follow for the match (side cams, follow cam,
// FOV lerps live in Gameplay/Cameras/*). Under lockstep both clients follow their own local
// deterministic ball, so the old divergence band-aids (freeze/switch-skips) are gone.
// ════════════════════════════════════════════════════════════════════════════════════════════
using Beebyte.Obfuscator;
using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Cricket;
using UnityEngine.Serialization;

// Partial of GroundController — thematic split (see GroundController.cs for fields + lifecycle).
public partial class GroundController
{
    private void setPreviewCamPanel()
    {
        float num = 267f;
        float num2 = 217f;
        float num3 = 1.33f;
        float num4 = -151f;
        float num5 = Screen.width;
        float num6 = Screen.height;
        float num7 = num5 / num6;
        float num8 = num * num7 / num3;
        float num9 = num4 * num7 / num3;
    }

    public void showPreviewCamera(bool status)
    {
        cameraPreview.enabled = status;
        ballPreviewPanel.SetActive(status);
        if (Singleton<Scoreboard>.instance.scoreBoard.activeSelf)
        {
            Singleton<PreviewScreen>.instance.previewScreen.SetActive(!status);
        }
    }

    private void RepositionSideCamera()
    {
        if ((!(_ballAngle >= 180f) || !(_ballAngle <= 210f)) && _ballAngle >= 330f && !(_ballAngle <= 359f))
        {
        }
    }

    // The framing a shot starts from. Matches ResetAll's per-delivery reset of both side cameras, so the
    // two cannot drift apart; kept as one named value because three separate paths write this field.
    private const float SIDE_CAMERA_SHOT_FOV = 50f;

    /// <summary>
    /// Parks both side cameras back at the shot framing. ResetAll does this once per delivery, which is
    /// enough for every ball EXCEPT the last one of an innings: the replay follow drives the field of view
    /// down a distance curve (45 - dist/2, floor 12) and there is no next delivery to reset it, so the
    /// innings ends — and the break screen behind it sits — on whatever zoom the replay finished at.
    /// That is the tester's "camera gets close again prominently on the last ball of an inning".
    /// </summary>
    public void ResetSideCameraFraming()
    {
        if (leftFieldCamera != null) leftFieldCamera.fieldOfView = SIDE_CAMERA_SHOT_FOV;
        if (rightFieldCamera != null) rightFieldCamera.fieldOfView = SIDE_CAMERA_SHOT_FOV;
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
            ConstantsData_M.MpLog($"[CamDiag] Innings end: side-camera FOV parked at left={(leftFieldCamera != null ? leftFieldCamera.fieldOfView.ToString("F1") : "<null>")} right={(rightFieldCamera != null ? rightFieldCamera.fieldOfView.ToString("F1") : "<null>")}.");
    }

    public void ZoomCameraToPitch()
    {
        if (CONTROLLER.cameraType == 0)
        {
            iTween.MoveTo(gameplayCamera.gameObject, iTween.Hash("position", gameplayCameraZoomInPosition, "time", 3, "easetype", "easeInOutSine"));
        }
        else
        {
            iTween.MoveTo(gameplayCamera.gameObject, iTween.Hash("position", gameplayCameraZoomInPosition, "time", 5, "easetype", "easeInOutSine"));
        }
        for (int i = 0; i < slipFielders.Count; i++)
        {
            if (slipFielders[i] != null)
            {
                GameObject gameObject = slipFielders[i];
                gameObject.GetComponent<Animation>().Play("getReadyInSlip");
                gameObject.GetComponent<Animation>()["getReadyInSlip"].speed = UnityEngine.Random.Range(1, 3);
            }
        }
    }

    public void ZoomCameraToBowler()
    {
        if (bowlerZoomCameraStartTime != -1f && Time.time > bowlerZoomCameraStartTime + 0.2f)
        {
            bowlerZoomCameraStartTime = -1f;
            if (bowlerType == "fast")
            {
                iTween.MoveTo(gameplayCamera.gameObject, iTween.Hash("position", fastBowlerCameraZoomOutPosition, "time", 0, "easetype", "easeInOutSine", "oncomplete", "ZoomCameraToBowlerOnComplete", "oncompletetarget", base.gameObject));
            }
            else
            {
                iTween.MoveTo(gameplayCamera.gameObject, iTween.Hash("position", gameplayCameraZoomOutPosition, "time", 0, "easetype", "easeInOutSine", "oncomplete", "ZoomCameraToBowlerOnComplete", "oncompletetarget", base.gameObject));
            }
        }
    }

    [Skip]
    private void ZoomCameraToBowlerOnComplete()
    {
        ActivateColliders(boolean: false);
        if (CONTROLLER.cameraType == 0)
        {
        }
        batsmanTriggeredAttemptedShot = false;
        isShotAllowed = false;
        isBowlerWaiting = true;
        canShowFieldControlPowers = true;
        if (Singleton<GameData>.instance != null)
        {
            Singleton<GameData>.instance.ShowBowlingInterface(boolean: true);
        }
    }

    private void ZoomCameraToBatsman()
    {
        Vector3 vector = gameplayCameraZoomInPosition;
        vector += new Vector3(0f, 0f, 2f);
        if (CONTROLLER.cameraType == 0)
        {
            iTween.MoveTo(gameplayCamera.gameObject, iTween.Hash("position", vector, "time", 0.5, "easetype", "easeInOutSine"));
        }
        else
        {
            iTween.MoveTo(gameplayCamera.gameObject, iTween.Hash("position", vector, "time", 0.3, "easetype", "easeInOutSine"));
        }
    }

    private void ZoomCameraToUmpire()
    {
        Vector3 vector = gameplayCameraZoomInPosition;
        vector -= new Vector3(0f, 0.5f, 0f);
        iTween.MoveTo(gameplayCamera.gameObject, iTween.Hash("position", vector, "time", 1, "easetype", "easeInOutSine"));
    }

    private void ZoomCameraToWicketKeeper()
    {
        closeUpCameraTransform.position = _wicketKeeperTransform.position;
        closeUpCameraTransform.position = new Vector3(closeUpCameraTransform.position.x, 2f, closeUpCameraTransform.position.z);
        closeUpCameraTransform.position -= new Vector3(0f, 0f, 8f);
        closeUpCameraTransform.eulerAngles = new Vector3(closeUpCameraTransform.eulerAngles.x, 0f, closeUpCameraTransform.eulerAngles.z);
        closeUpViewCamera.enabled = true;
    }

    private void InitCamera()
    {
        if (bowlerType == "fast")
        {
            mainCamTransform.position = fastBowlerCameraZoomOutPosition;
        }
        else
        {
            mainCamTransform.position = gameplayCameraZoomOutPosition;
        }
        mainCamTransform.eulerAngles = gameplayCameraInitialRotation;
    }

    // Camera-jerk fix (bowling side): online the bowling client's ball position is stream-fed (20 Hz
    // packets + corrections), so a per-frame hard LookAt jerks the follow camera each step. Damp the
    // ROTATION on the bowling client only — the camera POSITION/framing is left exactly as the stock path
    // sets it, so the view still matches the batting side; only the jitter is removed. Batting keeps the
    // exact original instant LookAt (its locally-simulated ball is already smooth).
    // Bowling-side camera-jerk fix: on the bowling client the post-shot ball position is rebuilt from
    // discrete network snaps (RPC_SyncBallShot adopt + RpcSyncBallTrajectory corrections), so
    // temporaryPosition.x oscillates (e.g. jumps backward then forward) — the top-down follow camera
    // snaps its X to it every frame and visibly jerks. Damp the X toward the target instead of snapping.
    // Camera-only (no physics/collision/fielder impact); bowling-client + online only, so the batting
    // side and offline are untouched. Tunable lerp speed.
    private float BowlingFollowCamX(float targetX, float currentX)
    {
        // Stadium-clip fix: every caller feeds the BALL's X here and moves the main camera to it with no
        // bound — on a hard square shot the ball's X runs to the boundary (±80u+), sliding the camera
        // sideways INSIDE the stadium stands (screen fills with clipped orange interior). Clamp the follow
        // to a corridor around the pitch line; SmoothFollowLookAt still ROTATES to track the ball, so the
        // shot stays framed — the camera body just never leaves the field. (initialBallPosition.x = pitch X,
        // captured at ball spawn — valid before any post-shot follow can run.)
        targetX = Mathf.Clamp(targetX, initialBallPosition.x - 25f, initialBallPosition.x + 25f);
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
            return Mathf.Lerp(currentX, targetX, Time.deltaTime * 8f);
        return targetX;
    }

    private void SmoothFollowLookAt(Transform cam, Transform target)
    {
        if (cam == null || target == null) return;
        // Tester #2 (back-boundary jerk on the BATSMAN side too): the damped Slerp was bowling-follower-only;
        // the batting side kept the instant cam.LookAt, which whips when a boundary ball crosses close to a
        // side camera (large angular change in one frame). Apply the same critically-fast damp (10/s — near
        // instant for normal tracking, kills only the whip) to BOTH sides in online MP. Offline/AI keeps the
        // stock instant LookAt.
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
        {
            // NOTE: the old "ball-behind" camera freeze (don't follow the ball once it passed the keeper) was
            // a band-aid for the spin/beaten-ball DIVERGENCE that made the follow camera jerk. Now that the
            // post-shot flight is deterministic + the keeper catch and full fielding setup are synced, the
            // bowling ball no longer diverges, so the freeze is removed — the camera follows the ball normally
            // (it was making the bowling camera stop rotating). Smooth Slerp follow kept for a non-jerky feel.
            Vector3 dir = target.position - cam.position;
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                cam.rotation = Quaternion.Slerp(cam.rotation, targetRot, Time.deltaTime * 10f);
            }
        }
        else
        {
            cam.LookAt(target);
        }
    }

    // [CamFollow] Computes the bowling-client PREDICTIVE follow target once per frame. Critically-damped
    // SmoothDamp (never overshoots) chasing a small, bounded velocity lead so the camera hides ~1 packet of
    // network lag without ever sailing ahead of a stopping ball. Camera-only: reads matchBallTransform/
    // _ballAngle/horizontalVelocity/isBallPaused, writes only _camFollow* + the inert _camFollowProxy. Off
    // the bowling+online+live gate it passes the raw ball straight through, so BowlingFollowCamX/
    // SmoothFollowLookAt collapse to the original instant path on batting/offline/replay (untouched).
    private void UpdateBowlingCamFollowTarget()
    {
        if (_camFollowProxy == null)
        {
            _camFollowProxy = new GameObject("_CamFollowProxy").transform; // no collider/rigidbody -> inert
            _camFollowProxy.SetParent(transform, worldPositionStays: false);
        }
        Vector3 rawBall = matchBallTransform.position;                      // PURE READ — never written back
        // !isReplayModeActive: replay ball is driven by the smooth local integrator (no 20Hz snap), so it
        // needs no smoothing — adding lag/lead there would make the slow-mo replay follow WORSE.
        bool gated = CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                     && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
                     && !isReplayModeActive;
        if (!gated || currentBallStatus != "shotSuccess")
        {
            _camFollowInit = false;
            _camFollowTarget = rawBall;
            _camFollowProxy.position = rawBall;                             // batting/offline/replay = instant
            return;
        }
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        if (!_camFollowInit)                                               // seed exactly on the ball, zero lead
        {
            _camFollowTarget = rawBall; _camFollowVel = Vector3.zero;
            _camRawPrev = rawBall; _camPredictScale = 0f; _camFollowInit = true;
            _camFollowProxy.position = rawBall;
            return;
        }

        // Analytic per-second horizontal velocity (matches BallMovement's per-step delta, packet-noise-free
        // so the lead never inherits the 20Hz snap).
        float a = _ballAngle * degToRad;
        Vector3 vel = new Vector3(Mathf.Cos(a) * horizontalVelocity, 0f, Mathf.Sin(a) * horizontalVelocity);
        float speed = vel.magnitude;
        Vector3 frameDelta = rawBall - _camRawPrev;

        float wantScale = Mathf.Clamp01((speed - CAM_LEAD_MIN_SPEED) / (CAM_PREDICT_FULL_SPEED - CAM_LEAD_MIN_SPEED));
        // STOP guards — kill the lead the instant the ball is no longer advancing in WORLD space. Critical:
        // a catch/clean-stop sets isBallPaused=true and FREEZES the position while leaving horizontalVelocity
        // high (so a speed-only guard fails) — that is the overshoot-then-snapback case. isBallPaused + a
        // world-space frozen-position check catch it regardless of the stale velocity field.
        bool reversed = frameDelta.sqrMagnitude > 0.0001f && Vector3.Dot(frameDelta, vel) < 0f;
        bool frozen = isBallPaused || frameDelta.sqrMagnitude < 1e-5f;
        if (speed < CAM_LEAD_MIN_SPEED || reversed || frozen) wantScale = 0f;
        _camPredictScale = Mathf.Lerp(_camPredictScale, wantScale, Mathf.Clamp01(dt * CAM_PREDICT_EASE));

        Vector3 lead = vel * (CAM_LEAD_SECONDS * _camPredictScale);
        float lm = lead.magnitude; if (lm > CAM_LEAD_MAX) lead *= CAM_LEAD_MAX / lm;   // hard distance clamp
        Vector3 aim = rawBall + lead;
        aim.y = rawBall.y;                                                  // never lead vertically (arc apex)

        // Critically-damped spring: never crosses its target -> structurally cannot overshoot/snap-back.
        _camFollowTarget = Vector3.SmoothDamp(_camFollowTarget, aim, ref _camFollowVel,
                                              CAM_SMOOTH_TIME, Mathf.Infinity, dt);

        // Leash: bound only the TRAILING failure (a frame hitch leaving the target far behind), never ahead.
        Vector3 leash = _camFollowTarget - rawBall;
        if (leash.magnitude > CAM_MAX_LAG) _camFollowTarget = rawBall + leash.normalized * CAM_MAX_LAG;
        _camFollowTarget.y = rawBall.y;                                     // re-pin Y to truth

        _camRawPrev = rawBall;
        _camFollowProxy.position = _camFollowTarget;
    }

    private float _ghostBallStuckTimer;
    private Vector3 _ghostBallLastPos;

    private void LookForMainCameraTopDownView()
    {
        if (isThirdUmpireRunoutReplaySkipped)
        {
            return;
        }
        // [CamFollowDiag] Bowling-side post-shot camera-follow trace. The follow camera reads
        // temporaryPosition/_ballAngle/matchBallTransform — locally integrated on the batting client,
        // but stream-fed (RPC_SyncBallShot) on the bowling client. Logs whether the ball state is
        // actually advancing on the bowling client (ballPos/tempPos changing + shotSyncRecv rising) and
        // which follow path is taken (camType / topDown / sideCam / ballAngle), so a bowler-side log of
        // a four/catch where the camera froze pinpoints the failing link. Bowling client + online only.
        // Now logs on BOTH sides (removed the BowlingTeamIndex restriction) so a WIDE/boundary log shows which
        // camera + angle the BATTING side uses (side=BAT) vs the BOWLING follower (side=BOWL) — to match the
        // bowler-side wide-follow to the batter's (tester: "bowler side camera top-down, batter side proper follow").
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && isBallReleased && !isReplayModeActive)
        {
            _camFollowDiagTimer += Time.unscaledDeltaTime;
            if (_camFollowDiagTimer >= 0.25f)
            {
                _camFollowDiagTimer = 0f;
                string _side = (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex) ? "BAT" : "BOWL";
                string _activeCam = (gameplayCamera != null && gameplayCamera.enabled) ? "gameplay"
                    : (leftFieldCamera != null && leftFieldCamera.enabled) ? "left"
                    : (rightFieldCamera != null && rightFieldCamera.enabled) ? "right" : "none";
                // ConstantsData_M.MpLog($"[CamFollowDiag] side={_side} activeCam={_activeCam} status={currentBallStatus} ballHit={isBallHit} ballAngle={CONTROLLER.BALLANGLE:F1} topDown={isMainCameraInTopDownView} camType={CONTROLLER.cameraType} sideCamSel={isSideCameraSelected} ballPos={matchBallTransform.position} mainCam={mainCamTransform.position} leftCam={leftCamTransform.position} rightCam={rightCamTransform.position} boundary={isBallOnBoundaryLine} wide={isBallWide}");
            }
        }
        // Ghost-ball cleanup (tester: bowling side "ball nazar aati hai" — game keeps running fine, but a leftover
        // ball stays VISIBLE stuck at the keeper). When a shot registered here (isBallHit) but its flight never
        // applied on this BOWLING follower (RpcSyncBallShot lost — e.g. during the unstable connection), the ball
        // stops PAST the keeper (z > outOfPitchZPosition) with a BLANK status and just sits there rendered while
        // play continues. If it stays frozen there for >1.5s, hide it. Cosmetic ONLY — toggles the renderer, never
        // touches score/outcome/sim/state. A real shot keeps status=="shotSuccess" (not blank), a dot/leave keeps
        // "bowling", a catch/keeper keeps its own status — so this only ever hides the blank-status ghost.
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false && !isReplayModeActive
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex
            && isBallReleased && isBallHit && string.IsNullOrEmpty(currentBallStatus)
            && matchBallTransform != null && matchBallTransform.position.z > outOfPitchZPosition)
        {
            if ((matchBallTransform.position - _ghostBallLastPos).sqrMagnitude < 0.0004f)   // ~<0.02u moved = frozen
            {
                _ghostBallStuckTimer += Time.unscaledDeltaTime;
                if (_ghostBallStuckTimer > 1.5f)
                    ShowBall(status: false);   // hide the lingering ghost; the next delivery's ShowBall(true) re-shows it
            }
            else
            {
                _ghostBallStuckTimer = 0f;
            }
            _ghostBallLastPos = matchBallTransform.position;
        }
        else
        {
            _ghostBallStuckTimer = 0f;
        }
        // [CamFollow] One predictive-target compute per frame feeds BOTH the live and replay/else follow
        // blocks below (same proxy/_camFollowTarget). Off the bowling+online+live gate it = raw ball.
        UpdateBowlingCamFollowTarget();
        if (!isReplayModeActive)
        {
            //if (ballTransform.position.z > outOfPitchZPos && ballStatus == "bowling" && !cameraToKeeper)
            if (temporaryPosition.z > outOfPitchZPosition && currentBallStatus == "bowling" && !isCameraFocusedOnKeeper)
            {
                isCameraFocusedOnKeeper = true;
                // White-spot fix (morning#2): the bowlingSpot disc hides on the BATTING client when the ball
                // reaches the batsman (currentActionState==3) and on a relayed HIT (MultiplayerHit) — but on the
                // BOWLING follower a dot/leave/miss to the keeper hits NEITHER path, so the white pitch mark
                // lingered. The ball has passed the keeper here (every client, every no-shot outcome) → hide it.
                HideBowlingSpot();
                ActivateStadiumAndSkybox(boolean: true);
                SetCurrentBatsmanAnimSpeed(1f);
                if (!(wkOppositeLength > 1f) || UnityEngine.Random.Range(0, 10) <= 5 || !isReplayModeActive)
                {
                }
                if (isBatsmanConfident)
                {
                    DecreaseConfidenceLevel(currentShotPlayed);
                }
            }
            // #3 (bowler-side camera doesn't follow a WIDE): a wide is never a shot, so currentBallStatus stays
            // "bowling" and this gate never fired on the bowling follower → its camera stayed on the pitch while
            // the batting side followed the ball. The earlier fix only accepted "wideAndBoundary" (a wide that
            // reaches the rope, GroundController.cs:4084) — but a PLAIN wide to the keeper (isBallWide, e.g. logged
            // ball at (4.38,59.91), not a boundary) was still not followed. Accept ANY wide (isBallWide) so the
            // bowler-side camera follows every wide, matching the batting side.
            else if ((currentBallStatus == "shotSuccess" || currentBoundaryAction == "wideAndBoundary" || isBallWide) && !isMainCameraInTopDownView && currentBallStatus != "bowled" && !isSlipShot)
            {
                isMainCameraInTopDownView = true;
                topDownViewActivationTime = Time.time;
            }
            if (isSlipShot && !isReplayModeActive && !isBallPickedByFielder)
            {
                mainCamTransform.position += new Vector3(0f, 0f, 3f * Time.deltaTime);
                mainCamTransform.LookAt(raycastAnchorBallTransform);
            }
            else if (isSlipShot && !isReplayModeActive && isBallPickedByFielder)
            {
                mainCamTransform.position -= new Vector3(0f, 0f, 3f * Time.deltaTime);
                mainCamTransform.LookAt(raycastAnchorBallTransform);
            }
            if (!isMainCameraInTopDownView || isReplayModeActive)
            {
                return;
            }
            // #4 (catch camera JERK on the bowling side): a keeper catch sets isBallPaused=true and the ball
            // freezes mid-air on the FOLLOWER (the catch doesn't visually complete until the relayed outcome
            // resolves ~2-3s later). Re-zooming/re-aiming the follow camera at that frozen-then-reset ball
            // produced a visible jerk for those 2-3s (tester: "camera jerk aata hai 2-3 sec ke liye phir theek").
            // HOLD the camera while the ball is paused — camera-ONLY (no physics/outcome/catch impact),
            // bowling-follower + online only; the follow resumes (damped) once isBallPaused clears.
            if (isBallPaused && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
            {
                return;
            }
            // Wide follows at BOWLING height, NOT top-down (parity with client): skip this zoom-out for a wide so the
            // camera stays at the resting bowling Y instead of climbing to a top-down view. A wide is never a shot, so
            // the shotSuccess / catch zooms are unaffected (isBallWide is false for those).
            if (Time.time < topDownViewActivationTime + topDownViewZoomDuration && !isBallWide)
            {
                //mainCameraTransform.position = new Vector3(ballTransform.position.x, mainCameraTransform.position.y, mainCameraTransform.position.z);
                mainCamTransform.position = new Vector3(BowlingFollowCamX(_camFollowTarget.x, mainCamTransform.position.x), mainCamTransform.position.y, mainCamTransform.position.z);
                mainCamTransform.position += new Vector3(0f, 3f * Time.deltaTime, 28f * Time.deltaTime);
                mainCamTransform.eulerAngles += new Vector3(17f * Time.deltaTime, 0f, 0f);
                if (CONTROLLER.cameraType == 0)
                {
                    gameplayCamera.fieldOfView += 40f * Time.deltaTime;
                }
                else
                {
                    gameplayCamera.fieldOfView += 25f * Time.deltaTime;
                }
                SmoothFollowLookAt(mainCamTransform, _camFollowProxy);
                return;
            }
            // #3b (WIDE follow on the DEFAULT camera rig, cameraType==1) — parity with client: here
            // mainCamTransform IS the visible gameplay camera. The shot side-cam switch below is gated on
            // currentBallStatus=="shotSuccess", so for a WIDE (status stays "bowling") the side cams never
            // enable and mainCamTransform FREEZES after the 0.8s zoom window while the ball runs on to the
            // keeper. Physically follow the ball down-pitch (X damped via BowlingFollowCamX, Z lerped toward
            // the ball) + rotate via SmoothFollowLookAt, matching the batting side. WIDE-only, online
            // bowling-follower + live only, cameraType!=0. && gameplayCamera.enabled: once the keeper-miss
            // switch enables a side cam and disables gameplayCamera, #3b must YIELD to the side-cam follow.
            if (CONTROLLER.cameraType != 0 && isBallWide && currentBallStatus != "shotSuccess"
                && CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && !isReplayModeActive
                && gameplayCamera.enabled)
            {
                Vector3 wideCamTarget = _camFollowProxy.position;   // raw ball for a wide (UpdateBowlingCamFollowTarget only smooths shots)
                mainCamTransform.position = new Vector3(
                    BowlingFollowCamX(wideCamTarget.x, mainCamTransform.position.x),
                    mainCamTransform.position.y,
                    Mathf.Lerp(mainCamTransform.position.z, wideCamTarget.z, Time.deltaTime * 2.5f));
                SmoothFollowLookAt(mainCamTransform, _camFollowProxy);
                return;
            }
            if (CONTROLLER.cameraType == 0)
            {
                Singleton<MainCameraController>.instance.StartFollowTween();
                return;
            }
            if (currentBallStatus == "shotSuccess" && !isSideCameraSelected)
            {
                // NOTE: the old bowling-side "don't switch to the ball-behind camera" guard (skip the side
                // camera for leg/behind shots, _ballAngle 90-270, on the bowling follower) was a band-aid for
                // the spin/beaten-ball divergence jerk. With deterministic flight + synced keeper/fielders the
                // ball no longer diverges, and that guard left the visible gameplay camera with NOTHING
                // following it (only the disabled side cams got SmoothFollowLookAt) → the camera STOPPED
                // rotating mid-shot on the bowling side. Removed: switch to the side camera normally (same as
                // the other camera path below), so the bowling camera follows the shot like the batting side.
                // FRAME THE SHOT FROM A KNOWN FOV. Three different paths write these cameras' fieldOfView and they
                // disagree: ResetAll parks both at 50 per delivery, the keeper view drops one to 35, and the
                // distance-follow curve (45 - dist/2) drives it as low as its floor of 12. The LIVE follow used
                // below never writes it at all, so whatever ran last simply carried into the shot — CamDiag caught
                // exactly that: 38.2 and 30.7 at selection where 50 was intended, on BOTH clients identically, i.e.
                // every shot starting already zoomed in (tester: "camera bht close/zoom ho jata"). Assert the
                // intended framing at the moment the shot takes the camera; the follow behaviour after that is
                // unchanged, and ResetAll's own 50 stays as the between-delivery default.
                if (_ballAngle >= 90f && _ballAngle <= 270f)
                {
                    leftFieldCamera.enabled = true;
                    leftFieldCamera.fieldOfView = SIDE_CAMERA_SHOT_FOV;
                }
                else
                {
                    rightFieldCamera.enabled = true;
                    rightFieldCamera.fieldOfView = SIDE_CAMERA_SHOT_FOV;
                }
                isSideCameraSelected = true;
                gameplayCamera.enabled = false;
                // "camera bahut close/zoom ho jata hai" — there is NO camera logging anywhere, which is why this
                // has stayed open across rounds. The side camera the shot switches to keeps whatever FOV it was
                // last left with: this LIVE block never writes fieldOfView (it only follows), while the REPLAY
                // block drives it down a distance curve to a floor of 12 and the keeper path parks it at 35 —
                // against a normal 50. So the FOV AT SELECTION is the whole question: ~50 means the framing is
                // correct here and the zoom comes from somewhere later; a low value means it leaked in from a
                // previous replay/keeper view and this shot inherited it. One line per shot, not per frame.
                if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                {
                    ConstantsData_M.MpLog($"[CamDiag] Side camera selected: {( _ballAngle >= 90f && _ballAngle <= 270f ? "LEFT" : "RIGHT")} ballAngle={_ballAngle:F1}"
                        + $" leftFOV={(leftFieldCamera != null ? leftFieldCamera.fieldOfView.ToString("F1") : "<null>")}"
                        + $" rightFOV={(rightFieldCamera != null ? rightFieldCamera.fieldOfView.ToString("F1") : "<null>")}"
                        + $" replay={isReplayModeActive} onBoundaryLine={isBallOnBoundaryLine} shot={currentShotPlayed} amBatting={(CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)}");
                }
            }
            // Chowka-camera fix (morning#3 / v2#2): on a boundary the ball travels well past the old 90u cutoff,
            // AND the (!isBallOnBoundaryLine || bounceCount<=0) clause FROZE the follow the instant the ball
            // bounced near the rope — so the camera stopped while the four was still rolling out. Extend the LIVE
            // side-cam follow distance and keep tracking the ball all the way to/over the boundary
            // (SmoothFollowLookAt is damped → no whip). Live block only; the replay block keeps the 90u cap
            // because its FOV-by-distance formula (45 - mag/2) goes negative past ~90.
            // Over-swing fix (tester: "back-boundary fix ke baad baaki shots ka camera bhi kharab"): the 140u
            // extension only makes sense for a REAL boundary — for every other shot it made the camera keep
            // swinging to track a medium-length ball to 140u instead of the original 90u. Only extend to 140u
            // once the ball is actually at the boundary line (a four/six); otherwise keep the original 90u so
            // normal shots frame exactly as before.
            float _followMax = isBallOnBoundaryLine ? 140f : 90f;
            //float magnitude = (ballTransform.position - rightSideCamTransform.position).magnitude;
            float magnitude = (temporaryPosition - rightCamTransform.position).magnitude;
            if (magnitude > 25f && magnitude < _followMax)
            {
                SmoothFollowLookAt(rightCamTransform, _camFollowProxy);
            }
            //float magnitude2 = (ballTransform.position - leftSideCamTransform.position).magnitude;
            float magnitude2 = (temporaryPosition - leftCamTransform.position).magnitude;
            if (magnitude2 > 25f && magnitude2 < _followMax)
            {
                SmoothFollowLookAt(leftCamTransform, _camFollowProxy);
            }
            return;
        }
        //if (ballTransform.position.z > outOfPitchZPos && ballStatus == "bowling" && !cameraToKeeper)
        if (temporaryPosition.z > outOfPitchZPosition && currentBallStatus == "bowling" && !isCameraFocusedOnKeeper)
        {
            isCameraFocusedOnKeeper = true;
            HideBowlingSpot();   // White-spot fix (morning#2): clear the white pitch disc on the replay path too.
            ActivateStadiumAndSkybox(boolean: true);
            SetCurrentBatsmanAnimSpeed(1f);
            if (!(wkOppositeLength > 1f) || UnityEngine.Random.Range(0, 10) > 5)
            {
            }
            if (isBatsmanConfident)
            {
                DecreaseConfidenceLevel(currentShotPlayed);
            }
        }
        else if (currentBallStatus == "shotSuccess" && !isMainCameraInTopDownView && currentBallStatus != "bowled" && !isSlipShot)
        {
            isMainCameraInTopDownView = true;
            topDownViewActivationTime = Time.time;
        }
        if (isSlipShot && !isBallPickedByFielder)
        {
            mainCamTransform.position += new Vector3(0f, 0f, 3f * Time.deltaTime);
            mainCamTransform.LookAt(raycastAnchorBallTransform);
        }
        else if (isSlipShot && isBallPickedByFielder)
        {
            mainCamTransform.position -= new Vector3(0f, 0f, 3f * Time.deltaTime);
            mainCamTransform.LookAt(raycastAnchorBallTransform);
        }
        if (!isMainCameraInTopDownView)
        {
            return;
        }
        if (Time.time < topDownViewActivationTime + topDownViewZoomDuration)
        {
            mainCamTransform.position = new Vector3(BowlingFollowCamX(_camFollowTarget.x, mainCamTransform.position.x), mainCamTransform.position.y, mainCamTransform.position.z);
            mainCamTransform.position += new Vector3(0f, 3f * Time.deltaTime, 28f * Time.deltaTime);
            mainCamTransform.eulerAngles += new Vector3(17f * Time.deltaTime, 0f, 0f);
            if (CONTROLLER.cameraType == 0)
            {
                gameplayCamera.fieldOfView += 40f * Time.deltaTime;
            }
            else
            {
                gameplayCamera.fieldOfView += 25f * Time.deltaTime;
            }
            SmoothFollowLookAt(mainCamTransform, _camFollowProxy);
            return;
        }
        if (CONTROLLER.cameraType == 0)
        {
            Singleton<MainCameraController>.instance.StartFollowTween();
            return;
        }
        if (currentBallStatus == "shotSuccess" && !isSideCameraSelected)
        {
            if (_ballAngle >= 90f && _ballAngle <= 270f)
            {
                leftFieldCamera.enabled = true;
                // Same known-FOV assert as the other selection site. Here the distance curve below usually takes
                // over immediately, but only once the ball is past 25u — on a shot that stays short nothing writes
                // the field and the previous ball's zoom would otherwise stand.
                leftFieldCamera.fieldOfView = SIDE_CAMERA_SHOT_FOV;
            }
            else
            {
                rightFieldCamera.enabled = true;
                rightFieldCamera.fieldOfView = SIDE_CAMERA_SHOT_FOV;
            }
            isSideCameraSelected = true;
            gameplayCamera.enabled = false;
        }
        // Boundary-REPLAY freeze fix (tester #2, back-boundary jerk): the LIVE block above already follows a
        // boundary ball to 140u with the rope-freeze clause removed — this replay branch still froze at the
        // rope (90u cap + "!isBallOnBoundaryLine || bounceCount<=0" freeze), only because its FOV-by-distance
        // formula (45 - mag/2) goes NEGATIVE past ~90u. Clamp the FOV to a floor instead and track the
        // replayed boundary all the way out, matching the live camera. Same isBallOnBoundaryLine gating as
        // live, so non-boundary replays keep the original 90u framing.
        float _replayFollowMax = isBallOnBoundaryLine ? 140f : 90f;
        float magnitude3 = (temporaryPosition - rightCamTransform.position).magnitude;
        if (magnitude3 > 25f && magnitude3 < _replayFollowMax)
        {
            rightFieldCamera.fieldOfView = Mathf.Max(12f, 45f - magnitude3 / 2f);
            SmoothFollowLookAt(rightCamTransform, _camFollowProxy);
        }
        float magnitude4 = (temporaryPosition - leftCamTransform.position).magnitude;
        if (magnitude4 > 25f && magnitude4 < _replayFollowMax)
        {
            leftFieldCamera.fieldOfView = Mathf.Max(12f, 45f - magnitude4 / 2f);
            SmoothFollowLookAt(leftCamTransform, _camFollowProxy);
        }
    }

    public void RotateUltraMotionCameraBatsmanCelebration()
    {
        ultraMotionCameraTransform.LookAt(new Vector3(batsmanReferencePoint.position.x, batsmanReferencePoint.position.y - 1f, batsmanReferencePoint.position.z));
        if (batsmanHand == "right")
        {
            ultraMotionCameraTransform.RotateAround(_batsmanTransform.position, -Vector3.down, 20f * Time.deltaTime);
        }
        else
        {
            ultraMotionCameraTransform.RotateAround(_batsmanTransform.position, Vector3.up, 20f * Time.deltaTime);
        }
    }

    private void ActivateReplayCamera()
    {
        replayViewCamera.enabled = true;
        rightFieldCamera.enabled = false;
        leftFieldCamera.enabled = false;
        showPreviewCamera(status: false);
        umpireViewCamera.enabled = false;
        closeUpViewCamera.enabled = false;
        gameplayCamera.enabled = false;
    }

    public void SetReplayCamera()
    {
        int num = 0;
        num++;
        if (num > 5)
        {
            num = 1;
        }
        AssignTrace(string.Empty + num);
        float z = fielder10SkinTransform.position.z;
        if (batsmanHand == "right")
        {
            replayViewCamera.gameObject.transform.position = new Vector3(10f, 4.5f, z + 15.5f);
            replayCamTransform.LookAt(fielder10SkinTransform.position);
        }
        else
        {
            replayViewCamera.gameObject.transform.position = new Vector3(-10f, 4.5f, z + 15.5f);
            replayCamTransform.LookAt(fielder10SkinTransform.position);
        }
    }

    public void disableCamOnShowScore()
    {
        introCutsceneCamera.enabled = false;
        gameplayCamera.enabled = true;
    }

    public Vector3 UpdateCameraPosition()
    {
        return gameplayCamera.WorldToScreenPoint(batsmanReferencePoint.position);
    }

    public Vector3 getPerspectiveCamPos()
    {
        return gameplayCamera.gameObject.transform.position;
    }

    private void ReplayCameraMovement()
    {
        if (!isReplayModeActive)
        {
            return;
        }
        if (!CONTROLLER.stumpingAttempted)
        {
            if (replayActionStatus == "follow")
            {
                if (currentActionState == 3 || currentActionState == 4)
                {
                    if (!isBallOnBoundaryLine)
                    {
                        replayControllerTransform.position = raycastAnchorBallTransform.position;
                        replayControllerTransform.eulerAngles = raycastAnchorBallTransform.eulerAngles;
                    }
                    else
                    {
                        replayControllerTransform.eulerAngles += new Vector3(0f, replayCameraBoundaryRotationAngle * Time.deltaTime, 0f);
                    }
                }
                if (currentActionState == 3)
                {
                    replayControllerTransform.position = new Vector3(0f, replayControllerTransform.position.y, replayControllerTransform.position.z);
                    replayControllerTransform.eulerAngles = new Vector3(replayControllerTransform.eulerAngles.x, 0f, replayControllerTransform.eulerAngles.z);
                }
            }
            else if (replayActionStatus == "lookAt")
            {
                replayCamTransform.LookAt(replayControllerTransform);
            }
            else if (replayActionStatus == "bowledSlowDown")
            {
                replayCameraController.enabled = false;
            }
        }
        else if (CONTROLLER.stumpingAttempted)
        {
            UpdateBatShadow();

            if (temporaryPosition.z < 8.8f)
            {
                replayCamTransform.LookAt(matchBallTransform);
            }
        }
    }

    public void hideAllCamera()
    {
        iTween.Stop(gameplayCamera.gameObject);
        gameplayCamera.enabled = true;
        mainCamTransform.position = new Vector3(-30f, 6.8f, 0f);
        mainCamTransform.eulerAngles = new Vector3(10f, 90f, 0f);
        gameplayCamera.fieldOfView = 45f;
        rightFieldCamera.enabled = false;
        leftFieldCamera.enabled = false;
        showPreviewCamera(status: false);
        umpireViewCamera.enabled = false;
        introCutsceneCamera.enabled = false;
        slowMotionCamera.enabled = false;
        closeUpViewCamera.enabled = false;
        replayViewCamera.enabled = false;
        rightBoundaryCamera.enabled = false;
        leftBoundaryCamera.enabled = false;
    }

    private void SetUmpireCameraPosition(string umpire)
    {
        _mainUmpireTransform.position = mainUmpireInitialPosition;
        if (umpire == "MainUmpire")
        {
            int num = UnityEngine.Random.Range(0, 7);
            if ((num == 0 || num == 1) && (_batsmanTransform.position.z < -10f || _runnerTransform.position.z < -10f))
            {
                num = 6;
            }
            switch (num)
            {
                case 0:
                    umpireCamTransform.position = new Vector3(-4.5f, 3f, -10f);
                    umpireCamTransform.eulerAngles = new Vector3(15f, 144f, 0f);
                    umpireViewCamera.fieldOfView = 20f;
                    break;
                case 1:
                    umpireCamTransform.position = new Vector3(4.5f, 3f, -10f);
                    umpireCamTransform.eulerAngles = new Vector3(15f, 215f, 0f);
                    umpireViewCamera.fieldOfView = 20f;
                    break;
                case 2:
                    umpireCamTransform.position = new Vector3(0.8f, 1f, -11f);
                    umpireCamTransform.eulerAngles = new Vector3(358f, 188f, 0f);
                    umpireViewCamera.fieldOfView = 18f;
                    break;
                case 3:
                    umpireCamTransform.position = new Vector3(-0.8f, 1f, -11f);
                    umpireCamTransform.eulerAngles = new Vector3(358f, 170f, 0f);
                    umpireViewCamera.fieldOfView = 18f;
                    break;
                case 4:
                    umpireCamTransform.position = new Vector3(-5f, 2f, -13f);
                    umpireCamTransform.eulerAngles = new Vector3(6f, 120f, 0f);
                    umpireViewCamera.fieldOfView = 20f;
                    break;
                case 5:
                    umpireCamTransform.position = new Vector3(5f, 2f, -13f);
                    umpireCamTransform.eulerAngles = new Vector3(6f, 237f, 0f);
                    umpireViewCamera.fieldOfView = 20f;
                    break;
                case 6:
                    umpireCamTransform.position = new Vector3(0f, 1.3f, -12f);
                    umpireCamTransform.eulerAngles = new Vector3(0f, 180f, 0f);
                    umpireViewCamera.fieldOfView = 25f;
                    break;
            }
            iTween.MoveTo(umpireViewCamera.gameObject, iTween.Hash("y", umpireCamTransform.position.y + 0.2f, "time", 2, "easetype", "easeInOutSine"));
        }
        else if (!(umpire == "SideUmpire"))
        {
        }
    }

}

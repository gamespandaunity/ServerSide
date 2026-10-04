using UnityEngine;
using Cricket;
public class BowlerController : MonoBehaviour
{
    private GroundController ballScript;

    private void Start()
    {
        ballScript = GameObject.Find("GroundController").GetComponent<GroundController>();
    }

    private void TriggerCameraZoom()
    {
        ballScript.ZoomCameraToPitch();
    }

    private void FreezeTheBowlingSpot()
    {
        ballScript.FreezeTheBowlingSpot();
    }

    private void ReleaseTheBall()
    {
        // In live multiplayer, the batting client must NOT trigger a local ball release
        // from the bowler animation event. The bowling player sends CmdSyncBallRelease
        // which initialises ball state on the batting side via RPC_SyncBallRelease.
        // Replays are local-only simulations, so the batting replay must allow this event.
        // Without this guard: batting client calls ReleaseTheBall() with its own (wrong)
        // bowling parameters, THEN receives RPC_SyncBallRelease → ball appears thrown twice.
        if (CONTROLLER.PlayModeSelected == 8
            && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
            && !CONTROLLER.ReplayShowing)
        {
            // Locked delivery plan: the release params+seed arrived BEFORE the run-up, so the batting
            // client launches at its OWN anim event — perfect throw-to-ball sync, no release-time wait.
            ballScript.TryLaunchFromLockedPlan();
            return;
        }

        //if(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && (CONTROLLER.OpponentTutorial||CONTROLLER.tutorialToggle == 1))
        if (CONTROLLER.PlayModeSelected == 18 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex && (CONTROLLER.OpponentTutorial || CONTROLLER.tutorialToggle == 1))
        {
            Time.timeScale = 0;
            Singleton<GameData>.instance.opponentWatchingTutorialPanel.SetActive(true);
            Singleton<Tutorial>.instance.CallRPCBowlingSpotInfo();
        }
        else
        {
            //if(CONTROLLER.PlayModeSelected == 8 && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
            //{
            //	return;
            //}
            ballScript.ReleaseTheBall();
        }
    }

    private void HideBowler()
    {
    }
}

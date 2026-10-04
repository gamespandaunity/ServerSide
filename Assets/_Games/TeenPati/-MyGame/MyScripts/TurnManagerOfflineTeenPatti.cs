using TeenPattiGame;
using UnityEngine;
using UnityExtensions;

public class TurnManagerOfflineTeenPatti : MonoBehaviour
{

    public static TurnManagerOfflineTeenPatti Instance;


    void Awake()
    {
        Instance = this;
    }
    // public float RemainingSecondsInTurn;
    // public float TurnDuration;
    // public float localDuration;

    // public int Turn;


    // public void CmdBeginTurn()
    // {
    //     Turn = this.Turn + 1;
    //     TurnDuration = 15f;
    // }
    // public void CmdResetTurn()
    // {
    //     Turn = 1;
    // }
    // public void CmdSetDuration(float Duration, bool isRpc)
    // {
    //     this.TurnDuration = Duration;
    //     if (Duration <= 0)
    //     {
    //         RpcOnTimerEnd();
    //     }
    //     if (isRpc)
    //         RpcSetLocalDuration(Duration);
    // }
    // public void SetDuration(float Duration)
    // {
    //     if (Duration <= 0)
    //     {
    //         RpcOnTimerEnd();
    //     }

    // }
    // public void RpcOnTimerEnd()
    // {
    //     PlayerTurnManager.Instance.OnTurnTimeEnds(Turn);
    // }
    // public void RpcSetLocalDuration(float duration)
    // {
    //     localDuration = duration;
    // }
    // public void CmdSetTimer(int Timer)
    // {
    //     this.RemainingSecondsInTurn = Timer;
    // }

    public void OutOfTheTableMethod()
    {
        this.Delay(1f, () =>
                {
                    PlayerStateManager.Instance.PlayingList.Count.Show("Playing List Count After Delay");
                    if (RoomStateManager.Instance.CurrentRoomState == RoomState.STATE.GameIsPlaying)
                        if (PlayerStateManager.Instance.PlayingList.Count <= 1)
                            PlayerStateManager.Instance.RemainingPlayerWonGameAutomatic();


                });
    }

    public void TurnOnBottomAmountAfterDelay()
    {
        this.Delay(2f, () =>
        {
            PlayerStateManager.Instance.Amountobject.SetActive(true);
            "Show hoga bottom object".Show();

        });
    }
}

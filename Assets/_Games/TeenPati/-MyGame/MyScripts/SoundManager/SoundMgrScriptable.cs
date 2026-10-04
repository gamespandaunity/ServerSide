using UnityEngine;
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/SoundMgrScriptable", order = 1)]
public class SoundMgrScriptable : ScriptableObject
{
    public GameObject AudioSourcePrefab;
    public AudioClip BGMusic,
        ButtonSound,
        CardFlip,
        ChipAdding,
        Notification,
        Reward,
        WinFinal,
        ToonSound,
        WingoLotteryRotate,
        DragonSound,
        CameraShutterSound,
        TigerSound,
        DailyReward,
        ChipsCollect,
        GoldCollectFromSenderTick,
        MainMenuGoldWin,
        MainMenuGoldLoose,
        ClockTikSound;

}
public class Enums
{

}


public enum AdLocation
{
    GameStart,
    GameOver,
    LevelComplete,
    Pause,
    FacebookFriends,
    GameFinishWindow,
    StoreWindow,
    GamePropertiesWindow

};

public enum MyGameType
{
    TwoPlayer, FourPlayer, Private
};

public enum MyGameMode
{
    Classic, Master, Quick
}

public enum EnumPhoton
{
    BeginPrivateGame = 171,
    NextPlayerTurn = 172,
    StartWithBots = 173,
    StartGame = 174,
    FinishedGame = 178,
    ReconnectGame = 181,
    ReadyToPlay = 179,
    ExitGame = 180,
    SaveBoardState = 182,   // real player client → server: latest board snapshot for reconnect
}

public enum EnumGame
{
    DiceRoll = 50,
    PawnMove = 51,
    PawnRemove = 52,
}
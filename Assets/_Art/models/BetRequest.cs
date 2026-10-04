[System.Serializable]
public class BetRequest
{
    public int user_id;
    public int first_player;
    public string second_player;
    public string game_id;
    public string gold;
    public string silver;
    public string status;
    public string remark;

    public string second_player_info;
    public string screenstatus;
    public string message;
}

[System.Serializable]

public class BetJoinRequest
{
    public string transaction_id;
    public int second_player;
    public string second_player_info;
}
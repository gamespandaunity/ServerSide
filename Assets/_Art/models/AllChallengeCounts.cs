using System.Collections.Generic;
[System.Serializable]
public class AllChallengeCounts
{

    public bool status;
    public List<GameCount> game_count;
    public string message;
    

}
[System.Serializable]
public class GameCount
{
    public string _id;
    public string game_id;
    public int active_silver;
    public int active_gold;
    public int inactive_silver;
    public int inactive_gold;
}

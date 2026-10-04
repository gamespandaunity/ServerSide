using UnityEngine;

public class TurnamentPlayers : MonoBehaviour
{
    [SerializeField] private TurnamentPlayer player1;
    [SerializeField] private TurnamentPlayer player2;

    [SerializeField] private TurnamentPlayers turnamentPlayers;

    public void SetToParent(TurnamentPlayer player)
    {
        turnamentPlayers.Set(player);
    }
    private void Set(TurnamentPlayer player)
    {
        if (!player1.isFull)
            player1.Set(player);
        else
            player2.Set(player);
    }

    public void Set(string playerName, Texture image)
    {
        if (!player1.isFull)
            player1.Set(playerName, image);
        else
            player2.Set(playerName, image);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class TurnamentManager : MonoBehaviour 
{
    [SerializeField] private TurnamentPlayers[] turnamentPlayers;
    private List<TurnamentPlayers> currentTurPlayers;
    public bool IsStarted { get; private set; }
    public bool IsReady
    {
        get { return !IsStarted && currentTurPlayers != null && currentTurPlayers.Count == turnamentPlayers.Length; }
    }
    public void Reset()
    {
        foreach (var item in GetComponentsInChildren< TurnamentPlayer>())
        {
            item.Reset();
        }
        currentTurPlayers = new List<TurnamentPlayers>(0);
        IsStarted = false;
    }

    public void Add(string playerName, Texture playerTexture)
    {
        if (IsStarted)
        {
            return;
        }
        if (currentTurPlayers == null)
        {
            Reset();
        }
        if(currentTurPlayers.Count == turnamentPlayers.Length)
        {
            return;
        }
        TurnamentPlayers cPlayer = turnamentPlayers[currentTurPlayers.Count];
        cPlayer.Set(playerName, playerTexture);
        currentTurPlayers.Add(cPlayer);
    }

    public void StartGame()
    {
        if(!IsReady)
        {
            return;
        }
        IsStarted = true;
    }

}

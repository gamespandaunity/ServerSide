using NetworkManagement;

/// <summary>
/// Network and game adapter.
/// </summary>
public interface NetworkGameAdapter
{

    HomeMenuManager homeMenuManager
    {
        get;
    }
    void SetTurn(int turnId);
    void OnMainPlayerLoaded (int playerId, string name, int coins, object avatar, string avatarURL, int prize);
	void OnUpdateMainPlayerName (string name);
	void OnUpdatePrize (int prize);
	void OnUpdateOver (int Over);
	void OnGoToPlayWithAI (int playerId, string name, int coins, object avatar, string avatarURL);
    
}

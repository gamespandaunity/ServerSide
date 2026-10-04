using UnityEngine;
using UnityEngine.UI;
using BallPool;

public class PlayAgainMenu : MonoBehaviour 
{
    public GameObject winPanel;
    [SerializeField] private Text winnerName;
    [SerializeField] private Text winnerCoins;
    [SerializeField] private RawImage winnerImage;
    [SerializeField] private RectTransform playAgainButton;




    private bool _wasOpened = false;
    public bool wasOpened { get { return _wasOpened; } }

    public void HidePlayAgainButton()
    {
        playAgainButton.gameObject.SetActive(false);
    }
    public void Hide()
    {
        winPanel.SetActive(false);
    }
    public void Show(BallPoolPlayer player)
    {
     //   winnerName.text = player.name;
      //  winnerImage.texture = (Texture2D)player.avatar;
      //  winnerCoins.text = player.coins + "";
        winPanel.SetActive(true);
        //Debug.Log("Calling from here show paly again menu");
        _wasOpened = true;
    }

    public void ShowMainPlayer()
    {
        HidePlayAgainButton();
        Show(BallPoolPlayer.mainPlayer);
    }
    private void Start()
    {
       // //print("Game finished");
    }
}

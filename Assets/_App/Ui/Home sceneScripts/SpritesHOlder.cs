using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Sprites", menuName = "Sprites/GenericSprites")]

public class SpritesHOlder : ScriptableObject
{

    public Sprite goldenCoin, silvercoin, silverChest, goldChest;
    public Sprite[] GameLogos;
    public Sprite silverHeader, goldheader;
  
    public Texture2D nullProfileImg, aiImg;
    public Sprite coinsInSilver, coinsInGold;

    public GameObject CreateChallenge_coinTransfer, waitingForOpponentPanel;
    public Sprite[] Bgs;
    public Sprite[] downloadedGamePics;
    [Header("Gold  / Silver  panel for create challenges")]
    public Sprite goldPanel;
    public Sprite silverPanel;
    [Header("Gold  / Silver  create challenges button")]
    public Sprite goldbtn;
    public Sprite silverbtn;

    [Tooltip("DONT DELETE IF DONT DOWNLOAD FROM SERVER")] public List<Texture2D> serverLogo = new List<Texture2D>();
    
    public void LogoChangerWith_GameId(Image logo)
    {
        //ConstantsData_M.Log("Game Id" +(ApiAndRoomManager.currentGameId - 1));  
       logo.sprite = GameLogos[ApiAndRoomManager.currentGameId-1];
    }
    public void CreateButton_ImgSetter(Image button)
    {
        if (staticVariables.isgoldcoins)
        {
            button.sprite = goldbtn;
        }

        else
        {
            button.sprite = silverbtn;
        }
    }
    public void CoinSpriteChangerAccordingToDecision(Image coin)
    {

        if (staticVariables.isgoldcoins)
        {
            coin.sprite = goldenCoin;
        }

        else
        {
            coin.sprite = silvercoin;
        }
    }
    public void HeaderSpriteChanger(Image header)
    {
        if (staticVariables.isgoldcoins)
        {
            header.sprite = goldheader;
        }
        else
        {
            header.sprite = silverHeader;
        }
    }
    public void BgHandlerAccordingTo_Id(Image bg)
    {
        bg.sprite = Bgs[ApiAndRoomManager.currentGameId-1];


    }
    public void CreateCHallenge_SetPanel_Img(Image img)
    {
        if (staticVariables.isgoldcoins)
        {
            img.sprite = goldPanel;
        }
        else
        {
            img.sprite = silverPanel;

        }
    }
}

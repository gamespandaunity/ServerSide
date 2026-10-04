using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
public class UpdaterChallenge : MonoBehaviour
{
    public Image coin, border;
    public RawImage img;
    public string str;
    public bool isYourChallenge= false;
   public PlayerProfileUI playerUi;
    private void OnEnable()
    {
        SpritesManager.Instance.spritesScriptable.CoinSpriteChangerAccordingToDecision(coin);
        SpritesManager.Instance.spritesScriptable.HeaderSpriteChanger(border);
         if(gameObject.GetComponent<PlayerProfileUI>().enabled == false)
        {
            playerUi = gameObject.GetComponent<PlayerProfileUI>();
            playerUi.enabled = true;
        }
    }
    private void Start()
    {
        ServerConnection.DownloadSprite("/" + GetComponent<PlayerProfileUI>().player.imageURL
              , ImageTexture =>
              {
                  img.texture = ImageTexture;
              },
              Onfailed =>
              {
                  //Debug.Log("Failed To Load Image  with " + Onfailed);
                  img.texture = SpritesManager.Instance.spritesScriptable.nullProfileImg;
              }
              );


    }
   

    

}

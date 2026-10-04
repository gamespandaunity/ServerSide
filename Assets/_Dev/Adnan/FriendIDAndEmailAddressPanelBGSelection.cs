using UnityEngine;
using UnityEngine.UI;

public class FriendIDAndEmailAddressPanelBGSelection : MonoBehaviour
{
    public Sprite[] Borders;
    public Sprite[] logos;
    public Image InoutField;
    public Button seacrchbtn;
    public Sprite[] Coins;
    public Sprite[] CoinsShine;
    public Sprite[] bgImages;

    public Image borderImage, background, Inofcontainer_borderPanel;
    public Sprite[] borderSprite;
    public Borderspanel headerpanel;

    // Start is called before the first frame update
    void OnEnable()
    {
        headerpanel.OnEnable();
    

        if (staticVariables.isgoldcoins)
        {
            seacrchbtn.GetComponent<Image>().sprite = Borders[0];
            InoutField.sprite = Borders[0];

            borderImage.sprite = borderSprite[0];
            Inofcontainer_borderPanel.sprite = borderSprite[0];
        }
        else
        {
            seacrchbtn.GetComponent<Image>().sprite = Borders[1];
            InoutField.sprite = Borders[1];
  
            borderImage.sprite = borderSprite[1];
            Inofcontainer_borderPanel.sprite = borderSprite[1];
        }
        SpritesManager.Instance.spritesScriptable.BgHandlerAccordingTo_Id(background);
    }

  
}

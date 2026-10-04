using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
public class aightballUI : MonoBehaviour
{
    public Text totalCoinsText;
    public Image prizeBackground;
    public Sprite[] prizeBackgroundSprites;
    public Image[] ballHolders;
    public Sprite[] ballHolderSprites;
    public Image coinImage;
    public Sprite[] coinSprites;
    public Color[] coinTextColors;

    private void OnEnable()
    {
        if (staticVariables.isgoldcoins)
        {
            prizeBackground.sprite = prizeBackgroundSprites[0];
            ballHolders[0].sprite = ballHolderSprites[0];
            ballHolders[1].sprite = ballHolderSprites[0];
            coinImage.sprite = coinSprites[0];
            coinTextColors[0].a = 1f;


            totalCoinsText.color = coinTextColors[0];
        }
        else
        {
            prizeBackground.sprite = prizeBackgroundSprites[1];
            ballHolders[0].sprite = ballHolderSprites[1];
            ballHolders[1].sprite = ballHolderSprites[1];
            coinImage.sprite = coinSprites[1];
            coinTextColors[1].a = 1f;
            totalCoinsText.color = coinTextColors[1];
        }
    }

}

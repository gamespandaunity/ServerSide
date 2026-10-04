using UnityEngine;
using UnityEngine.UI;

public class TurnamentPlayer : MonoBehaviour
{
    public Text nameText;
    public RawImage avatar;
    public bool isFull { get; private set; }

    public void Set(string playerName, Texture image)
    {
        isFull = true;
        nameText.text = playerName;
        if(image)
        avatar.texture = image;
    }
    public void Set(TurnamentPlayer player)
    {
        isFull = true;
        Set(player.nameText.text, player.avatar.texture);
    }

    public void Reset()
    {
        isFull = false;
    }
}

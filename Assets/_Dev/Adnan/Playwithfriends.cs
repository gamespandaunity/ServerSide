using UnityEngine;
using UnityEngine.UI;
public class Playwithfriends : MonoBehaviour
{

    public Image border;
    public Sprite[] borders;
    void OnEnable()
    {
        if(staticVariables.isgoldcoins)
        {
            border.sprite = borders[0];
        }
        else
        {
            border.sprite = borders[1];
        }
    }

    
}

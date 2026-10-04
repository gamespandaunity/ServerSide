using NetworkManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class BrowseChallageUI : MonoBehaviour
{
    public Image createChallengeBorder;
    public Image createBtn;
    public Image browseChallengeBorder;
    public Image browseBtn;
    public Image crete;
    public Image line;

    public Sprite[] createChallengeBorders;
    public Sprite[] createBtns;
   // public Sprite[] browseChallengeBorders;
    
    public Sprite[] cretes;
    public Sprite[] lines;

   // public Image challegeForFriends;
    public Sprite[] challengeForFriendSprites;

    public Borderspanel headerpanel;

    // Start is called before the first frame update
    void OnEnable()
    {
        headerpanel.OnEnable();
        if (staticVariables.isgoldcoins)
        {
            createChallengeBorder.sprite = createChallengeBorders[0];
            createBtn.sprite = createBtns[0];
            browseChallengeBorder.sprite = createChallengeBorders[0];

            crete.sprite = cretes[0];
            line.sprite = lines[0];
        }
        else
        {
            createChallengeBorder.sprite = createChallengeBorders[1];
            createBtn.sprite = createBtns[1];
            browseChallengeBorder.sprite = createChallengeBorders[1];

            crete.sprite = cretes[1];
            line.sprite = lines[1];
        }
    }

}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectOver : MonoBehaviour
{
    public GameObject OverObject;
    public static int SelectedOver = 3;
    public Button threeOvers, fiveOvers;
    public Sprite ThreeUnselect,ThreeSelect ,fiveUnselect,fiveSelect;

    private void OnEnable()
    {
        //APIManager.gameid.Show("GAME ID");
        if (ApiAndRoomManager.currentGameId == 13)
        {
            OverObject.SetActive(true);
            SelectOvers(0);
        }
        else
        {
            OverObject.SetActive(false);

        }
    }
    public void SelectOvers(int over)
    {
        SelectedOver = over;
        if (SelectedOver == 0)
        {
            threeOvers.GetComponent<Image>().sprite = ThreeSelect;
            fiveOvers.GetComponent <Image>().sprite = fiveUnselect;
        }
        else
        {
            threeOvers.GetComponent<Image>().sprite = ThreeUnselect;
            fiveOvers.GetComponent<Image>().sprite = fiveSelect;
        }
        HomeMenuManager.instance.UpdateOverStatus(over);
    }
}

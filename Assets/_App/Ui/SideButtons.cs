using NetworkManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SideButtons : MonoBehaviour
{
    public List<Image> sideButtons = new List<Image>();
    public List<GameObject> ContentsData = new List<GameObject>();
    public Sprite SelectedButton, UnSelectedButton;
    public RectTransform BarLine;
    public TextMeshProUGUI OpenChallengecountText, MyChallengeCountText;
    public delegate void UpdateRoomsCount(string count);
    public static UpdateRoomsCount _updateRoomCount, myRoomCount;
    public Borderspanel topHeader;
    private void Start()
    {
        myRoomCount = null;
        myRoomCount += UpdateMyChallengesCount;
    }
    private void OnEnable()
    {
        _updateRoomCount = null;
        _updateRoomCount += UpdateOpnChallengesCount;

        topHeader.headerText.text = "CHALLENGES";
        if (Borderspanel.coinSelected != null)
        {
            Borderspanel.coinSelected(staticVariables.isgoldcoins);
        }
        sideButtons[0]?.GetComponent<Button>().onClick.Invoke();
        //if (staticVariables.isgoldcoins)
        //{
        //    string myListCount = RoomsListManager.myChallengeGold.Count > 99 ? "99+" : RoomsListManager.myChallengeGold.Count.ToString();
        //    UpdateMyChallengesCount(myListCount);
        //}
        //else
        //{
        //    string myListCount = RoomsListManager.myChallengeSilver.Count > 99 ? "99+" : RoomsListManager.myChallengeSilver.Count.ToString();
        //    UpdateMyChallengesCount(myListCount);

        //}
    }

    private void LateUpdate()
    {
        updateChildCount();
    }
    int lastOpenChallengecCount = -1;
    int lastMyChallengecCount = -1;
    void updateChildCount()
    {
        if (RoomsListManager.instance.BrowseOthersRoomsContent.transform.childCount != lastOpenChallengecCount)
        {
            lastOpenChallengecCount= RoomsListManager.instance.BrowseOthersRoomsContent.transform.childCount;
            OpenChallengecountText.text = GetListCount(RoomsListManager.instance.BrowseOthersRoomsContent.transform);
        }
        if (RoomsListManager.instance.BrowseYourRoomsContent.childCount != lastMyChallengecCount)
        {
            lastMyChallengecCount = RoomsListManager.instance.BrowseYourRoomsContent.childCount;
            MyChallengeCountText.text = GetListCount(RoomsListManager.instance.BrowseYourRoomsContent.transform);
        }
    }
    public void SelectGameobject(Image SelectedObject)
    {
        for (int i = 0; i < sideButtons.Count; i++)
        {
            if (sideButtons[i] == SelectedObject)
            {
                sideButtons[i].sprite = SelectedButton;
                BarLine.SetAsLastSibling();
                sideButtons[i].rectTransform.localScale = new Vector3(1.13f, sideButtons[i].rectTransform.localScale.y, sideButtons[i].rectTransform.localScale.z);
                sideButtons[i].rectTransform.SetAsLastSibling();
                ContentsData[i].SetActive(true);
                topHeader.headerText.text = sideButtons[i].gameObject.name;
            }
            else
            {
                sideButtons[i].sprite = UnSelectedButton;

                //sideButtons[i].rectTransform.anchoredPosition = new Vector2(0, 0);
                sideButtons[i].rectTransform.localScale = new Vector3(1, sideButtons[i].rectTransform.localScale.y, sideButtons[i].rectTransform.localScale.z);
                ContentsData[i].SetActive(false);

            }
        }
        SoundManagerMain.instance.ClickSoundPlay();
    }
    public string GetListCount(Transform content)
    {
        if (content.childCount > 99)
        {
            return "99+";
        }
        else
        {
            return content.childCount.ToString();
        }
    }
    public void UpdateMyChallengesCount(string myCount)//button function
    {
        //print("my Count"+ myCount);

        MyChallengeCountText.text = myCount;
    }
    public void UpdateOpnChallengesCount(string count)//button function
    {
        OpenChallengecountText.text = string.Empty;
        OpenChallengecountText.text = "";
        OpenChallengecountText.text = count;
    }
    private void OnDisable()
    {
        _updateRoomCount = null;
        _updateRoomCount -= UpdateOpnChallengesCount;
    }
}

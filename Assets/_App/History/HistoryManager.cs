using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HistoryManager : MonoBehaviour
{
    public Button firstBtn;
    public Button nextBtn;
    public Button lastBtn;
    public Button previousBtn;
    public Text currentPage;
    public Text lastPage;
    public GameObject[] historyButtons;
    public GameObject[] historyPanels;
    public Sprite[] SelectedSprite;
    public Sprite[] UnSelectedSprite;
    public int currentpageNumber = 1;
    public GameObject Headings;
    private void OnEnable()
    {
        if (Borderspanel.UnselectCoin != null)
            Borderspanel.UnselectCoin(false);

        if (Borderspanel.logoHandler != null)
        {
            Borderspanel.logoHandler(false);
        }
        if (Borderspanel.ChangeTitle!= null)
        {
            Borderspanel.ChangeTitle("");
        }
        historyButtons[0].GetComponent<Button>().onClick.Invoke();
    }
    public void HistoryButtonSelecction(Button btn)
    {
        for (int i = 0; i < historyButtons.Length; i++)
        {
            historyButtons[i].transform.SetAsFirstSibling();
            if (btn.gameObject == historyButtons[i])
            {
                historyPanels[i].SetActive(true);
                historyButtons[i].GetComponent<Image>().sprite = SelectedSprite[i];

            }
            else
            {
                historyPanels[i].SetActive(false);
                historyButtons[i].GetComponent<Image>().sprite = UnSelectedSprite[i];

            }
        }
        btn.transform.SetAsLastSibling();
    }
    public void ReturnBtn()
    {
        SceneLoaderUtility.LoadScene("MainmenuScene");
    }
    public void ToggleHeading(bool activate)
    {
        Headings.gameObject.SetActive(activate);
    }
    public void setFirstPage()
    {
        if (currentpageNumber != 1)
        {
            currentpageNumber = 1;
            //        APIManager.instance.fetchgamehistoryTransection();
        }



    }
    public void setLastPage()
    {
        if (staticVariables.gameHistoryObj != null)
        {
            //      APIManager.instance.fetchgamehistoryTransection();
        }

    }
    public void setpreviousPage()
    {
        //   if (int.Parse(staticVariables.gameHistoryObj.currentPage) != 1)
        //   {
        //       currentpageNumber--;
        ////       APIManager.instance.fetchgamehistoryTransection();
        //   }
    }
    public void setnextPage()
    {
        // if (staticVariables.gameHistoryObj != null && staticVariables.gameHistoryObj.totalPages!= int.Parse(staticVariables.gameHistoryObj.currentPage))
        // {
        //     currentpageNumber++; 
        ////     APIManager.instance.fetchgamehistoryTransection();
        // }
    }
}

namespace POKER 
{
using POKER;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuBGUpdate : MonoBehaviour
{

    public MenuBGCollection menuBGCollection;
    public GameObject menuBGBtn;
    public List<Image> MainMenuBackGroundSprite = new List<Image>();
    public static int saveMenuBGIndex
    {
        get
        {
            return PlayerPrefs.GetInt("Menu_BackGround", 0);
        }
        set
        {
            PlayerPrefs.SetInt("Menu_BackGround", value);
        }
    }

    private List<GameObject> buttonList = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        CreateMenuShopBG();
        AssignMenuBG(saveMenuBGIndex);
        //Invoke(nameof(AddChips), 10f);
    }

   

    void CreateMenuShopBG()
    {
        for (int i = 0; i < menuBGCollection.menuBackgrounds.Length; i++)
        {
            GameObject bGBtn = Instantiate(menuBGBtn);
            LocalSettings.SetPosAndRect(bGBtn, menuBGBtn.GetComponentInChildren<RectTransform>(), menuBGBtn.transform.parent);
            bGBtn.GetComponent<Image>().sprite = menuBGCollection.menuBackgrounds[i].shopSprites;
            bGBtn.transform.GetChild(2).GetChild(0).GetComponent<TMP_Text>().text = menuBGCollection.menuBackgrounds[i].bgName;
            TMP_Text costText = bGBtn.transform.GetChild(1).GetChild(0).GetChild(0).GetComponentInChildren<TMP_Text>();
            
            int number = LocalSettings.GetMenuBgSaveIndex(i);
           
            if (number != 1 && i != 0)
            {
                costText.text = LocalSettings.Rs(menuBGCollection.menuBackgrounds[i].BgCost);
                costText.gameObject.transform.parent.parent.gameObject.SetActive(true);
            }
            else
            {
               // Debug.LogError("Check index of Button........."+number+" .." + LocalSettings.GetMenuBgSaveIndex(i));
                costText.gameObject.transform.parent.parent.gameObject.SetActive(false);
            }
            bGBtn.SetActive(true);
            buttonList.Add(bGBtn);
            // Adding button listener
            Button BtnAvat = bGBtn.GetComponentInChildren<Button>();
            BtnAvat.onClick.AddListener(() => OnMenuBGSelectBtnClick());
        }
    }

    void OnMenuBGSelectBtnClick()
    {
        int indexOfChild = EventSystem.current.currentSelectedGameObject.transform.GetSiblingIndex() - 1;
        if (LocalSettings.GetMenuBgSaveIndex(indexOfChild) == 1)
        {
            AssignMenuBG(indexOfChild);
              //  Debug.LogError("Check index of Button.........3 .." + indexOfChild);
        }
        else if (menuBGCollection.menuBackgrounds[indexOfChild].BgCost > LocalSettings.GetTotalChips())
        {
            Toaster.ShowAToast("You Could Not Have Enough Chips");
           
            return;
        }
        else
        {
            if (LocalSettings.GetMenuBgSaveIndex(indexOfChild) == 0)
            {
                BigInteger bgCost = menuBGCollection.menuBackgrounds[indexOfChild].BgCost;
               
                Debug.LogError("Bg Cost" + bgCost.ToString());
                LocalSettings.SetMenuBgSaveIndex(indexOfChild, 1);
                AssignMenuBG(indexOfChild);
                //Debug.LogError("Check index of Button.........1 .." + indexOfChild);


            }
           // Debug.LogError("Check index of Button.........4 .." + indexOfChild);
        }
    }



    void AssignMenuBG(int number)
    {
        foreach (Image item in MainMenuBackGroundSprite)
        {
            item.sprite = menuBGCollection.menuBackgrounds[number].menuBGSprites;
        }
        foreach (var item in buttonList)
        {
            item.transform.GetChild(0).gameObject.SetActive(false);
        }

        saveMenuBGIndex = number;
        buttonList[number].transform.GetChild(0).gameObject.SetActive(true);
        buttonList[number].transform.GetChild(1).gameObject.SetActive(false);
       // Debug.LogError("Check index of Button.........2 .." + number);
    }









}

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public GameObject[] ShopButtons;
    public GameObject[] ShopPanel;
    public TMP_Text shopCoinsText;

    // Start is called before the first frame update

    private void Start()
    {

    }

    private void OnEnable()
    {
        shopCoinsText.text = LocalSettings.Rs(LocalSettings.GetTotalChips());
    }


    public void OnClickShopButto(int index)
    {
        foreach (GameObject item in ShopButtons)
        {
            item.SetActive(false);
        }
        foreach (GameObject item in ShopPanel)
        {
            item.SetActive(false);
        }
        ShopButtons[index].SetActive(true);
        ShopPanel[index].SetActive(true);

    }




}

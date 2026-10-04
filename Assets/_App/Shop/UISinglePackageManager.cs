using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class UISinglePackageManager : MonoBehaviour
{
    // Start is called before the first frame update

    public Text goldQty;
    public Text dollarprice;
    public Text localCurrencyPrice;
    public Text localCurrencyCode;
    public GameObject payWithBankPanel, CryptoPaymentMethodPanel;

    private void OnEnable()    
    {     
        //packageName.text = staticVariables.shopPackage.title;
        //silverQty.text = staticVariables.shopPackage.silver_coin;
        goldQty.text = staticVariables.shopPackage.gold_coin;
        dollarprice.text = staticVariables.shopPackage.amount_usd;
        localCurrencyPrice.text = ((int)staticVariables.shopPackage.amount).ToString();
        localCurrencyCode.text = $"Price({staticVariables.shopPackage.currency})";
    }
    public void payWithBankAccount()
    {
       GameObject gb=  Instantiate(payWithBankPanel,HomeMenuManager.instance.mainCanvasObject.transform);
        if (!gb.activeSelf)
        {
            gb.SetActive(true);
        }
      
    }

    public void payWithCryptoAccount()
    {
      GameObject gb=  Instantiate(CryptoPaymentMethodPanel, HomeMenuManager.instance.mainCanvasObject.transform);
        if (!gb.activeSelf)
        {
            gb.SetActive(true);
        }
        // CryptoPaymentMethodPanel.SetActive(true);
    }


    public void returnToBackDashboard()
    {
        // gameObject.SetActive(false);
    }
 

}

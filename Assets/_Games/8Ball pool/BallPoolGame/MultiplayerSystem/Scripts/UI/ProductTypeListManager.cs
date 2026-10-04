using UnityEngine;
using UnityEngine.UI;

namespace NetworkManagement
{
    public delegate void SelecProductTypeHandler(ProductTypeUI productTypeUI);
    /// <summary>
    /// Product type list manager.
    /// </summary>
    public class ProductTypeListManager : ContentListManager
    {
        public static event SelecProductTypeHandler OnSelectProductType;

        public void UpdateProductsType(NetworkManagement.ProductType[] productsTypeList)
        {
            RessetOldButtons();

            if (productsTypeList == null || productsTypeList.Length == 0)
            {
                return;
            }
            for (int i = 0; i < productsTypeList.Length; i++)
            {
                //Button currentButton = GetCurrentButton(null);
                Button currentButton = null;

                ProductTypeUI currentProductTypeUI = currentButton.GetComponentInChildren<ProductTypeUI>();
                NetworkManagement.ProductType currentProductType = productsTypeList[i];

                currentProductTypeUI.SetProductType(currentProductType);
                NetworkManagement.Pointer currentPointer = currentButton.GetComponent<Pointer>();

                if (currentPointer && Application.isMobilePlatform)
                {
                    
                    currentPointer.OnSelecButton += (Pointer pointer) => 
                        {
                            if (OnSelectProductType != null)
                            {
                                OnSelectProductType(currentProductTypeUI);
                            }
                        };
                }
                else
                {
                    currentButton.onClick.AddListener(() =>
                        {
                            if (OnSelectProductType != null)
                            {
                                OnSelectProductType(currentProductTypeUI);
                            }
                        });
                }
                AddButton(currentButton.gameObject);
            }
        }
    }
}

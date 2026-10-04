using UnityEngine;

namespace NetworkManagement
{
    /// <summary>
    ///Products user interface manager, it meneg all products properties, using "ProductUITool".
    /// </summary>
    public abstract class ProductsUIManager : MonoBehaviour
    {
        [SerializeField] protected RectTransform[] productsGroupTranform;
        protected RectTransform currentProductsGroupTranform;
        private ProductUITool[] productsTranform;

        public void ShowGroupUI(int id)
        {
            for (int i = 0; i < productsGroupTranform.Length; i++)
            {
                productsGroupTranform[i].gameObject.SetActive(i == id);
                if (i == id)
                {
                    currentProductsGroupTranform = productsGroupTranform[i];
                    productsTranform = currentProductsGroupTranform.GetComponentsInChildren<ProductUITool>();
                }
            }
            for (int i = 0; i < productsTranform.Length; i++) 
            {
                productsTranform[i].panel.gameObject.SetActive(false);
            }
        }
        public void ShowProductUI(string type)
        {
            foreach (ProductUITool tool in productsTranform)
            {
                tool.panel.gameObject.SetActive(tool.productType == type);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace NetworkManagement
{
    /// <summary>
    /// Product type UI.
    /// </summary>
    public class ProductTypeUI : MonoBehaviour
    {
        [SerializeField] private Image productImage;
        [SerializeField] private Text productTypeText;
        public ProductType productType
        {
            get;
            private set;
        }


        public void SetProductType(ProductType productType)
        {
            this.productType = productType;
            productImage.sprite = productType.icon;
            this.productTypeText.text = productType.type;
        }
    }
}

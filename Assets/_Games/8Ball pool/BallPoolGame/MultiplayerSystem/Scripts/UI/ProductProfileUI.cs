using UnityEngine;
using UnityEngine.UI;

namespace NetworkManagement
{
    /// <summary>
    /// The product UI.
    /// </summary>
    public class ProductProfileUI : MonoBehaviour
    {
        [SerializeField] private RawImage icon;
        [SerializeField] private Text productName;
        [SerializeField] private Text price;
        [SerializeField] private Image state;
        [SerializeField] private Sprite availableSprite;
        [SerializeField] private Sprite lockedSprite;

        [SerializeField] private Image priceImage;
        [SerializeField] private Sprite coinsSprite;
        [SerializeField] private Sprite dollarSprite;
        private bool isSeted = false;

        public NetworkManagement.ProductProfile product
        {
            get;
            private set;
        }

        void Awake()
        {
            if (!isSeted)
            {
                price.transform.parent.gameObject.SetActive(false);
                state.gameObject.SetActive(false);
            }
        }

        public void SetProduct(NetworkManagement.ProductProfile product)
        {
            isSeted = true;
            bool purchased = product.data.purchased;
            this.product = product;
            icon.texture = product.icon;
            this.productName.text = product.data.name;
            if (product.data.price > 0)
            {
                price.text = (product.data.isRealMoney ? "$" : "") + product.data.price + "";
            }
            price.transform.parent.gameObject.SetActive(product.data.price > 0 && (!product.data.oneTimeBought || !purchased));
            state.sprite = purchased ? availableSprite : lockedSprite;

            priceImage.sprite = product.data.isRealMoney ? dollarSprite : coinsSprite;
            state.gameObject.SetActive(product.data.oneTimeBought);
        }
    }
}

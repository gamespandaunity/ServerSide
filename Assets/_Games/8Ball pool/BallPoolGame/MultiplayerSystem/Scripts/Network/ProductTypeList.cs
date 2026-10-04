using UnityEngine;
using NetworkManagement;

public class ProductTypeList : ScriptableObject
{
//    [SerializeField] private DefaultProductProfile _defaultCoins;
//    public ProductProfile defaultCoins
//    {
//        get
//        {
//            ProductData productData = new ProductData(0, "Coin", "", _defaultCoins.name, _defaultCoins.price, true, false, 1, null, null);
//            return new ProductProfile(productData, _defaultCoins.icon);
//        }
//    }

    public ProductTypeGroup[] productsGroup;
    public ProductType[] GetProductTypeListByName(string name)
    {
        foreach (var productTypeGroup in productsGroup)
        {
            if (productTypeGroup.name == name)
            {
                return productTypeGroup.productsList;
            }
        }
        return null;
    }
}
[System.Serializable]
public class ProductTypeGroup
{
    public string name;
    public ProductType[] productsList;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

namespace NetworkManagement
{
    public enum GraphicMode
    {
        Universal = 0,
        TwoD,
        ThreeD
    }
    [ExecuteInEditMode]
    /// <summary>
    /// The Products management, do not destroyable object, it controlled all product on all scenes.
    /// </summary>
    public class ProductsManagement : MonoBehaviour
    {
        public static string productsPath;
        [SerializeField] private string _productsPath;
        public class ProductsGroup
        {
            public Product[] products
            {
                get;
                private set;
            }
            public ProductsGroup(Product[] products)
            {
                this.products = products;
                foreach (var item in products)
                {
                    if(item.gameObject.activeSelf)
                    {
                        item.Initialize();
                    }
                }
            }
        }

        public static ProductsManagement instance;

        private static ProductsGroup[] productsGroup;
        [SerializeField] private Transform[] productsGroupTranform;

        void Awake()
        {
            productsPath = _productsPath;
            if (Application.isPlaying)
            {
                if (instance)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DontDestroyOnLoad(gameObject);
                    instance = this;
                    productsGroup = new ProductsGroup[productsGroupTranform.Length];
                    for (int i = 0; i < productsGroupTranform.Length; i++)
                    {
                        productsGroup[i] = new ProductsGroup(productsGroupTranform[i].GetComponentsInChildren<Product>());
                    }
                }
            }
        }
        void OnEnable()
        {
            //if (!Application.isPlaying)
            //{
            //    foreach (var item in transform.GetComponentsInChildren<Product>())
            //    {
            //        item.ResetWhenBackToEditor();
            //    }
            //}
        }

        void OnDisable()
        {
            //if (!Application.isPlaying)
            //{
            //    foreach (var item in transform.GetComponentsInChildren<Product>())
            //    {
            //        item.ResetWhenBackToEditor();
            //    }
            //}
        }
        public static Product[] GetProductsById(int id)
        {
            return productsGroup[id].products;
        }
        public class ProductInfo
        {
            public string nameInВatabase{ get ; private set; }
            public ProductInfo(string nameInВatabase)
            {
                this.nameInВatabase = nameInВatabase;
            }
            public List<string> sourceURLInAllPlatform;
        }
        public static IEnumerator LoadProducts(ProductInfo productInfo)
        {
            DownloadManager.DownloadParameters productsParameters = new DownloadManager.DownloadParameters(ProductsManagement.productsPath, "list", DownloadManager.DownloadType.DownloadOrLoadFromDisc);
            yield return DownloadManager.Download(productsParameters);
            if (!productsParameters.isNull)
            {
                XmlDocument productsDockument = new XmlDocument();
                productsDockument.LoadXml(productsParameters.text);
                XmlNode productsXml = productsDockument.SelectSingleNode("Products");
                XmlNode concreteProducts = productsXml.SelectSingleNode(productInfo.nameInВatabase);
                foreach (XmlNode product in concreteProducts.ChildNodes)
                {
                    XmlNode sources = product.SelectSingleNode("SourcesURL");
                    if (sources != null)
                    {
                        XmlNodeList sourcesListURL = sources.ChildNodes;
                        productInfo.sourceURLInAllPlatform = new List<string>(0);
                        foreach (XmlNode sourceURL in sourcesListURL)
                        {
                            productInfo.sourceURLInAllPlatform.Add(sourceURL.InnerText);
                        }
                    }
                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace NetworkManagement
{
    public delegate void SetParametersHandler(System.Type type, params object[] parameters); 
    /// <summary>
    /// Describes the product to be purchased.
    /// </summary>
    public abstract class Product : MonoBehaviour
    {
        public static event SetParametersHandler OnSetParameters;
        protected DownloadManager.DownloadParameters[] parameters;
        [SerializeField] protected string[] names;
        [SerializeField] protected string type;
        public ProductProfile productProfile{ get; private set; }
        public int currentProductCount{ get; set; }
        public string iconeURL { get; protected set; }
        public string iconeName { get; protected set; }
        public string[] sourcesURL { get; protected set; }
		public static bool update;
		protected bool updateThis;
        public void Initialize()
        {
            StartCoroutine( FirstInitializeProduct() );
        }
        protected static void CallSetParameters(System.Type type, params object[] parameters)
        {
            if (OnSetParameters != null)
            {
                OnSetParameters(type, parameters); 
            }
        }
		public void SetForUpdate()
		{
			updateThis = Product.update;
		}
        public abstract void ResetWhenBackToEditor();
        protected abstract IEnumerator SetSources();
       
        protected virtual IEnumerator FirstInitializeProduct()
        {
            List<string> sourcesUrlList = new List<string>(0);
            int sourcesId = 0;
            string sourceURL = GetSourcesURL(sourcesId);

            while (!string.IsNullOrEmpty(sourceURL))
            {
                sourcesUrlList.Add(sourceURL);
                sourcesId++;
                sourceURL = GetSourcesURL(sourcesId);
                yield return null;
            }
            if (sourcesId > 0)
            {
                sourcesURL = sourcesUrlList.ToArray();
            }
            iconeURL = GetIconURL();
            iconeName = GetIconName();

            yield return StartCoroutine(SetSources());
        }
        public virtual IEnumerator ShowSavedProduct()
        {
            yield return null;
        }
       
        public virtual bool CheckForDownload(string sourceURL)
        {
            return true;
        }
        public virtual IEnumerator InitializeProduct (ProductProfile productProfile)
        {
            yield return null;
            this.productProfile = productProfile;
            if (productProfile.data.xmlNode != null)
            {
                XmlNode sources = productProfile.data.xmlNode.SelectSingleNode("SourcesURL");
                if (sources != null)
                {
                    XmlNodeList sourcesListURL = sources.ChildNodes;

                    List<string> sourceURLInCurrentPlatform = new List<string>(0);

                    foreach (XmlNode sourceURL in sourcesListURL)
                    {
                        if (CheckForDownload(sourceURL.InnerText))
                        {
                            sourceURLInCurrentPlatform.Add(sourceURL.InnerText);
                        }
                    }
                    sourcesURL = new string[sourceURLInCurrentPlatform.Count];
                    int i = 0;
                    foreach (string sourceURL in sourceURLInCurrentPlatform)
                    {
                        sourcesURL[i] = sourceURL;
                        i++;
                    }
                }
            }
            else
            {
                sourcesURL = null;
            }
        }
        public static Product FindProductByType(string type, Product[] products)
        {
            foreach (Product product in products)
            {
                if (product.type == type)
                {
                    return product;
                }
            }
            return null;
        }
        public void SaveIconName(string iconName)
        {
            DataManager.SetStringData(type + "_IconName", iconName);
        }
        public string GetIconName()
        {
            return DataManager.GetStringData(type + "_IconName");
        }

        public void SaveIconURL(string iconURL)
        {
            DataManager.SetStringData(type + "_IconURL", iconURL);
        }
        public string GetIconURL()
        {
            return DataManager.GetStringData(type + "_IconURL");
        }

        public void SaveSourceURL(int sourcesId, string sourcesURL)
        {
            DataManager.SetStringData(name + "_SourceUR_" + type + "_"  + sourcesId, sourcesURL);
        }
        public string GetSourcesURL(int sourcesId)
        {
            return DataManager.GetStringData(name + "_SourceUR_" + type + "_"  + sourcesId);
        }
    }
    /// <summary>
    /// Describes all the data and properties of the product
    /// </summary>
    public class ProductData
    {
        /// <summary>
        /// This product unique identifier.
        /// </summary>
        public int id
        {
            get;
            private set;
        }
        /// <summary>
        /// The product can be bought only with real money
        /// </summary>
        public bool isRealMoney
        {
            get;
            private set;
        }
        /// <summary>
        /// The product can be bought one time.
        /// </summary>
        public bool oneTimeBought
        {
            get;
            private set;
        }
        /// <summary>
        /// How many time the product can be bought?.
        /// </summary>
        public int maxCount
        {
            get;
            private set;
        }

        /// <summary>
        /// This product name.
        /// </summary>
        public string name
        {
            get;
            private set;
        }
        public string type
        {
            get;
            private set;
        }
        /// <summary>
        /// Product icone URL.
        /// </summary>
        public string iconURL
        {
            get;
            private set;
        }
        /// <summary>
        /// Product price.
        /// </summary>
        public int price
        {
            get;
            set;
        }
        /// <summary>
        /// Product is purchased.
        /// </summary>
        public bool purchased
        {
            get{ return false; }
        }
        /// <summary>
        /// Used this xml, you can get product data.
        /// </summary>
        public XmlNode xmlNode
        {
            get;
            private set;
        }
        /// <summary>
        /// Gets the default product profile on the build.
        /// </summary>
        public DefaultProductProfile defaultProductProfile
        {
            get;
            private set;
        }
        public ProductData(int id, string type, string iconURL,  string name, int price, bool isRealMoney, bool oneTimeBought, int maxCount, XmlNode xmlNode, DefaultProductProfile defaultProductProfile)
        {
            this.id = id;
            this.type = type;
            this.iconURL = iconURL;
            this.name = name;
            this.price = price;
            this.isRealMoney = isRealMoney;
            this.oneTimeBought = oneTimeBought;
            this.maxCount = maxCount;
            this.xmlNode = xmlNode;
            this.defaultProductProfile = defaultProductProfile;
        }
        public static ProductData GetFromXmlNode(int id, string type, XmlNode xmlNode)
        {
            string name = xmlNode.SelectSingleNode("Name").InnerText;
            string iconURL = xmlNode.SelectSingleNode("IconURL").InnerText;
            int price = int.Parse(xmlNode.SelectSingleNode("Price").InnerText);
            bool isRealMoney = xmlNode.SelectSingleNode("RealMoney").InnerText == "Yes";
            bool oneTimeBought = xmlNode.SelectSingleNode("OneTimeBought").InnerText == "Yes";
            XmlNode maxCountNode = xmlNode.SelectSingleNode("MaxCount");
            int maxCount = 0;
            if (maxCountNode != null)
            {
                maxCount = int.Parse(xmlNode.SelectSingleNode("MaxCount").InnerText);
            }
            return new ProductData(id, type, iconURL, name, price, isRealMoney, oneTimeBought, maxCount, xmlNode, null);
        }
        public int PriceFromName()
        {
            string priceData = "";
            int result = 0;
            foreach (var item in name)
            {
                int s;
                if (int.TryParse(item.ToString(), out s))
                {
                    priceData += s.ToString();
                }
            }
            result = int.Parse(priceData);
            return result;
        }   
    }
}

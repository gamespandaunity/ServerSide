using System.Collections;
using UnityEngine;

namespace NetworkManagement
{
    public class ProductTable2D : NetworkManagement.ProductWithTexturs2D
    {
        public Material boardMaterial;
        public Material clothColorMaterial;
        public Material clothMaterial;

        public string table2DBoardURL{ get; private set; }
        public string table2DClothURL{ get; private set; }
       

        public Texture tableBoardDefault2DTexture{ get; private set; }
        public Texture tableClothDefault2DTexture{ get; private set; }

        public Color mainTable2DColor{ get; private set; }
        public Texture mainTableBoard2DTexture{ get; private set; }
        public Texture mainTableCloth2DTexture{ get; private set; }

        void OnEnable()
        {
            GradientTool.OnSelectColor += GradientTool_OnSelectColor;
            string currentKey = DataManager.GetStringData("Table2DClothColorKey");
            GetStartColor(currentKey);
        }

        void Start()
        {
            tableBoardDefault2DTexture = boardMaterial.mainTexture;
            tableClothDefault2DTexture = clothMaterial.mainTexture;
        }

        void GetStartColor(string key)
        {
            Color color;
            if (NetworkManagement.DataManager.GetColorData(key, out color))
            {
                clothColorMaterial.color = color;
            }
            GradientTool.SetStartColor(clothColorMaterial.color);
        }

        void OnDisable()
        {
            GradientTool.OnSelectColor -= GradientTool_OnSelectColor;
        }

        void GradientTool_OnSelectColor(Color color)
        {
            clothColorMaterial.color = color;
            string currentKey = DataManager.GetStringData("Table2DClothColorKey");
            NetworkManagement.DataManager.SetColorData(currentKey, color);
        }

        public override IEnumerator InitializeProduct(ProductProfile productProfile)
        {
            yield return StartCoroutine(base.InitializeProduct(productProfile));
            string currentKey = "Table2DClothColor_" + productProfile.data.type + "_" + productProfile.data.name;
            DataManager.SetStringData("Table2DClothColorKey", currentKey);
            GetStartColor(currentKey);
        }
        protected override IEnumerator SetSources()
        {
            yield return StartCoroutine(base.SetSources());
            if (sourcesURL != null && sourcesURL.Length > 1)
            {
                table2DBoardURL = sourcesURL[0];
                table2DClothURL = sourcesURL[1];
            }
            else
            {
                table2DBoardURL = "";
                table2DClothURL = "";
            }
            mainTableBoard2DTexture = materials[0].mainTexture;
            mainTableCloth2DTexture = materials[1].mainTexture;
            mainTable2DColor = clothColorMaterial.color;
        }
    }
}
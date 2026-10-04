using UnityEngine;
using UnityEngine.UI;

namespace NetworkManagement
{
    public class LinesUITool : ProductUITool
    {
        [SerializeField] private Text[] linesText;

        void OnEnable()
        {
            ProductLines_OnSetParameters(typeof(ProductLines), new object[]{ ProductLines.lineLength, ProductLines.shortLineCound, ProductLines.middleLineCound, ProductLines.longLineCound });
            ProductLines.OnSetParameters += ProductLines_OnSetParameters;
        }
            
        void OnDisable()
        {
            ProductLines.OnSetParameters -= ProductLines_OnSetParameters;
        }
        void ProductLines_OnSetParameters (System.Type type, object[] parameters)
        {
            if (type == typeof(ProductLines))
            {
                for (int i = 1; i < parameters.Length; i++)
                {
                    linesText[i - 1].text = parameters[i] + "";
                }
            }
        }
    }
}

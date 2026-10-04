using UnityEngine;
using UnityEngine.UI;

namespace NetworkManagement
{
    public class AIUITool : ProductUITool
    {
        [SerializeField] private Text aiText;

        void OnEnable()
        {
            ProductAI_OnSetParameters (typeof(ProductAI), new object[]{ProductAI.aiCount});
            ProductAI.OnSetParameters += ProductAI_OnSetParameters;
        }
        void OnDisable()
        {
            ProductAI.OnSetParameters -= ProductAI_OnSetParameters;
        }
        void ProductAI_OnSetParameters (System.Type type, object[] parameters)
        {
            if (type == typeof(ProductAI))
            {
                aiText.text = parameters[0] + "";
            }
        }

    }
}

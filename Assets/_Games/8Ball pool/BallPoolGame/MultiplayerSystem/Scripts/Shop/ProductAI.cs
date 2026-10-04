using System.Collections;
using UnityEngine;

namespace NetworkManagement
{
    public class ProductAI : NetworkManagement.Product
    {
        public static int aiCount
        {
            get{return DataManager.GetInt("AI_Count");}
            set
            {
                int _aiCount = Mathf.Clamp(value, 0, value); 
                if (_aiCount == 0)
                {
                    DataManager.SetIntData("AI_IsZero", 1);
                }
                CallSetParameters(typeof(ProductAI), _aiCount); 
                DataManager.SetInt("AI_Count", _aiCount);
            }
        }
        protected override IEnumerator FirstInitializeProduct()
        {
            yield return StartCoroutine(base.FirstInitializeProduct());
            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();
            bool iaIsZero = DataManager.GetIntData("AI_IsZero") == 1;
            aiCount = DataManager.GetInt("AI_Count");

            if (!iaIsZero && aiCount == 0)
            {
                aiCount = 5;
            }
        }
        public override IEnumerator InitializeProduct(ProductProfile productProfile)
        {
            yield return StartCoroutine(base.InitializeProduct(productProfile));
            aiCount += currentProductCount * int.Parse(productProfile.data.name.Replace(" AI", ""));
            //Debug.Log("Initialized aiCount " + aiCount);
        }
        public override void ResetWhenBackToEditor()
        {
            
        }
        protected override IEnumerator SetSources()
        {
            yield return null;
        }
    }
}

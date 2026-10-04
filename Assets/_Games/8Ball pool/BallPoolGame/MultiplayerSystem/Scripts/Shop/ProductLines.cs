using System.Collections;
using UnityEngine;

namespace NetworkManagement
{
    public class ProductLines : NetworkManagement.Product
    {
        private static void SaveData()
        {
            CallSetParameters(typeof(ProductLines), lineLength, shortLineCound, middleLineCound, longLineCound);
        }

        private static float _lineLength;
        public static float lineLength
        {
            get
            { 
                _lineLength = 0.125f;
                if (longLineCound > 0)
                {
                    _lineLength = 0.75f;
                }
                if (longLineCound == 0 && middleLineCound > 0)
                {
                    _lineLength = 0.5f;
                }
                if (middleLineCound == 0 && longLineCound == 0 && shortLineCound > 0)
                {
                    _lineLength = 0.25f;
                }
                return _lineLength; 
            }
        }

        public static int shortLineCound
        {
            get{ return DataManager.GetInt("ShortLineCound"); }
            set{DataManager.SetInt("ShortLineCound", value); SaveData(); }
        }

        public static int middleLineCound
        {
            get{ return DataManager.GetInt("MiddleLineCound"); }
            set{DataManager.SetInt("MiddleLineCound", value); SaveData(); }
        }

        public static int longLineCound
        {
            get{ return DataManager.GetInt("LongLineCound"); }
            set{DataManager.SetInt("LongLineCound", value); SaveData(); }
        }

        protected override IEnumerator FirstInitializeProduct()
        {
            yield return StartCoroutine(base.FirstInitializeProduct());
            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();
            bool isFirstTime = DataManager.GetIntData("FirstTimeUptadeLineLength") == 0;
            if (isFirstTime)
            {
                DataManager.SetIntData("FirstTimeUptadeLineLength", 1);
                _lineLength = 0.25f;
                shortLineCound = 1000;
            }
        }

        public override IEnumerator InitializeProduct(ProductProfile productProfile)
        {
            yield return StartCoroutine(base.InitializeProduct(productProfile));
            string typeName = productProfile.data.name.Replace("line", "").Replace(" ", "");
            if (typeName == "Short")
            {
                if (middleLineCound == 0 && longLineCound == 0)
                {
                    _lineLength = 0.25f;
                }
                shortLineCound += 1000;
            }
            else if (typeName == "Middle")
            {
                if (longLineCound == 0)
                {
                    _lineLength = 0.5f;
                }
                middleLineCound += 100;
            }
            else if (typeName == "Long")
            {
                _lineLength = 0.75f;
                longLineCound += 100;
            }
            //Debug.Log("Initialized lineLength " + typeName + "  " + _lineLength);
        }

        private static void CalculateLineLength()
        {
            _lineLength = 0.125f;
            if (longLineCound > 0)
            {
                longLineCound--;
                if (longLineCound > 0)
                {
                    _lineLength = 0.75f;
                }
            }
            if (longLineCound == 0 && middleLineCound > 0)
            {
                middleLineCound--;
                if (middleLineCound > 0)
                {
                    _lineLength = 0.5f;
                }
            }
            if (middleLineCound == 0 && longLineCound == 0 && shortLineCound > 0)
            {
                shortLineCound--;
                if (shortLineCound > 0)
                {
                    _lineLength = 0.25f;
                }
            }
        }
        public static void OnShot(ref float lLength)
        {
            CalculateLineLength();
            lLength = _lineLength;
            //ConstantsData_M.LogInfo("lLength "  +lLength +  "  " + shortLineCound + "  " + middleLineCound + "  "+ longLineCound);
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

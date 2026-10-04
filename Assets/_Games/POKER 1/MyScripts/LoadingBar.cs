using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POKER
{
    public class LoadingBar : MonoBehaviour
    {
        public float TimeToFill = 3.0f;
        public Image LoadingBarImg;
        public TMP_Text LoadingPercentageTxt;
        float daltaTime;

        // Update is called once per frame
        void Update()
        {
            if (daltaTime < TimeToFill)
            {
                daltaTime += Time.deltaTime;
                float barProgress = daltaTime / TimeToFill;
                LoadingBarImg.fillAmount = barProgress;
                if (barProgress > 1)
                    barProgress = 1;
                LoadingPercentageTxt.text = (int)(barProgress * 100) + "%";
            }
        }

        private void OnEnable()
        {
            daltaTime = 0;
            LoadingBarImg.fillAmount = 0;
            LoadingPercentageTxt.text = "0%";
        }
    }
}
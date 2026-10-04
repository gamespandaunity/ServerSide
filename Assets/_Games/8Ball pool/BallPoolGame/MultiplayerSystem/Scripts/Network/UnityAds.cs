#if UNITY_ADS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;



namespace NetworkManagement
{
    public class UnityAds: Ads
    {
        
        private ViewAdsHandler handler;
        public void ShowAds(ViewAdsHandler handler)
        {
            ShowSomeAds(handler, "rewardedVideo");
        }
        public void AutoShowAds(ViewAdsHandler handler)
        {
            ShowSomeAds(handler, "video");
        }
        private void ShowSomeAds(ViewAdsHandler handler, string placementId)
        {
            this.handler = handler;
            Constants_M.LogInfo(Advertisement.isInitialized + "  " + Advertisement.IsReady(placementId) + "   " + placementId);
            if (Advertisement.isInitialized && Advertisement.IsReady(placementId))
            {
                var options = new ShowOptions { resultCallback = OnShowResult };
                Advertisement.Show(placementId, options);
            }
            else
            {
                this.handler(AdsShowResult.Failed);
            }
        }
        private void OnShowResult(ShowResult result)
        {
            if (result == ShowResult.Finished)
            {
                this.handler(AdsShowResult.Finished);
            }
            else
            {
                this.handler(AdsShowResult.Failed);
            }
        }
    }
}
#endif

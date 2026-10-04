using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BannerMove : MonoBehaviour
{
    public GameObject texture2d;
    void Start()
    {
        ApiAndRoomManager._instance.DisplayBanner(OnSuccess =>
        {
            DeserlizeBannerDetails(OnSuccess);
        });
    }

    void DeserlizeBannerDetails(string jsonString)
    {
        JObject jsonObj = JObject.Parse(jsonString);
        // Check the status
        bool status = jsonObj["status"].Value<bool>();
        if (status)
        {
            JObject dataArray = (JObject)jsonObj["data"];
            JArray bankDetails = (JArray)dataArray["banner_list"];
            //  GameObject banksmanager = Instantiate(bankDetailTemplate, bankDetailinfo.transform);
            foreach (JObject bankDetail in bankDetails)
            {
                              
                staticVariables.BannerUrls.Add(bankDetail["file_url"].ToString());
            }
            for(int i =  0; i <  staticVariables.BannerUrls.Count; i++)
            {
                GameObject img = Instantiate(UIMainMenManager.instance.bannerImagePrefab, UIMainMenManager.instance.bannerContainer.transform);
                Image rawComponent = img.GetComponent<Image>();
                UIMainMenManager.instance.bannerImages.Add(rawComponent);
            }
            UIMainMenManager.instance.FetchAndShowBanners();
        }
        else
        {
            //Debug.Log("No banks found");
        }
    }
}

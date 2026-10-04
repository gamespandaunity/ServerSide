using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkManagement
{
    public class ProductWithAssetBundle : NetworkManagement.Product
    {
        protected AssetBundle[] assetBundles;
        public override void ResetWhenBackToEditor()
        {

        }
        public string GetPlatformNameFromPlatform(RuntimePlatform platform)
        {
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                return "StandaloneOSXUniversal";
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                return "StandaloneWindows";
            }
            else if(Application.platform == RuntimePlatform.Android)
            {
                return "Android";
            }
            else if(Application.platform == RuntimePlatform.IPhonePlayer)
            {
                return "iOS";
            }
            else if(Application.platform == RuntimePlatform.WebGLPlayer)
            {
                return "WebGL";
            }
            return "";
        }
        public string RemovePlatformNameFromSourceURL(string sourceURL)
        {
            return sourceURL.Replace(GetPlatformNameFromPlatform(Application.platform), "");
        }
        public string AddPlatformNameToSourceURL(string sourceURL)
        {
            if (string.IsNullOrEmpty(sourceURL))
            {
                return "";
            }
            return sourceURL.Insert(sourceURL.LastIndexOf("_"), GetPlatformNameFromPlatform(Application.platform));
        }
        public RuntimePlatform GetPlatformFromSourceURL(string sourceURL)
        {
            if (sourceURL.Contains("Android"))
            {
                return RuntimePlatform.Android;
            }
            else if (sourceURL.Contains("iOS"))
            {
                return RuntimePlatform.IPhonePlayer;
            }
            else if (sourceURL.Contains("StandaloneOSXUniversal"))
            {
                return RuntimePlatform.OSXPlayer;
            }
            else if (sourceURL.Contains("StandaloneWindows"))
            {
                return RuntimePlatform.WindowsPlayer;
            }
            else if (sourceURL.Contains("WebGL"))
            {
                return RuntimePlatform.WebGLPlayer;
            }
            else
            {
                return RuntimePlatform.LinuxPlayer;
            }
        }

        public string FindUrlForCurrentPlatform(string sourceURL, List<string> sourceURLInAllPlatform)
        {
            if (sourceURLInAllPlatform == null || sourceURLInAllPlatform.Count == 0)
            {
                return "";
            }
            foreach (var item in sourceURLInAllPlatform)
            {
                if (GetNameFromSourceURL(sourceURL) == GetNameFromSourceURL(item) && CheckForDownload(item))
                {
                    return item;
                }
            } 
            return "";
        }
        public string GetNameFromSourceURL(string sourceURL)
        {
            bool canSum = false;
            string sum = "";
            foreach (var item in sourceURL)
            {
                if (item == '?')
                {
                    break;
                }
                if (canSum)
                {
                    sum += item + "";
                }
                if (item == '_')
                {
                    canSum = true;
                }
            }
            return sum;
        }
        public override bool CheckForDownload(string sourceURL)
        {
            //Debug.Log("Application.platform " + Application.platform.ToString());
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                return sourceURL.Contains("StandaloneOSXUniversal");
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                return sourceURL.Contains("StandaloneWindows");
            }
            else
            {
                return Application.platform == GetPlatformFromSourceURL(sourceURL);
            }
        }
        public override IEnumerator InitializeProduct(ProductProfile productProfile)
        {
            yield return StartCoroutine(base.InitializeProduct(productProfile));
            yield return StartCoroutine(SetSources());
        }
        protected override IEnumerator SetSources()
        {
            if (sourcesURL != null && sourcesURL.Length > 0)
            {
                parameters = new DownloadManager.DownloadParameters[sourcesURL.Length];
                for (int i = 0; i < sourcesURL.Length; i++)
                {
                    if (assetBundles != null && assetBundles[i] != null)
                    {
                        assetBundles[i].Unload(true);
                    }
					parameters[i] = new DownloadManager.DownloadParameters(sourcesURL[i], names[i], updateThis? DownloadManager.DownloadType.Update : DownloadManager.DownloadType.DownloadOrLoadFromDisc);
                    yield return DownloadManager.Download(parameters[i], true);
                }
                assetBundles = new AssetBundle[sourcesURL.Length];
                for (int i = 0; i < sourcesURL.Length; i++)
                {
                    if (!parameters[i].isNull)
                    {
                        assetBundles[i] = parameters[i].assetBundle;
                        SaveSourceURL(i, parameters[i].URL);
                    }
                }
            }
            else
            {
                if (assetBundles != null && assetBundles.Length > 0)
                {
                    for (int i = 0; i < assetBundles.Length; i++)
                    {
                        if (assetBundles[i] != null)
                        {
                            assetBundles[i].Unload(true);
                        }
                    }
                }
               
                if (parameters != null)
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        SaveSourceURL(i, "");
                    }
                    parameters = null;
                }
                assetBundles = null;

            }
			updateThis = false;
            yield return null;
        }
    }
}

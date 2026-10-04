using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.UI;

public class AddressablesBundleSpawning : MonoBehaviour
{
    //[SerializeField] public AssetReference RouletteAddressableSceneName;
    [SerializeField] public AssetReference twelveOfflineddressableSceneName;
    [SerializeField] public AssetReference twelveOnlineAddressableSceneName;
    [SerializeField] public AssetReference horseAddressableSceneName;
    [SerializeField] public AssetReference carAddressableSceneName;
    // [SerializeField] public AssetReference cricketMenuAddressableSceneName;
    // [SerializeField] public AssetReference cricketGroundAddressableSceneName;
    //[SerializeField] public Image downloadProgressSlider;
    //[SerializeField] private TextMeshProUGUI downloadProgressText;

    public UnityEvent OnDownloaded;
    public UnityEvent OnNotDownloaded;

    public static AddressablesBundleSpawning instance;

    private void Awake()
    {
        instance = this;
        //Caching.ClearCache();
    }
    public async void CheckSceneStatus(AssetReference sceneName, Action<bool> onDownloadedCallback)
    {
        AsyncOperationHandle<long> downloadSizeHandle = Addressables.GetDownloadSizeAsync(sceneName);

        await downloadSizeHandle.Task;

        long downloadSize = downloadSizeHandle.Result;
        if (downloadSizeHandle.Status == AsyncOperationStatus.Succeeded)
        {

            if (downloadSize == 0)
            {
                ////Debug.Log("Scene is already cached.");
            }
            else
            {
                //Debug.Log("Scene needs to be downloaded. Download size: " + downloadSize);
            }
        }
        else
        {
            ConstantsData_M.Log("Failed to get download size: " + downloadSizeHandle.Status);
        }
        onDownloadedCallback.Invoke(downloadSize == 0);
    }
  
    

    public async void LoadAddressableScene(AssetReference sceneName)
    {

       var handle = Addressables.LoadSceneAsync(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            //Debug.Log("Scene loaded successfully.");
        }
        else
        {
            ConstantsData_M.Log("Failed to load scene.");
        }
    }
  
  

    //public async void LoadMaterial()
    //{
    //    materialHandle = Addressables.LoadAssetAsync<Material>(materialReference);
    //    await materialHandle.Task;

    //    if (materialHandle.Status == AsyncOperationStatus.Succeeded)
    //    {
    //        Material loadedMaterial = materialHandle.Result;
    //        GetComponent<Renderer>().material = loadedMaterial;
    //    }
    //    else
    //    {
    //        Constants_M.Log("Failed to load material.");
    //    }
    //}

    private void OnDestroy()
    {
        //if (handle.IsValid()) Addressables.Release(handle);
        //if (materialHandle.IsValid()) Addressables.Release(materialHandle);
    }
}

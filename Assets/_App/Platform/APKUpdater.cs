using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class APKUpdater : MonoBehaviour
{
    string apkUrl = "https://bundles.pandaau.net.au/GamesBundles/0.8/1.apk";

    public void StartAPKDownload()
    {
        StartCoroutine(DownloadAndInstallAPK());
    }

    IEnumerator DownloadAndInstallAPK()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "app-latest.apk");

        UnityWebRequest request = UnityWebRequest.Get(apkUrl);
        request.downloadHandler = new DownloadHandlerFile(filePath);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("APK downloaded to: " + filePath);
            InstallAPK(filePath);
        }
        else
        {
            Debug.LogError("Download failed: " + request.error);
        }
    }

    void InstallAPK(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
 
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW");
            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
            AndroidJavaObject file = new AndroidJavaObject("java.io.File", path);
            AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("fromFile", file);
 
            intent.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");
            intent.Call<AndroidJavaObject>("addFlags", 0x10000000); // FLAG_ACTIVITY_NEW_TASK
            currentActivity.Call("startActivity", intent);
        }
#endif
    }
}
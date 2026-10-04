using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Android;

public class ScreenshotCaptureAndDisplay : MonoBehaviour
{
    //public Image screenshotImage;
     string screenshotFileName;
    string TextToShowOnShare;
    GameObject btn;
    public GameObject[] objects;
    public void CaptureScreenshotAndDisplay()
    {
        btn = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject;
        btn.SetActive(false);
        LocalSettings.ToggleObjectState(objects, false);
        string formattedDate = System.DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
        TextToShowOnShare = "Receipt_" + formattedDate;
        screenshotFileName = "Receipt_" + formattedDate + ".png";
        TeenPattiGame.SoundManager.Instance.PlayAudioClip(TeenPattiGame.SoundManager.AllSounds.CameraShutterSound, false);
        StartCoroutine(TakeSS());
    }


    IEnumerator TakeSS()
    {
        yield return new WaitForEndOfFrame();
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
        // Capture the entire screen, including UI elements
        Texture2D screenshotTexture = null;
        screenshotTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        yield return new WaitForEndOfFrame();
        screenshotTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        yield return new WaitForEndOfFrame();
        screenshotTexture.Apply();
        yield return new WaitForEndOfFrame();
        // Encode the screenshot to a byte array
        byte[] bytes = screenshotTexture.EncodeToPNG();
        yield return new WaitForEndOfFrame();
        string screenshotPath = Path.Combine(Application.persistentDataPath, screenshotFileName);
        // Save the screenshot to a file
        System.IO.File.WriteAllBytes(screenshotPath, bytes);
        yield return new WaitForEndOfFrame();
        // Load the saved screenshot and display it on the Canvas Image
        Texture2D loadedTexture = new Texture2D(2, 2); // Create a temporary texture
        loadedTexture.LoadImage(bytes);
        // Converting texture to sprite
        //screenshotImage.sprite = Sprite.Create(loadedTexture, new Rect(0, 0, loadedTexture.width, loadedTexture.height), Vector2.zero);

        //Toaster.ShowAToast(TextToShowOnShare);
        btn.SetActive(true);
        LocalSettings.ToggleObjectState(objects, true);
        

    }



    
}

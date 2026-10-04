using System.Collections;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

public class ScreenShot : MonoBehaviour
{
    public Texture2D screenshotImg;
    public Image screenshotImgSprite;
    // Start is called before the first frame update
    void Start()
    {

    }


    bool isProcessing;
    public void ShareBtnPress()
    {
        //if (!isProcessing)
        //{
        //    StartCoroutine(ShareScreenshot());
        //}
        takeSS();
    }

    IEnumerator ShareScreenshot()
    {
        isProcessing = true;
        yield return new WaitForSeconds(0);
        string nameOfImg = "SS_" + System.DateTime.Now + ".png";
        string destination = Path.Combine(Application.persistentDataPath, nameOfImg);
        Toaster.ShowAToast(destination);
        if (!Application.isEditor)
        {
            AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
            AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");
            intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
            AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
            AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", "file://" + destination);
            intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_STREAM"),
                uriObject);
            intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"),
                "Can you beat my score?");
            intentObject.Call<AndroidJavaObject>("setType", "image/jpeg");
            AndroidJavaClass unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser",
                intentObject, "Share your new score");
            currentActivity.Call("startActivity", chooser);
            Toaster.ShowAToast("Done: ");
        }
        isProcessing = false;
    }
    string destination;
    //void takeSS()
    //{
    //    string fileName = "SS_" + System.DateTime.Now + ".png";
    //     destination = Path.Combine(Application.persistentDataPath, fileName);
    //    ScreenCapture.CaptureScreenshot(fileName);

    //    Toaster.ShowAToast("SS done");
    //    StartCoroutine(LoadScreenshot(fileName));
    //}
    public SpriteRenderer screenshotSpriteRenderer;
    public RenderTexture renderTexture;
    void takeSS()
    {

        // Ensure the RenderTexture and SpriteRenderer are assigned in the Inspector
        if (screenshotSpriteRenderer == null || renderTexture == null)
        {
            Debug.LogError("Assign the RenderTexture and SpriteRenderer in the Inspector.");
            return;
        }

        // Capture the screenshot to the RenderTexture
        Camera.main.targetTexture = renderTexture;
        Camera.main.Render();

        // Convert the RenderTexture to a Texture2D
        Texture2D screenshotTexture = new Texture2D(renderTexture.width, renderTexture.height);
        RenderTexture.active = renderTexture;
        screenshotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        screenshotTexture.Apply();

        // Create a Sprite from the Texture2D
        Sprite screenshotSprite = Sprite.Create(screenshotTexture, new Rect(0, 0, screenshotTexture.width, screenshotTexture.height), Vector2.zero);

        // Display the Sprite in the SpriteRenderer
        screenshotSpriteRenderer.sprite = screenshotSprite;
        screenshotImgSprite.sprite = screenshotSprite;
        screenshotImgSprite.gameObject.SetActive(true);
        // Clean up by resetting the Camera's target texture
        Camera.main.targetTexture = null;
    }


    // IEnumerator LoadScreenshot(string fileName)
    //{
    //    yield return new WaitForSeconds(1.0f);

    //    string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
    //    screenshotImg = new Texture2D(Screen.width, Screen.height);
    //    screenshotImg.LoadImage(System.IO.File.ReadAllBytes(filePath));
    //    Sprite sprite = Sprite.Create(screenshotImg, new Rect(0, 0, screenshotImg.width, screenshotImg.height), Vector2.zero);

    //    // Assign the Sprite to the SpriteRenderer
    //    screenshotImgSprite.sprite = sprite;

    //    // Optionally, you can use 'img' as needed or display it on a UI element
    //}
}

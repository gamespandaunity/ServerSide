using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Net;
using TMPro;
public class UITransectionDetailSubmition : MonoBehaviour
{

    public TMP_InputField transectionId;
    public Text dollarAmount;
    public Text localCurrencyAmount;
    public Text priceCode;
    [Header("Image ")]
    public RawImage pictureSlip;
    public Texture2D  defaultImage;
    public Texture2D pictureTextureImage;
    public string filePath;
    public static UITransectionDetailSubmition instance;


    
    // Start is called before the first frame update
    void OnEnable()
    {
        if (instance == null)
        {
            instance = this;
        }
        
        dollarAmount.text = staticVariables.shopPackage.amount_usd;
        localCurrencyAmount.text = ((int)staticVariables.shopPackage.amount).ToString();
        priceCode.text = $"Price({staticVariables.shopPackage.currency})";
        HomeMenuManager.instance.backButtonHeader.onClick.RemoveAllListeners();
        HomeMenuManager.instance.backButtonHeader.onClick.AddListener(Back);
    }

        public void Back()
        {
            foreach (GameObject gb in ScreenNavigotor_Custom.Instance.ScreenGameObjects)
            {
                gb.SetActive(gb.name.Equals("ShopPanel"));
            }


        }

  
    public void OnClickUploadImageButton()
    {
        string fileType = NativeFilePicker.ConvertExtensionToFileType("png,jpg,jpeg");
        NativeFilePicker.Permission permission1 = NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                //Debug.Log("File picking cancelled or failed.");
            }
            else
            {
                filePath = path;
                Texture2D originalTexture = NativeGallery.LoadImageAtPath(path, 2073600, false);
                if (originalTexture == null) return;

                Texture2D compressedTexture = CompressTexture(originalTexture, 1024, 1024);
                if (compressedTexture == null) return;

                pictureSlip.texture = compressedTexture;
                pictureTextureImage = compressedTexture;
            }
        }, new string[] { "image/*" });
    }

    private Texture2D CompressTexture(Texture2D source, int maxWidth, int maxHeight)
    {
        // Calculate the new size
        float aspectRatio = (float)source.width / source.height;
        int newWidth, newHeight;

        if (aspectRatio > 1)
        {
            newWidth = maxWidth;
            newHeight = Mathf.RoundToInt(maxWidth / aspectRatio);
        }
        else
        {
            newWidth = Mathf.RoundToInt(maxHeight * aspectRatio);
            newHeight = maxHeight;
        }

        // Resize the texture
        Texture2D result = new Texture2D(newWidth, newHeight, source.format, false);
        Color[] pixels = source.GetPixels(0, 0, source.width, source.height);
        Color[] resizedPixels = new Color[newWidth * newHeight];

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                float newX = x * ((float)source.width / newWidth);
                float newY = y * ((float)source.height / newHeight);
                resizedPixels[y * newWidth + x] = source.GetPixelBilinear(newX / source.width, newY / source.height);
            }
        }

        result.SetPixels(resizedPixels);
        result.Apply();

        return result;
    }
    public void returnBackBtn()
    {
        ResetInpField();
        Destroy(gameObject);
    }

    public void ResetInpField()
    {
        transectionId.text = string.Empty;
        pictureSlip.texture= defaultImage ;        

    }


}

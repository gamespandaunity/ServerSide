using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
public class KeyboardAnimation : MonoBehaviour
{
    public Vector2 originalChatUIPos;
   public RectTransform MoveArea;
    public float panelOffset;
    public TMP_InputField[] inputmaping;
    private KeyboardHeightProvider keyboardHeightProvider;

    private void Start()
    {
        keyboardHeightProvider = FindObjectOfType<KeyboardHeightProvider>();

        MoveArea = GetComponent<RectTransform>();
        originalChatUIPos = MoveArea.anchoredPosition;
        foreach (TMP_InputField iap in inputmaping)
        {
            iap.onSelect.AddListener(delegate { OnSelectInput(iap); });
            ////Debug.Log("Focus Found adding");

        }
    }
   
    void OnSelectInput(TMP_InputField iap)
    {
        if (iap == KeyboardGameManager.Instance.textBox) return;
        Vector2 newPosition = originalChatUIPos;
        float keyboardHeight = 570.8862f;
        RectTransform inputRectTransform = iap.GetComponent<RectTransform>();

        Vector3[] worldCorners = new Vector3[4];
        inputRectTransform.GetWorldCorners(worldCorners);
        float inputFieldBottomY = worldCorners[0].y; // Bottom-left corner

        float targetYFraction = keyboardHeight - inputFieldBottomY;

        float targetY = targetYFraction+panelOffset ;

        newPosition.y += keyboardHeight/2;

        MoveArea.anchoredPosition = newPosition;
    }
    private static int GetKeyboardHeight(bool includeInput)
    {
#if UNITY_EDITOR
        return 400;
#elif UNITY_ANDROID
            using (AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject unityPlayer = unityClass.GetStatic<AndroidJavaObject>("currentActivity").Get<AndroidJavaObject>("mUnityPlayer");
                AndroidJavaObject view = unityPlayer.Call<AndroidJavaObject>("getView");
                AndroidJavaObject dialog = unityPlayer.Get<AndroidJavaObject>("mSoftInputDialog");
                if (view == null || dialog == null)
                    return 0;
                var decorHeight = 0;
                if (includeInput)
                {
                    AndroidJavaObject decorView = dialog.Call<AndroidJavaObject>("getWindow").Call<AndroidJavaObject>("getDecorView");
                    if (decorView != null)
                        decorHeight = decorView.Call<int>("getHeight");
                }
                using (AndroidJavaObject rect = new AndroidJavaObject("android.graphics.Rect"))
                {
                    view.Call("getWindowVisibleDisplayFrame", rect);
                    return Screen.height - rect.Call<int>("height") + decorHeight;
                }
            }
#elif UNITY_IOS
            return (int)TouchScreenKeyboard.area.height;
#else
        return 400;

#endif
    }
    [Serializable]
    public struct InputAreaMaping
    {
        public TMP_InputField inputfield;
        public float VisiblePercentage;
    }
}


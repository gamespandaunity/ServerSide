using UnityEngine;

public class KeyboardHeightProvider : MonoBehaviour
{
    private AndroidJavaObject keyboardHeightProvider;
    private int keyboardHeight;

    void Start()
    {
        
    }

    public int GetKeyboardHeight()
    {
        return keyboardHeight;
    }

   
}

#if UNITY_EDITOR
using UnityEngine;

[CreateAssetMenu(fileName = "GeminiAPISettings", menuName = "Gemini/API Settings")]
public class GeminiAPISettings : ScriptableObject
{
    [SerializeField]
    private string apiKey = "";
    
    public string ApiKey => apiKey;
}
#endif
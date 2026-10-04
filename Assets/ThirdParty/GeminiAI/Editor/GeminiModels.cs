using UnityEngine;
using System.Collections.Generic;

public class GeminiModels : MonoBehaviour
{
    
}
#if UNITY_EDITOR

namespace GeminiScriptGenerator
{
    [System.Serializable]
    public class GeminiRequest
    {
        public List<GeminiContent> contents;
        public GeminiGenerationConfig generationConfig;

        public GeminiRequest(string prompt)
        {
            contents = new List<GeminiContent>
            {
                new GeminiContent
                {
                    parts = new List<GeminiPart>
                    {
                        new GeminiPart
                        {
                            text = $"Generate a C# Unity script based on the following description. Make it a MonoBehaviour. Provide ONLY the code, no explanations: {prompt}"
                        }
                    }
                }
            };
            
            generationConfig = new GeminiGenerationConfig
            {
                temperature = 0.7f,
                topK = 40,
                topP = 0.95f,
                maxOutputTokens = 8192
            };
        }
    }

    [System.Serializable]
    public class GeminiContent
    {
        public List<GeminiPart> parts;
    }

    [System.Serializable]
    public class GeminiPart
    {
        public string text;
    }

    [System.Serializable]
    public class GeminiGenerationConfig
    {
        public float temperature;
        public int topK;
        public float topP;
        public int maxOutputTokens;
    }

    [System.Serializable]
    public class GeminiResponse
    {
        public List<GeminiCandidate> candidates;
    }

    [System.Serializable]
    public class GeminiCandidate
    {
        public GeminiContent content;
    }
}
#endif